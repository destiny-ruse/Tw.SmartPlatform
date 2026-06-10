# P6 MVC 承载 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 新增 `Tw.AspNetCore.Mvc` 包，承载 MVC/Razor Page 专属能力、MVC Filter AOP adapter，并把 `HttpContextCancellationTokenProvider` 从 host 包迁移进 MVC 包。

**Architecture:** `Tw.AspNetCore.Mvc` 引用 `Tw.AspNetCore`，消费 P4 的 `IInterceptorPipeline`。Controller action 与 Razor Page handler 通过 Filter 创建 `IInvocationContext`，不启用 Castle class proxy。Middleware、Minimal API endpoint 与 gRPC 不进入 MVC adapter。

**Tech Stack:** C# / .NET 10、ASP.NET Core MVC、xunit、FluentAssertions。

---

## 文件结构

**新增项目：**
- `backend/dotnet/BuildingBlocks/src/Tw.AspNetCore.Mvc/Tw.AspNetCore.Mvc.csproj`
- `backend/dotnet/BuildingBlocks/src/Tw.AspNetCore.Mvc/package-charter.yaml`
- `backend/dotnet/BuildingBlocks/tests/Tw.AspNetCore.Mvc.Tests/Tw.AspNetCore.Mvc.Tests.csproj`

**迁移：**
- Move: `backend/dotnet/BuildingBlocks/src/Tw.AspNetCore/Context/*` → `backend/dotnet/BuildingBlocks/src/Tw.AspNetCore.Mvc/Context/*`

**新增：**
- `backend/dotnet/BuildingBlocks/src/Tw.AspNetCore.Mvc/DependencyInjection/MvcIntegrationServiceCollectionExtensions.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.AspNetCore.Mvc/DynamicProxy/MvcInvocationContext.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.AspNetCore.Mvc/DynamicProxy/TwActionInterceptionFilter.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.AspNetCore.Mvc/DynamicProxy/TwPageInterceptionFilter.cs`
- `backend/dotnet/BuildingBlocks/tests/Tw.AspNetCore.Mvc.Tests/DynamicProxy/MvcInvocationContextTests.cs`
- `backend/dotnet/BuildingBlocks/tests/Tw.AspNetCore.Mvc.Tests/DynamicProxy/MvcInterceptionFilterTests.cs`
- `docs/shared-packages/dotnet/Tw.AspNetCore.Mvc/README.md`
- `docs/shared-packages/dotnet/Tw.AspNetCore.Mvc/mvc-interception.md`

**修改：**
- `backend/dotnet/Tw.SmartPlatform.slnx`
- `backend/dotnet/BuildingBlocks/src/Tw.AspNetCore/package-charter.yaml`
- `docs/shared-packages/dotnet/README.md`
- `docs/shared-packages/dotnet/Tw.AspNetCore/README.md`

## Task 1: 项目脚手架与 HTTP provider 迁移

**Files:**
- Create: `backend/dotnet/BuildingBlocks/src/Tw.AspNetCore.Mvc/Tw.AspNetCore.Mvc.csproj`
- Create: `backend/dotnet/BuildingBlocks/tests/Tw.AspNetCore.Mvc.Tests/Tw.AspNetCore.Mvc.Tests.csproj`
- Move: `backend/dotnet/BuildingBlocks/src/Tw.AspNetCore/Context/*`
- Modify: `backend/dotnet/Tw.SmartPlatform.slnx`
- Test: `backend/dotnet/BuildingBlocks/tests/Tw.AspNetCore.Mvc.Tests/Context/HttpContextCancellationTokenProviderTests.cs`

- [ ] **Step 1: 写失败迁移测试**

测试命名空间使用 `Tw.AspNetCore.Mvc.Context`，断言 `HttpContextCancellationTokenProvider` 从 `HttpContext.RequestAborted` 读取取消令牌。

- [ ] **Step 2: 运行测试确认失败**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.AspNetCore.Mvc.Tests/Tw.AspNetCore.Mvc.Tests.csproj --nologo`

Expected: 项目或类型不存在。

- [ ] **Step 3: 新建项目**

生产项目使用 `Microsoft.NET.Sdk.Web`、`OutputType=Library`、`IsPackable=true`，引用 `..\Tw.AspNetCore\Tw.AspNetCore.csproj`。测试项目引用 `Tw.AspNetCore.Mvc`、`Microsoft.NET.Test.Sdk`、`xunit`、`FluentAssertions`。

- [ ] **Step 4: 移动 Context 文件并改命名空间**

把 `Tw.AspNetCore.Context` 改为 `Tw.AspNetCore.Mvc.Context`。删除 host 包聚合入口对 `AddHttpContextCancellationTokenProvider` 的调用。

- [ ] **Step 5: 登记解决方案并运行测试**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.AspNetCore.Mvc.Tests/Tw.AspNetCore.Mvc.Tests.csproj --nologo`

Expected: PASS。

## Task 2: MVC InvocationContext

