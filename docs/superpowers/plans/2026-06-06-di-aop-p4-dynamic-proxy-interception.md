# P4 AOP 承载 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 在 `Tw.DependencyInjection` 中落地统一 `IInterceptorPipeline`、拦截器选择、Castle invocation adapter、Castle interface/class proxy 承载与 `InterceptionReport`。

**Architecture:** P4 只处理方法级调用。Castle adapter 与将来 MVC Filter adapter 都创建 `Tw.DynamicProxy.Abstractions.IInvocationContext`，再调用同一个 pipeline。Middleware、Minimal API 和 gRPC 不进入统一 AOP。Castle 类型必须使用命名空间限定或 alias，避免与 `Tw.DynamicProxy.Abstractions.IInterceptor` 同名混淆。Autofac DynamicProxy 需要在 Autofac `ContainerBuilder.RegisterType(...).EnableInterfaceInterceptors()` / `EnableClassInterceptors()` 注册链上启用，不能只依赖 P2 已写入的 `IServiceCollection` 描述符；因此本阶段先新增 Autofac-native 注册执行器，保留 P2 `IServiceCollection` 执行器作为无 AOP 路径。

**Tech Stack:** C# / .NET 10、Autofac.Extras.DynamicProxy、Castle.Core、Castle.Core.AsyncInterceptor、xunit、FluentAssertions。

---

## 文件结构

**修改：**
- `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Tw.DependencyInjection.csproj`
- `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Registration/ServiceRegistrationExecutor.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/package-charter.yaml`
- `docs/shared-packages/dotnet/Tw.DependencyInjection/README.md`

**新增：**
- `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/DynamicProxy/IInterceptorPipeline.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/DynamicProxy/IInterceptorSelector.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/DynamicProxy/AttributeInterceptorSelector.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/DynamicProxy/InterceptorPipeline.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/DynamicProxy/CastleInvocationContext.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/DynamicProxy/CastleAsyncInterceptorAdapter.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/DynamicProxy/InterceptionRegistrationPlanner.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Registration/AutofacServiceRegistrationExecutor.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Registration/AutofacServiceRegistrationExtensions.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Diagnostics/InterceptionDiagnostic.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Diagnostics/InterceptionReport.cs`
- `backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests/DynamicProxy/InterceptorPipelineTests.cs`
- `backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests/DynamicProxy/AttributeInterceptorSelectorTests.cs`
- `backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests/DynamicProxy/CastleInvocationContextTests.cs`
- `backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests/DynamicProxy/CastleInterceptionIntegrationTests.cs`
- `docs/shared-packages/dotnet/Tw.DependencyInjection/dynamic-proxy-interception.md`

## Task 1: 依赖与诊断模型

**Files:**
- Modify: `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Tw.DependencyInjection.csproj`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Diagnostics/InterceptionDiagnostic.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Diagnostics/InterceptionReport.cs`
- Test: `backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests/DynamicProxy/InterceptorPipelineTests.cs`

- [ ] **Step 1: 写失败测试**

```csharp
using FluentAssertions;
using Tw.DependencyInjection.Diagnostics;
using Xunit;

namespace Tw.DependencyInjection.Tests.DynamicProxy;

public class InterceptorPipelineTests
{
    [Fact]
    public void InterceptionReport_ExposesDiagnostics()
    {
        var item = new InterceptionDiagnostic(
            ServiceTypeName: "Sample.IOrderService",
            ImplementationTypeName: "Sample.OrderService",
            MethodName: "SubmitAsync",
            Carrier: "CastleInterfaceProxy",
            InterceptorTypeNames: ["Sample.AuditInterceptor"],
            Status: "enabled",
            Reason: null);

        new InterceptionReport([item]).Items.Should().ContainSingle();
    }
}
```

- [ ] **Step 2: 运行测试确认失败**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests/Tw.DependencyInjection.Tests.csproj --nologo`

Expected: 编译失败，诊断类型不存在。

- [ ] **Step 3: 添加包引用**

```xml
    <PackageReference Include="Autofac.Extras.DynamicProxy" />
    <PackageReference Include="Castle.Core" />
    <PackageReference Include="Castle.Core.AsyncInterceptor" />
```

- [ ] **Step 4: 新增诊断模型**