**Files:**
- Create: `backend/dotnet/BuildingBlocks/src/Tw.AspNetCore.Mvc/DynamicProxy/MvcInvocationContext.cs`
- Test: `backend/dotnet/BuildingBlocks/tests/Tw.AspNetCore.Mvc.Tests/DynamicProxy/MvcInvocationContextTests.cs`

- [ ] **Step 1: 写失败测试**

测试必须覆盖：
- `Arguments` 按 action 参数声明顺序从 `ActionExecutingContext.ActionArguments` 物化。
- 修改 `Arguments[i]` 后，`ProceedAsync()` 前回写到 `ActionArguments[参数名]`。
- `ArgumentsByName` 为只读视图。
- `Method` 为 action 对应 `MethodInfo`。

- [ ] **Step 2: 运行测试确认失败**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.AspNetCore.Mvc.Tests/Tw.AspNetCore.Mvc.Tests.csproj --nologo`

Expected: 编译失败，`MvcInvocationContext` 不存在。

- [ ] **Step 3: 实现 MvcInvocationContext**

实现 `IInvocationContext`，构造参数包含 `ActionExecutingContext` 和 `ActionExecutionDelegate`。无法建立所有参数名映射时抛出 `InvalidOperationException`，消息包含 action 名和缺失参数名。

- [ ] **Step 4: 运行测试确认通过**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.AspNetCore.Mvc.Tests/Tw.AspNetCore.Mvc.Tests.csproj --nologo`

Expected: PASS。

## Task 3: MVC Filter 与注册入口

**Files:**
- Create: `backend/dotnet/BuildingBlocks/src/Tw.AspNetCore.Mvc/DynamicProxy/TwActionInterceptionFilter.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.AspNetCore.Mvc/DependencyInjection/MvcIntegrationServiceCollectionExtensions.cs`
- Test: `backend/dotnet/BuildingBlocks/tests/Tw.AspNetCore.Mvc.Tests/DynamicProxy/MvcInterceptionFilterTests.cs`

- [ ] **Step 1: 写失败测试**

构造一个 fake action 调用，注册测试拦截器，断言 filter 调用 P4 pipeline，且拦截器修改 `Arguments` 后 action 参数值变化。

- [ ] **Step 2: 运行测试确认失败**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.AspNetCore.Mvc.Tests/Tw.AspNetCore.Mvc.Tests.csproj --nologo`

Expected: filter 不存在或未调用 pipeline。

- [ ] **Step 3: 实现 Filter**

`TwActionInterceptionFilter` 实现 `IAsyncActionFilter`，从 DI 获取 `IInterceptorSelector` 与 `IInterceptorPipeline`。selector 为空时直接执行 `next()`；命中拦截器时创建 `MvcInvocationContext` 并调用 pipeline。

- [ ] **Step 4: 实现注册入口**

```csharp
namespace Tw.AspNetCore.Mvc;

public static class MvcIntegrationServiceCollectionExtensions
{
    public static IServiceCollection AddMvcIntegration(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddHttpContextCancellationTokenProvider();
        services.Configure<MvcOptions>(options =>
            options.Filters.Add<TwActionInterceptionFilter>());
        return services;
    }
}
```

- [ ] **Step 5: 运行测试确认通过**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.AspNetCore.Mvc.Tests/Tw.AspNetCore.Mvc.Tests.csproj --nologo`

Expected: PASS。

## Task 4: Charter、文档与索引

**Files:**
- Create: `backend/dotnet/BuildingBlocks/src/Tw.AspNetCore.Mvc/package-charter.yaml`
- Modify: `backend/dotnet/BuildingBlocks/src/Tw.AspNetCore/package-charter.yaml`
- Create: `docs/shared-packages/dotnet/Tw.AspNetCore.Mvc/README.md`
- Create: `docs/shared-packages/dotnet/Tw.AspNetCore.Mvc/mvc-interception.md`
- Modify: `docs/shared-packages/dotnet/README.md`
- Modify: `docs/shared-packages/dotnet/Tw.AspNetCore/README.md`

- [ ] **Step 1: 新增 MVC charter**

声明 `Tw.AspNetCore.Mvc` 负责 web/webapi 专属能力、HTTP cancellation provider 与 MVC Filter AOP adapter；`out_of_scope` 明确不承载 gRPC 和 Middleware adapter。

- [ ] **Step 2: 收窄 host charter**

从 `Tw.AspNetCore` 的 `in_scope` 移除中间件与过滤器、模型绑定与结果封装、Web 层横切关注点、HTTP cancellation provider；`out_of_scope` 声明这些能力归 `Tw.AspNetCore.Mvc`。

- [ ] **Step 3: 新增 How-to 与索引**

`mvc-interception.md` 必须说明 `AddMvcIntegration()`、MVC Filter adapter、参数回写规则、Middleware/Minimal API/gRPC 排除边界。

- [ ] **Step 4: 运行验证**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.AspNetCore.Mvc.Tests/Tw.AspNetCore.Mvc.Tests.csproj --nologo`

Expected: PASS。

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.AspNetCore.Tests/Tw.AspNetCore.Tests.csproj --nologo`

Expected: PASS。