```csharp
namespace Tw.DependencyInjection.Diagnostics;

/// <summary>方法级拦截承载诊断项</summary>
public sealed record InterceptionDiagnostic(
    string ServiceTypeName,
    string ImplementationTypeName,
    string MethodName,
    string Carrier,
    IReadOnlyList<string> InterceptorTypeNames,
    string Status,
    string? Reason);

/// <summary>AOP 拦截承载诊断报告</summary>
public sealed class InterceptionReport
{
    public InterceptionReport(IReadOnlyList<InterceptionDiagnostic> items) => Items = items;
    public IReadOnlyList<InterceptionDiagnostic> Items { get; }
}
```

- [ ] **Step 5: 运行测试确认通过**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests/Tw.DependencyInjection.Tests.csproj --nologo`

Expected: PASS。

## Task 2: Pipeline 与特性 Selector

**Files:**
- Create: `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/DynamicProxy/IInterceptorPipeline.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/DynamicProxy/IInterceptorSelector.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/DynamicProxy/AttributeInterceptorSelector.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/DynamicProxy/InterceptorPipeline.cs`
- Test: `backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests/DynamicProxy/AttributeInterceptorSelectorTests.cs`
- Test: `backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests/DynamicProxy/InterceptorPipelineTests.cs`

- [ ] **Step 1: 写失败测试**

测试必须覆盖：
- `[Intercept]` 可从接口、类、方法读取。
- 方法级 `[DisableInterception]` 返回空拦截器链。
- 同一拦截器类型去重。
- `[InterceptorOrder]` 小值先执行，顺序相同按类型全名稳定排序。
- `InterceptorPipeline` 按顺序执行，并只调用目标 `ProceedAsync()` 一次。

- [ ] **Step 2: 运行测试确认失败**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests/Tw.DependencyInjection.Tests.csproj --nologo`

Expected: 编译失败，pipeline 与 selector 不存在。

- [ ] **Step 3: 实现接口**

```csharp
using System.Reflection;
using Tw.DynamicProxy.Abstractions;

namespace Tw.DependencyInjection.DynamicProxy;

public interface IInterceptorPipeline
{
    ValueTask InvokeAsync(IInvocationContext context, IReadOnlyList<IInterceptor> interceptors);
}

public interface IInterceptorSelector
{
    IReadOnlyList<Type> SelectInterceptors(Type implementationType, Type serviceType, MethodInfo method);
}
```

- [ ] **Step 4: 实现 selector 与 pipeline**

`AttributeInterceptorSelector` 从服务契约、实现类型、方法读取 `InterceptAttribute`，遇到 `DisableInterceptionAttribute` 返回空集合，最后按 `InterceptorOrderAttribute.Order` 和类型全名排序。`InterceptorPipeline` 使用递归上下文包装或内部游标，保证最后一个节点调用原始 `context.ProceedAsync()`。

- [ ] **Step 5: 运行测试确认通过**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests/Tw.DependencyInjection.Tests.csproj --nologo`

Expected: PASS。

## Task 3: Castle InvocationContext 与 AsyncInterceptor adapter

**Files:**
- Create: `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/DynamicProxy/CastleInvocationContext.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/DynamicProxy/CastleAsyncInterceptorAdapter.cs`
- Test: `backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests/DynamicProxy/CastleInvocationContextTests.cs`

- [ ] **Step 1: 写失败测试**

测试必须覆盖同步方法、`Task`、`Task<T>`、`ValueTask`、`ValueTask<T>`：
- `ProceedAsync()` 等待目标完成并写入 `ReturnValue`。
- 拦截器改写 `Arguments[i]` 后能回写到 Castle invocation。
- `Proceed()` 命中异步目标时抛出 `InvalidOperationException`，消息包含“异步目标方法”。

- [ ] **Step 2: 运行测试确认失败**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests/Tw.DependencyInjection.Tests.csproj --nologo`

Expected: 编译失败，Castle adapter 类型不存在。

- [ ] **Step 3: 实现 CastleInvocationContext**

实现必须：
- 持有 `Castle.DynamicProxy.IInvocation`。
- `Method` 优先取 `invocation.MethodInvocationTarget`，为空时取 `invocation.Method`。
- `Arguments` 初始化为 invocation 参数副本。
- `ProceedAsync()` 先把 `Arguments` 写回 invocation，再调用 `invocation.Proceed()`。
- 对 `Task<T>` 和 `ValueTask<T>` 通过反射等待并提取结果。
- `ReturnValue` 被拦截器修改后写回 `invocation.ReturnValue`。

- [ ] **Step 4: 实现 CastleAsyncInterceptorAdapter**

Adapter 通过 DI 获取 `IInterceptorSelector`、`IInterceptorPipeline` 与 `IServiceProvider`，为当前方法解析拦截器实例并调用 pipeline。若 selector 返回空，直接 `invocation.Proceed()`。

- [ ] **Step 5: 运行测试确认通过**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests/Tw.DependencyInjection.Tests.csproj --nologo`

Expected: PASS。

## Task 4: Autofac-native 注册执行器与 Castle 承载

**Files:**
- Create: `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/DynamicProxy/InterceptionRegistrationPlanner.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Registration/AutofacServiceRegistrationExecutor.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Registration/AutofacServiceRegistrationExtensions.cs`
- Test: `backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests/DynamicProxy/CastleInterceptionIntegrationTests.cs`

- [ ] **Step 1: 写失败集成测试**

测试服务通过接口解析：

```csharp
public interface IAuditedOrderService { Task<string> SubmitAsync(string id); }

[Intercept(typeof(AuditInterceptor))]
public sealed class AuditedOrderService : IAuditedOrderService, IScopedDependency
{
    public Task<string> SubmitAsync(string id) => Task.FromResult(id);
}
```

断言：
- `IAuditedOrderService.SubmitAsync("A")` 触发 `AuditInterceptor`。
- 拦截器可把 `Arguments[0]` 改为 `"B"` 并修改 `ReturnValue`。
- `InterceptionReport` 包含 `CastleInterfaceProxy`。

- [ ] **Step 2: 运行测试确认失败**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests/Tw.DependencyInjection.Tests.csproj --nologo`

Expected: 测试失败，服务未被 Castle proxy 包装。

- [ ] **Step 3: 实现承载规划**

`InterceptionRegistrationPlanner` 基于 P2 `ServiceCandidate` 与 selector 判定：
- 契约是接口时使用 `CastleInterfaceProxy`。
- 无接口暴露且实现类型非 sealed、目标方法 virtual 时使用 `CastleClassProxy`。
- 不可代理方法写入 `InterceptionReport`，状态为 `skipped`。

- [ ] **Step 4: 实现 AutofacServiceRegistrationExecutor**

`AutofacServiceRegistrationExecutor.Apply(ContainerBuilder builder, ServiceRegistrationPlan plan, InterceptionReport report)` 执行：
- 注册 `AttributeInterceptorSelector`、`InterceptorPipeline`、`CastleAsyncInterceptorAdapter`。
- 对非 keyed interface 契约使用 `builder.RegisterType(implementation).As(service).EnableInterfaceInterceptors().InterceptedBy(typeof(CastleAsyncInterceptorAdapter))`。
- 对可 class proxy 的实现使用 `EnableClassInterceptors().InterceptedBy(...)`。
- 对未命中拦截的候选按 P2 生命周期与契约注册，不启用 Castle。
- 对 keyed service 保持 key 语义，并为可枚举 `KeyedServiceEntry<TService>` 维持 P2 的生命周期规则。

- [ ] **Step 5: 实现 Autofac 扩展入口**

`AutofacServiceRegistrationExtensions.AddServiceRegistration(this ContainerBuilder builder, IConfiguration configuration)` 复用 P1/P2/P3 的发现、Options 规划和服务规划逻辑，最终调用 Autofac-native executor。该入口用于 P5 host 聚合的 `ConfigureContainer<ContainerBuilder>` 路径；`IServiceCollection.AddServiceRegistration` 保持现有行为，供无需 AOP 的测试与组合方式使用。

- [ ] **Step 6: 运行测试确认通过**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests/Tw.DependencyInjection.Tests.csproj --nologo`

Expected: PASS。

## Task 5: 文档与验证

**Files:**
- Modify: `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/package-charter.yaml`
- Modify: `docs/shared-packages/dotnet/Tw.DependencyInjection/README.md`
- Create: `docs/shared-packages/dotnet/Tw.DependencyInjection/dynamic-proxy-interception.md`

- [ ] **Step 1: 更新 charter 与 README**

`in_scope` 增加 Castle 动态代理、拦截器选择、方法级 pipeline 与 `InterceptionReport`。README 增加 `dynamic-proxy-interception.md` 链接。

- [ ] **Step 2: 创建 How-to**

文档必须说明：
- 业务拦截器实现 `Tw.DynamicProxy.Abstractions.IInterceptor`。
- `[Intercept]`、`[DisableInterception]`、`[InterceptorOrder]` 用法。
- Castle 承载只用于方法级调用。
- Middleware、Minimal API 和 gRPC 不进入统一 AOP。

- [ ] **Step 3: 运行验证**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests/Tw.DependencyInjection.Tests.csproj --nologo`

Expected: PASS。

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Tw.Core.Tests.csproj --nologo`

Expected: PASS。
