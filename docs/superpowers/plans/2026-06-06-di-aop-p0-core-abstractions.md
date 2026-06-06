# P0 抽象地基 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 在 `Tw.Core` 落地 DI / 配置 Options / AOP 的全部框架无关抽象（标记接口、特性、契约、基类），并把历史命名空间 `Tw.Core.Configuration` / `Tw.Core.Reflection` 迁移到 `.Abstractions` 规约。

**Architecture:** 本阶段只产出纯抽象，不引用 Autofac / Castle / ASP.NET Core。所有类型落在 `Tw.Core` 程序集（`RootNamespace = Tw`）的四个能力命名空间：`Tw.DependencyInjection.Abstractions`、`Tw.Configuration.Abstractions`、`Tw.DynamicProxy.Abstractions`、`Tw.Reflection`。命名空间与文件夹一一对应。后续 P1–P7 的执行引擎与承载包都消费这些抽象。

**Tech Stack:** C# / .NET 10、xunit、FluentAssertions、中央包管理（CPM）、NuGet 锁定文件。

**对应 spec：** [docs/superpowers/specs/2026-06-06-di-options-aop-design.md](../specs/2026-06-06-di-options-aop-design.md)，阶段 P0。

**前置规范（实现前必读）：**
- [docs/engineering-standards/03-project-and-code/language-specific/dotnet-core.md](../../engineering-standards/03-project-and-code/language-specific/dotnet-core.md)（命名空间=RootNamespace+文件夹、`.Abstractions` 后缀、XML 文档注释）
- [docs/engineering-standards/03-project-and-code/shared-package-charter.md](../../engineering-standards/03-project-and-code/shared-package-charter.md)（charter 字段、采纳前破坏性变更、索引联动）

**通用约定：**
- 公开类型与公开成员必须带 DocFX XML 文档注释（`<summary>` 等）。
- 测试命名 `成员_预期[_条件]`，命名空间 `Tw.Core.Tests.<Area>`。
- 本阶段多数类型是声明，"测试先失败" 表现为测试项目**编译失败**（类型/命名空间不存在）；这就是该步骤期望的 RED 状态。
- 构建命令：`dotnet build backend/dotnet/BuildingBlocks/src/Tw.Core/Tw.Core.csproj`
- 测试命令：`dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Tw.Core.Tests.csproj`
- 当前分支：`feat/di-options-aop`。

---

## 文件结构

**迁移（修改现有文件）：**
- `backend/dotnet/BuildingBlocks/src/Tw.Core/Reflection/ITypeFinder.cs` 等 4 个文件：命名空间 `Tw.Core.Reflection` → `Tw.Reflection`
- `backend/dotnet/BuildingBlocks/src/Tw.Core/Configuration/IConfigurableOptions.cs` → 移动到 `Configuration/Abstractions/`，命名空间 → `Tw.Configuration.Abstractions`
- `backend/dotnet/BuildingBlocks/src/Tw.Core/Tw.Core.csproj`：新增包引用
- `backend/dotnet/BuildingBlocks/src/Tw.Core/package-charter.yaml`：公开能力与依赖边界
- `docs/shared-packages/dotnet/Tw.Core/README.md`：索引新增抽象能力

**新增（DI 抽象，`DependencyInjection/Abstractions/`，命名空间 `Tw.DependencyInjection.Abstractions`）：**
- `DependencyLifetime.cs`、`ITransientDependency.cs`、`IScopedDependency.cs`、`ISingletonDependency.cs`
- `ServiceRegistrationAttribute.cs`、`DisableServiceRegistrationAttribute.cs`、`ExposeServicesAttribute.cs`、`ExposeKeyedServiceAttribute.cs`、`ServicePriorityAttribute.cs`、`TwAssemblyPriorityAttribute.cs`
- `KeyedServiceEntry.cs`

**新增（AOP 抽象，`DynamicProxy/Abstractions/`，命名空间 `Tw.DynamicProxy.Abstractions`）：**
- `IInvocationContext.cs`、`IInterceptor.cs`、`InterceptorBase.cs`、`SyncInterceptorBase.cs`
- `InterceptAttribute.cs`、`DisableInterceptionAttribute.cs`、`InterceptorOrderAttribute.cs`

**新增（Options 抽象，`Configuration/Abstractions/`，命名空间 `Tw.Configuration.Abstractions`）：**
- `IConfigurableOptionsOfT.cs`（`IConfigurableOptions<TOptions>`）
- `OptionsSectionAttribute.cs`、`OptionsNameAttribute.cs`、`DisableOptionsBindingAttribute.cs`、`SensitiveConfigurationAttribute.cs`、`OptionsValidatorAttribute.cs`

**新增测试（`backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/`）：**
- `Reflection/ReflectionNamespaceTests.cs`
- `DependencyInjection/LifecycleMarkerTests.cs`、`DependencyInjection/RegistrationAttributeTests.cs`、`DependencyInjection/KeyedServiceEntryTests.cs`
- `Configuration/OptionsAbstractionsTests.cs`
- `DynamicProxy/InterceptionAttributeTests.cs`、`DynamicProxy/SyncInterceptorBaseTests.cs`、`DynamicProxy/InterceptorBaseTests.cs`
- `DynamicProxy/Fakes/FakeInvocationContext.cs`（测试替身）

---

## Task 1: 迁移 Reflection 命名空间到 `Tw.Reflection`

**Files:**
- Modify: `backend/dotnet/BuildingBlocks/src/Tw.Core/Reflection/ITypeFinder.cs`
- Modify: `backend/dotnet/BuildingBlocks/src/Tw.Core/Reflection/TypeFinder.cs`
- Modify: `backend/dotnet/BuildingBlocks/src/Tw.Core/Reflection/ReflectionCache.cs`
- Modify: `backend/dotnet/BuildingBlocks/src/Tw.Core/Reflection/TypeFinderExtensions.cs`
- Test: `backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Reflection/ReflectionNamespaceTests.cs`

- [ ] **Step 1: 写失败测试**

新建 `backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Reflection/ReflectionNamespaceTests.cs`：

```csharp
using FluentAssertions;
using Tw.Reflection;
using Xunit;

namespace Tw.Core.Tests.Reflection;

public class ReflectionNamespaceTests
{
    [Fact]
    public void TypeFinder_LivesIn_TwReflectionNamespace()
    {
        typeof(TypeFinder).Namespace.Should().Be("Tw.Reflection");
    }

    [Fact]
    public void ITypeFinder_LivesIn_TwReflectionNamespace()
    {
        typeof(ITypeFinder).Namespace.Should().Be("Tw.Reflection");
    }
}
```

- [ ] **Step 2: 运行测试确认失败**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Tw.Core.Tests.csproj`
Expected: 编译失败，`error CS0234: 命名空间 "Tw" 中不存在类型或命名空间名 "Reflection"`。

- [ ] **Step 3: 修改四个文件的命名空间**

把这四个文件的 `namespace Tw.Core.Reflection;` 全部改为 `namespace Tw.Reflection;`（仅改命名空间声明行，其余内容不动）：
- `ITypeFinder.cs`
- `TypeFinder.cs`
- `ReflectionCache.cs`
- `TypeFinderExtensions.cs`

- [ ] **Step 4: 运行测试确认通过**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Tw.Core.Tests.csproj`
Expected: PASS（含既有 Context 测试，确认迁移未破坏其他代码）。

- [ ] **Step 5: 提交**

```bash
git add backend/dotnet/BuildingBlocks/src/Tw.Core/Reflection backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Reflection
git commit -m "refactor(core): migrate reflection namespace to Tw.Reflection"
```

---

## Task 2: 迁移 Configuration 抽象到 `Tw.Configuration.Abstractions`

**Files:**
- Move: `backend/dotnet/BuildingBlocks/src/Tw.Core/Configuration/IConfigurableOptions.cs` → `backend/dotnet/BuildingBlocks/src/Tw.Core/Configuration/Abstractions/IConfigurableOptions.cs`
- Test: `backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Configuration/OptionsAbstractionsTests.cs`

- [ ] **Step 1: 写失败测试**

新建 `backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Configuration/OptionsAbstractionsTests.cs`：

```csharp
using FluentAssertions;
using Tw.Configuration.Abstractions;
using Xunit;

namespace Tw.Core.Tests.Configuration;

public class OptionsAbstractionsTests
{
    [Fact]
    public void IConfigurableOptions_LivesIn_AbstractionsNamespace()
    {
        typeof(IConfigurableOptions).Namespace.Should().Be("Tw.Configuration.Abstractions");
    }
}
```

- [ ] **Step 2: 运行测试确认失败**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Tw.Core.Tests.csproj`
Expected: 编译失败，`error CS0234: 命名空间 "Tw.Configuration" 中不存在 "Abstractions"`。

- [ ] **Step 3: 移动文件并改命名空间**

```bash
git mv backend/dotnet/BuildingBlocks/src/Tw.Core/Configuration/IConfigurableOptions.cs backend/dotnet/BuildingBlocks/src/Tw.Core/Configuration/Abstractions/IConfigurableOptions.cs
```

把移动后的 `Configuration/Abstractions/IConfigurableOptions.cs` 内容改为：

```csharp
namespace Tw.Configuration.Abstractions;

/// <summary>
/// 将类型标记为可参与配置绑定的选项对象
/// </summary>
public interface IConfigurableOptions;
```

- [ ] **Step 4: 运行测试确认通过**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Tw.Core.Tests.csproj`
Expected: PASS。

- [ ] **Step 5: 提交**

```bash
git add backend/dotnet/BuildingBlocks/src/Tw.Core/Configuration backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Configuration
git commit -m "refactor(core): migrate IConfigurableOptions to Tw.Configuration.Abstractions"
```

---

## Task 3: 为 Tw.Core 增加 Configuration / Options 包引用

**Files:**
- Modify: `backend/dotnet/BuildingBlocks/src/Tw.Core/Tw.Core.csproj`

`IConfigurableOptions<TOptions>`（Task 7）需要 `Microsoft.Extensions.Configuration.IConfiguration`，Options 校验抽象需要 `Microsoft.Extensions.Options`。版本由中央包管理提供（[Build/Packages.Microsoft.props](../../../backend/dotnet/Build/Packages.Microsoft.props)），此处只加无版本的 `PackageReference`。

- [ ] **Step 1: 修改 csproj**

把 `backend/dotnet/BuildingBlocks/src/Tw.Core/Tw.Core.csproj` 的 `<ItemGroup>` 改为：

```xml
  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" />
    <PackageReference Include="Microsoft.Extensions.Configuration.Abstractions" />
    <PackageReference Include="Microsoft.Extensions.Options" />
  </ItemGroup>
```

- [ ] **Step 2: 还原并构建确认通过**

Run: `dotnet build backend/dotnet/BuildingBlocks/src/Tw.Core/Tw.Core.csproj`
Expected: 构建成功，`packages.lock.json` 更新（出现新增包条目）。

- [ ] **Step 3: 提交**

```bash
git add backend/dotnet/BuildingBlocks/src/Tw.Core/Tw.Core.csproj backend/dotnet/BuildingBlocks/src/Tw.Core/packages.lock.json
git commit -m "build(core): add configuration and options package references"
```

---

## Task 4: 生命周期标记接口与 `DependencyLifetime`

**Files:**
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/DependencyInjection/Abstractions/DependencyLifetime.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/DependencyInjection/Abstractions/ITransientDependency.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/DependencyInjection/Abstractions/IScopedDependency.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/DependencyInjection/Abstractions/ISingletonDependency.cs`
- Test: `backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/DependencyInjection/LifecycleMarkerTests.cs`

> 设计说明：生命周期用自有枚举 `DependencyLifetime` 表达，避免把 `Microsoft.Extensions.DependencyInjection.ServiceLifetime`（`Singleton=0`）的 0 值当作"未设置"哨兵。引擎层（P2）负责把 `DependencyLifetime` 映射到容器生命周期。

- [ ] **Step 1: 写失败测试**

新建 `backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/DependencyInjection/LifecycleMarkerTests.cs`：

```csharp
using FluentAssertions;
using Tw.DependencyInjection.Abstractions;
using Xunit;

namespace Tw.Core.Tests.DependencyInjection;

public class LifecycleMarkerTests
{
    [Fact]
    public void Markers_LiveIn_AbstractionsNamespace()
    {
        typeof(ITransientDependency).Namespace.Should().Be("Tw.DependencyInjection.Abstractions");
        typeof(IScopedDependency).Namespace.Should().Be("Tw.DependencyInjection.Abstractions");
        typeof(ISingletonDependency).Namespace.Should().Be("Tw.DependencyInjection.Abstractions");
    }

    [Fact]
    public void DependencyLifetime_HasThreeMembers()
    {
        Enum.GetNames<DependencyLifetime>().Should()
            .BeEquivalentTo("Transient", "Scoped", "Singleton");
    }
}
```

- [ ] **Step 2: 运行测试确认失败**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Tw.Core.Tests.csproj`
Expected: 编译失败，`Tw.DependencyInjection.Abstractions` 不存在。

- [ ] **Step 3: 实现四个类型**

`DependencyLifetime.cs`：

```csharp
namespace Tw.DependencyInjection.Abstractions;

/// <summary>
/// 自动注册服务的生命周期
/// </summary>
public enum DependencyLifetime
{
    /// <summary>每次解析创建新实例</summary>
    Transient,

    /// <summary>每个作用域一个实例</summary>
    Scoped,

    /// <summary>容器全局单例</summary>
    Singleton,
}
```

`ITransientDependency.cs`：

```csharp
namespace Tw.DependencyInjection.Abstractions;

/// <summary>
/// 标记类型按瞬时（transient）生命周期参与自动注册
/// </summary>
public interface ITransientDependency;
```

`IScopedDependency.cs`：

```csharp
namespace Tw.DependencyInjection.Abstractions;

/// <summary>
/// 标记类型按作用域（scoped）生命周期参与自动注册
/// </summary>
public interface IScopedDependency;
```

`ISingletonDependency.cs`：

```csharp
namespace Tw.DependencyInjection.Abstractions;

/// <summary>
/// 标记类型按单例（singleton）生命周期参与自动注册
/// </summary>
public interface ISingletonDependency;
```

- [ ] **Step 4: 运行测试确认通过**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Tw.Core.Tests.csproj`
Expected: PASS。

- [ ] **Step 5: 提交**

```bash
git add backend/dotnet/BuildingBlocks/src/Tw.Core/DependencyInjection backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/DependencyInjection/LifecycleMarkerTests.cs
git commit -m "feat(core): add lifecycle marker interfaces and DependencyLifetime"
```

---

## Task 5: 注册与暴露特性

**Files:**
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/DependencyInjection/Abstractions/ServiceRegistrationAttribute.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/DependencyInjection/Abstractions/DisableServiceRegistrationAttribute.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/DependencyInjection/Abstractions/ExposeServicesAttribute.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/DependencyInjection/Abstractions/ExposeKeyedServiceAttribute.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/DependencyInjection/Abstractions/ServicePriorityAttribute.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/DependencyInjection/Abstractions/TwAssemblyPriorityAttribute.cs`
- Test: `backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/DependencyInjection/RegistrationAttributeTests.cs`

> 设计说明：`ServiceRegistrationAttribute` 不含 `Replace` 属性（spec 明确放弃显式替换）。生命周期经构造参数承载——可空枚举不能作为 attribute 命名参数（CS0655），因此用 `[ServiceRegistration(DependencyLifetime.Scoped)]` 而非 `[ServiceRegistration(Lifetime = ...)]`；`Priority` 为 `int` 命名参数。

- [ ] **Step 1: 写失败测试**

新建 `backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/DependencyInjection/RegistrationAttributeTests.cs`：

```csharp
using System.Reflection;
using FluentAssertions;
using Tw.DependencyInjection.Abstractions;
using Xunit;

namespace Tw.Core.Tests.DependencyInjection;

public class RegistrationAttributeTests
{
    [ServiceRegistration(DependencyLifetime.Scoped, Priority = 10)]
    private sealed class WithLifetime;

    [ServiceRegistration]
    private sealed class WithoutLifetime;

    [ExposeServices(typeof(IContract), IncludeSelf = true)]
    private sealed class Exposing;

    [ExposeKeyedService(typeof(IContract), "wechat")]
    private sealed class Keyed;

    private interface IContract;

    [Fact]
    public void ServiceRegistration_HasNoReplaceMember()
    {
        typeof(ServiceRegistrationAttribute).GetProperty("Replace").Should().BeNull();
        typeof(ServiceRegistrationAttribute).GetField("Replace").Should().BeNull();
    }

    [Fact]
    public void ServiceRegistration_CarriesLifetimeAndPriority()
    {
        var attr = typeof(WithLifetime).GetCustomAttribute<ServiceRegistrationAttribute>()!;
        attr.Lifetime.Should().Be(DependencyLifetime.Scoped);
        attr.Priority.Should().Be(10);
    }

    [Fact]
    public void ServiceRegistration_LifetimeIsNull_WhenNotSpecified()
    {
        var attr = typeof(WithoutLifetime).GetCustomAttribute<ServiceRegistrationAttribute>()!;
        attr.Lifetime.Should().BeNull();
    }

    [Fact]
    public void ExposeServices_CarriesTypesAndIncludeSelf()
    {
        var attr = typeof(Exposing).GetCustomAttribute<ExposeServicesAttribute>()!;
        attr.ServiceTypes.Should().ContainSingle().Which.Should().Be(typeof(IContract));
        attr.IncludeSelf.Should().BeTrue();
    }

    [Fact]
    public void ExposeKeyedService_CarriesContractAndKey()
    {
        var attr = typeof(Keyed).GetCustomAttribute<ExposeKeyedServiceAttribute>()!;
        attr.ServiceType.Should().Be(typeof(IContract));
        attr.Key.Should().Be("wechat");
    }

    [Fact]
    public void TwAssemblyPriority_TargetsAssembly()
    {
        var usage = typeof(TwAssemblyPriorityAttribute).GetCustomAttribute<AttributeUsageAttribute>()!;
        usage.ValidOn.Should().Be(AttributeTargets.Assembly);
    }

    [Fact]
    public void ExposeServices_AllowsMultiple()
    {
        var usage = typeof(ExposeServicesAttribute).GetCustomAttribute<AttributeUsageAttribute>()!;
        usage.AllowMultiple.Should().BeTrue();
    }
}
```

- [ ] **Step 2: 运行测试确认失败**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Tw.Core.Tests.csproj`
Expected: 编译失败，特性类型不存在。

- [ ] **Step 3: 实现六个特性**

`ServiceRegistrationAttribute.cs`：

```csharp
namespace Tw.DependencyInjection.Abstractions;

/// <summary>
/// 声明类型参与自动注册，并可指定生命周期与类型级优先级
/// </summary>
/// <remarks>
/// 本特性不承载服务替换语义；同一契约多个候选由优先级单实现仲裁决定唯一胜者。
/// </remarks>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class ServiceRegistrationAttribute : Attribute
{
    /// <summary>使用默认规则注册，生命周期由标记接口决定</summary>
    public ServiceRegistrationAttribute()
    {
    }

    /// <summary>使用显式生命周期注册</summary>
    /// <param name="lifetime">服务生命周期</param>
    public ServiceRegistrationAttribute(DependencyLifetime lifetime)
    {
        Lifetime = lifetime;
    }

    /// <summary>显式生命周期；为 <see langword="null"/> 时回退到标记接口</summary>
    public DependencyLifetime? Lifetime { get; }

    /// <summary>类型级显式优先级，参与单实现仲裁</summary>
    public int Priority { get; set; }
}
```

`DisableServiceRegistrationAttribute.cs`：

```csharp
namespace Tw.DependencyInjection.Abstractions;

/// <summary>
/// 标记类型跳过自动注册
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class DisableServiceRegistrationAttribute : Attribute;
```

`ExposeServicesAttribute.cs`：

```csharp
namespace Tw.DependencyInjection.Abstractions;

/// <summary>
/// 显式声明类型对外暴露的服务契约
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class ExposeServicesAttribute : Attribute
{
    /// <summary>声明要暴露的契约类型</summary>
    /// <param name="serviceTypes">对外暴露的契约类型</param>
    public ExposeServicesAttribute(params Type[] serviceTypes)
    {
        ServiceTypes = serviceTypes;
    }

    /// <summary>对外暴露的契约类型</summary>
    public IReadOnlyList<Type> ServiceTypes { get; }

    /// <summary>是否同时暴露实现类自身类型</summary>
    public bool IncludeSelf { get; set; }
}
```

`ExposeKeyedServiceAttribute.cs`：

```csharp
namespace Tw.DependencyInjection.Abstractions;

/// <summary>
/// 声明类型以指定 key 注册为 keyed service
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class ExposeKeyedServiceAttribute : Attribute
{
    /// <summary>声明 keyed 注册</summary>
    /// <param name="serviceType">服务契约类型</param>
    /// <param name="key">稳定 key，不可为空</param>
    public ExposeKeyedServiceAttribute(Type serviceType, object key)
    {
        ServiceType = serviceType;
        Key = key;
    }

    /// <summary>服务契约类型</summary>
    public Type ServiceType { get; }

    /// <summary>注册 key</summary>
    public object Key { get; }
}
```

`ServicePriorityAttribute.cs`：

```csharp
namespace Tw.DependencyInjection.Abstractions;

/// <summary>
/// 声明类型级显式注册优先级
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class ServicePriorityAttribute : Attribute
{
    /// <summary>声明类型级优先级</summary>
    /// <param name="priority">优先级数值，越大优先级越高</param>
    public ServicePriorityAttribute(int priority)
    {
        Priority = priority;
    }

    /// <summary>类型级优先级</summary>
    public int Priority { get; }
}
```

`TwAssemblyPriorityAttribute.cs`：

```csharp
namespace Tw.DependencyInjection.Abstractions;

/// <summary>
/// 声明程序集级显式注册优先级
/// </summary>
/// <remarks>配置 <c>Tw:DependencyInjection:AssemblyPriorities</c> 优先于本特性。</remarks>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
public sealed class TwAssemblyPriorityAttribute : Attribute
{
    /// <summary>声明程序集级优先级</summary>
    /// <param name="priority">优先级数值，越大优先级越高</param>
    public TwAssemblyPriorityAttribute(int priority)
    {
        Priority = priority;
    }

    /// <summary>程序集级优先级</summary>
    public int Priority { get; }
}
```

- [ ] **Step 4: 运行测试确认通过**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Tw.Core.Tests.csproj`
Expected: PASS。

- [ ] **Step 5: 提交**

```bash
git add backend/dotnet/BuildingBlocks/src/Tw.Core/DependencyInjection backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/DependencyInjection/RegistrationAttributeTests.cs
git commit -m "feat(core): add service registration and expose attributes"
```

---

## Task 6: `KeyedServiceEntry<TService>`

**Files:**
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/DependencyInjection/Abstractions/KeyedServiceEntry.cs`
- Test: `backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/DependencyInjection/KeyedServiceEntryTests.cs`

> 设计说明：消费方注入 `IEnumerable<KeyedServiceEntry<TService>>` 枚举某契约的全部 keyed 实现及其 key（spec「Keyed Service」节）。本类型是抽象，落在 `Tw.Core`，业务无需引用引擎包。

- [ ] **Step 1: 写失败测试**

新建 `backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/DependencyInjection/KeyedServiceEntryTests.cs`：

```csharp
using FluentAssertions;
using Tw.DependencyInjection.Abstractions;
using Xunit;

namespace Tw.Core.Tests.DependencyInjection;

public class KeyedServiceEntryTests
{
    private interface IProvider;
    private sealed class Provider : IProvider;

    [Fact]
    public void Entry_CarriesKeyAndService()
    {
        IProvider provider = new Provider();

        var entry = new KeyedServiceEntry<IProvider>("wechat", provider);

        entry.Key.Should().Be("wechat");
        entry.Service.Should().BeSameAs(provider);
    }

    [Fact]
    public void Entry_LivesIn_AbstractionsNamespace()
    {
        typeof(KeyedServiceEntry<IProvider>).Namespace.Should().Be("Tw.DependencyInjection.Abstractions");
    }
}
```

- [ ] **Step 2: 运行测试确认失败**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Tw.Core.Tests.csproj`
Expected: 编译失败，`KeyedServiceEntry` 不存在。

- [ ] **Step 3: 实现类型**

`KeyedServiceEntry.cs`：

```csharp
namespace Tw.DependencyInjection.Abstractions;

/// <summary>
/// 携带 key 元数据的 keyed 服务条目，用于枚举某契约的全部 keyed 实现
/// </summary>
/// <typeparam name="TService">服务契约类型</typeparam>
/// <param name="Key">注册时声明的稳定 key</param>
/// <param name="Service">该 key 对应的服务实例</param>
public readonly record struct KeyedServiceEntry<TService>(object Key, TService Service)
    where TService : notnull;
```

- [ ] **Step 4: 运行测试确认通过**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Tw.Core.Tests.csproj`
Expected: PASS。

- [ ] **Step 5: 提交**

```bash
git add backend/dotnet/BuildingBlocks/src/Tw.Core/DependencyInjection/Abstractions/KeyedServiceEntry.cs backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/DependencyInjection/KeyedServiceEntryTests.cs
git commit -m "feat(core): add KeyedServiceEntry abstraction"
```

---

## Task 7: Options 契约与特性

**Files:**
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/Configuration/Abstractions/IConfigurableOptionsOfT.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/Configuration/Abstractions/OptionsSectionAttribute.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/Configuration/Abstractions/OptionsNameAttribute.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/Configuration/Abstractions/DisableOptionsBindingAttribute.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/Configuration/Abstractions/SensitiveConfigurationAttribute.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/Configuration/Abstractions/OptionsValidatorAttribute.cs`
- Test: 追加到 `backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Configuration/OptionsAbstractionsTests.cs`

> 设计说明：`SensitiveConfigurationAttribute` 单特性同时支持类与属性（spec「敏感配置」节）。`OptionsValidatorAttribute` 与 `Microsoft.Extensions.Options` 内置同名属性命名空间不同，不冲突。

- [ ] **Step 1: 写失败测试**

把 `Configuration/OptionsAbstractionsTests.cs` 替换为：

```csharp
using System.Reflection;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Tw.Configuration.Abstractions;
using Xunit;

namespace Tw.Core.Tests.Configuration;

public class OptionsAbstractionsTests
{
    private sealed class CacheOptions : IConfigurableOptions<CacheOptions>
    {
        public int Ttl { get; set; }

        public void PostConfigure(CacheOptions options, IConfiguration configuration)
        {
            if (options.Ttl == 0)
            {
                options.Ttl = 60;
            }
        }
    }

    [Fact]
    public void IConfigurableOptions_LivesIn_AbstractionsNamespace()
    {
        typeof(IConfigurableOptions).Namespace.Should().Be("Tw.Configuration.Abstractions");
    }

    [Fact]
    public void GenericOptions_Implements_NonGenericMarker()
    {
        typeof(CacheOptions).Should().BeAssignableTo<IConfigurableOptions>();
    }

    [Fact]
    public void PostConfigure_FillsDefault_WhenUnset()
    {
        var options = new CacheOptions();
        IConfiguration configuration = new ConfigurationBuilder().Build();

        ((IConfigurableOptions<CacheOptions>)options).PostConfigure(options, configuration);

        options.Ttl.Should().Be(60);
    }

    [Fact]
    public void OptionsSection_CarriesPath()
    {
        var attr = new OptionsSectionAttribute("Tw:Cache");
        attr.Path.Should().Be("Tw:Cache");
    }

    [Fact]
    public void OptionsName_CarriesName()
    {
        new OptionsNameAttribute("primary").Name.Should().Be("primary");
    }

    [Fact]
    public void SensitiveConfiguration_TargetsClassAndProperty()
    {
        var usage = typeof(SensitiveConfigurationAttribute).GetCustomAttribute<AttributeUsageAttribute>()!;
        usage.ValidOn.Should().Be(AttributeTargets.Class | AttributeTargets.Property);
    }

    [Fact]
    public void OptionsValidator_CarriesValidatorType()
    {
        var attr = new OptionsValidatorAttribute(typeof(CacheOptions));
        attr.ValidatorType.Should().Be(typeof(CacheOptions));
    }
}
```

- [ ] **Step 2: 运行测试确认失败**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Tw.Core.Tests.csproj`
Expected: 编译失败，`IConfigurableOptions<>` 与各特性不存在。

- [ ] **Step 3: 实现契约与特性**

`IConfigurableOptionsOfT.cs`：

```csharp
using Microsoft.Extensions.Configuration;

namespace Tw.Configuration.Abstractions;

/// <summary>
/// 支持后置配置的强类型选项契约
/// </summary>
/// <typeparam name="TOptions">选项自身类型，必须等于实现类型</typeparam>
public interface IConfigurableOptions<TOptions> : IConfigurableOptions
    where TOptions : class, IConfigurableOptions
{
    /// <summary>
    /// 在绑定后补默认值、组合校验或派生非敏感字段
    /// </summary>
    /// <param name="options">已绑定的选项实例</param>
    /// <param name="configuration">该选项绑定的配置节</param>
    /// <remarks>不得在此解析服务或使用 Service Locator。</remarks>
    void PostConfigure(TOptions options, IConfiguration configuration);
}
```

`OptionsSectionAttribute.cs`：

```csharp
namespace Tw.Configuration.Abstractions;

/// <summary>
/// 显式声明选项绑定的配置节路径
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class OptionsSectionAttribute : Attribute
{
    /// <summary>声明配置节路径</summary>
    /// <param name="path">配置节路径，例如 <c>Tw:Cache</c></param>
    public OptionsSectionAttribute(string path)
    {
        Path = path;
    }

    /// <summary>配置节路径</summary>
    public string Path { get; }
}
```

`OptionsNameAttribute.cs`：

```csharp
namespace Tw.Configuration.Abstractions;

/// <summary>
/// 为选项类型声明命名实例
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class OptionsNameAttribute : Attribute
{
    /// <summary>声明命名实例名称</summary>
    /// <param name="name">命名实例名称</param>
    public OptionsNameAttribute(string name)
    {
        Name = name;
    }

    /// <summary>命名实例名称</summary>
    public string Name { get; }
}
```

`DisableOptionsBindingAttribute.cs`：

```csharp
namespace Tw.Configuration.Abstractions;

/// <summary>
/// 标记选项类型跳过自动绑定
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class DisableOptionsBindingAttribute : Attribute;
```

`SensitiveConfigurationAttribute.cs`：

```csharp
namespace Tw.Configuration.Abstractions;

/// <summary>
/// 标记配置整类或单个属性为敏感，诊断报告不输出其值
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
public sealed class SensitiveConfigurationAttribute : Attribute;
```

`OptionsValidatorAttribute.cs`：

```csharp
namespace Tw.Configuration.Abstractions;

/// <summary>
/// 为选项类型指定验证器类型
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class OptionsValidatorAttribute : Attribute
{
    /// <summary>声明验证器类型</summary>
    /// <param name="validatorType">实现 <c>IValidateOptions&lt;TOptions&gt;</c> 的验证器类型</param>
    public OptionsValidatorAttribute(Type validatorType)
    {
        ValidatorType = validatorType;
    }

    /// <summary>验证器类型</summary>
    public Type ValidatorType { get; }
}
```

- [ ] **Step 4: 运行测试确认通过**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Tw.Core.Tests.csproj`
Expected: PASS。

- [ ] **Step 5: 提交**

```bash
git add backend/dotnet/BuildingBlocks/src/Tw.Core/Configuration/Abstractions backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Configuration/OptionsAbstractionsTests.cs
git commit -m "feat(core): add configurable options contract and attributes"
```

---

## Task 8: AOP 契约与拦截特性

**Files:**
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/DynamicProxy/Abstractions/IInvocationContext.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/DynamicProxy/Abstractions/IInterceptor.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/DynamicProxy/Abstractions/InterceptAttribute.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/DynamicProxy/Abstractions/DisableInterceptionAttribute.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/DynamicProxy/Abstractions/InterceptorOrderAttribute.cs`
- Test: `backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/DynamicProxy/InterceptionAttributeTests.cs`

- [ ] **Step 1: 写失败测试**

新建 `backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/DynamicProxy/InterceptionAttributeTests.cs`：

```csharp
using System.Reflection;
using FluentAssertions;
using Tw.DynamicProxy.Abstractions;
using Xunit;

namespace Tw.Core.Tests.DynamicProxy;

public class InterceptionAttributeTests
{
    private sealed class AuditInterceptor;

    [Fact]
    public void IInterceptor_LivesIn_AbstractionsNamespace()
    {
        typeof(IInterceptor).Namespace.Should().Be("Tw.DynamicProxy.Abstractions");
        typeof(IInvocationContext).Namespace.Should().Be("Tw.DynamicProxy.Abstractions");
    }

    [Fact]
    public void Intercept_CarriesInterceptorType()
    {
        var attr = new InterceptAttribute(typeof(AuditInterceptor));
        attr.InterceptorType.Should().Be(typeof(AuditInterceptor));
    }

    [Fact]
    public void Intercept_TargetsClassInterfaceMethod_AndAllowsMultiple()
    {
        var usage = typeof(InterceptAttribute).GetCustomAttribute<AttributeUsageAttribute>()!;
        usage.ValidOn.Should().Be(
            AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Method);
        usage.AllowMultiple.Should().BeTrue();
    }

    [Fact]
    public void DisableInterception_TargetsClassAndMethod()
    {
        var usage = typeof(DisableInterceptionAttribute).GetCustomAttribute<AttributeUsageAttribute>()!;
        usage.ValidOn.Should().Be(AttributeTargets.Class | AttributeTargets.Method);
    }

    [Fact]
    public void InterceptorOrder_CarriesOrder()
    {
        new InterceptorOrderAttribute(5).Order.Should().Be(5);
    }
}
```

- [ ] **Step 2: 运行测试确认失败**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Tw.Core.Tests.csproj`
Expected: 编译失败，AOP 类型不存在。

- [ ] **Step 3: 实现契约与特性**

`IInvocationContext.cs`：

```csharp
using System.Reflection;

namespace Tw.DynamicProxy.Abstractions;

/// <summary>
/// 一次方法级调用的上下文，可适配 Castle invocation 与 MVC action
/// </summary>
public interface IInvocationContext
{
    /// <summary>被调用的目标方法</summary>
    MethodInfo Method { get; }

    /// <summary>调用目标实例，静态或不可用时为 <see langword="null"/></summary>
    object? Target { get; }

    /// <summary>按位置排列的调用参数，可在 Proceed 前改写以传递修改后的入参</summary>
    object?[] Arguments { get; }

    /// <summary>按参数名读取的只读视图，不用于写回</summary>
    IReadOnlyDictionary<string, object?> ArgumentsByName { get; }

    /// <summary>调用返回值，可在 Proceed 之后改写</summary>
    object? ReturnValue { get; set; }

    /// <summary>异步推进到目标方法或下一个拦截器，并写入 <see cref="ReturnValue"/></summary>
    ValueTask ProceedAsync();

    /// <summary>同步推进到目标方法；目标为异步时抛出明确异常</summary>
    void Proceed();
}
```

`IInterceptor.cs`：

```csharp
namespace Tw.DynamicProxy.Abstractions;

/// <summary>
/// 统一方法级拦截器契约
/// </summary>
public interface IInterceptor
{
    /// <summary>拦截一次方法级调用</summary>
    /// <param name="context">方法级调用上下文</param>
    /// <returns>表示拦截完成的 <see cref="ValueTask"/></returns>
    ValueTask InterceptAsync(IInvocationContext context);
}
```

`InterceptAttribute.cs`：

```csharp
namespace Tw.DynamicProxy.Abstractions;

/// <summary>
/// 声明类、接口或方法启用指定拦截器
/// </summary>
/// <remarks>方法级声明优先于类型级。</remarks>
[AttributeUsage(
    AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Method,
    AllowMultiple = true,
    Inherited = true)]
public sealed class InterceptAttribute : Attribute
{
    /// <summary>声明拦截器类型</summary>
    /// <param name="interceptorType">实现 <see cref="IInterceptor"/> 的拦截器类型</param>
    public InterceptAttribute(Type interceptorType)
    {
        InterceptorType = interceptorType;
    }

    /// <summary>拦截器类型</summary>
    public Type InterceptorType { get; }
}
```

`DisableInterceptionAttribute.cs`：

```csharp
namespace Tw.DynamicProxy.Abstractions;

/// <summary>
/// 关闭类或方法的拦截
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class DisableInterceptionAttribute : Attribute;
```

`InterceptorOrderAttribute.cs`：

```csharp
namespace Tw.DynamicProxy.Abstractions;

/// <summary>
/// 声明拦截器在调用链中的顺序
/// </summary>
/// <remarks>顺序相同按类型名称稳定排序。</remarks>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class InterceptorOrderAttribute : Attribute
{
    /// <summary>声明拦截器顺序</summary>
    /// <param name="order">顺序数值，越小越先执行</param>
    public InterceptorOrderAttribute(int order)
    {
        Order = order;
    }

    /// <summary>拦截器顺序</summary>
    public int Order { get; }
}
```

- [ ] **Step 4: 运行测试确认通过**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Tw.Core.Tests.csproj`
Expected: PASS。

- [ ] **Step 5: 提交**

```bash
git add backend/dotnet/BuildingBlocks/src/Tw.Core/DynamicProxy backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/DynamicProxy/InterceptionAttributeTests.cs
git commit -m "feat(core): add interception contracts and attributes"
```

---

## Task 9: 拦截器基类（同步与异步）

**Files:**
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/DynamicProxy/Abstractions/SyncInterceptorBase.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/DynamicProxy/Abstractions/InterceptorBase.cs`
- Create: `backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/DynamicProxy/Fakes/FakeInvocationContext.cs`
- Test: `backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/DynamicProxy/SyncInterceptorBaseTests.cs`
- Test: `backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/DynamicProxy/InterceptorBaseTests.cs`

> 设计说明：基类只编排 `Before`/`Proceed`/`OnException`/`After` 结构；真正的同步/异步目标适配在引擎层（P4）实现。这里用测试替身 `FakeInvocationContext` 验证编排顺序与异常路径。

- [ ] **Step 1: 写测试替身**

新建 `backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/DynamicProxy/Fakes/FakeInvocationContext.cs`：

```csharp
using System.Reflection;
using Tw.DynamicProxy.Abstractions;

namespace Tw.Core.Tests.DynamicProxy.Fakes;

/// <summary>用于基类编排测试的最小 IInvocationContext 替身</summary>
internal sealed class FakeInvocationContext : IInvocationContext
{
    private readonly Action? _onProceed;

    public FakeInvocationContext(Action? onProceed = null)
    {
        _onProceed = onProceed;
    }

    public int ProceedCount { get; private set; }

    public MethodInfo Method => typeof(FakeInvocationContext).GetMethod(nameof(Sample))!;
    public object? Target => null;
    public object?[] Arguments { get; } = [];
    public IReadOnlyDictionary<string, object?> ArgumentsByName { get; } =
        new Dictionary<string, object?>();
    public object? ReturnValue { get; set; }

    public void Proceed()
    {
        ProceedCount++;
        _onProceed?.Invoke();
    }

    public ValueTask ProceedAsync()
    {
        Proceed();
        return ValueTask.CompletedTask;
    }

    public void Sample()
    {
    }
}
```

- [ ] **Step 2: 写失败测试（同步基类）**

新建 `backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/DynamicProxy/SyncInterceptorBaseTests.cs`：

```csharp
using FluentAssertions;
using Tw.Core.Tests.DynamicProxy.Fakes;
using Tw.DynamicProxy.Abstractions;
using Xunit;

namespace Tw.Core.Tests.DynamicProxy;

public class SyncInterceptorBaseTests
{
    private sealed class RecordingInterceptor : SyncInterceptorBase
    {
        public List<string> Calls { get; } = [];

        protected override void Before(IInvocationContext context) => Calls.Add("before");
        protected override void After(IInvocationContext context) => Calls.Add("after");
        protected override void OnException(IInvocationContext context, Exception exception) =>
            Calls.Add("onexception");
    }

    [Fact]
    public async Task HappyPath_RunsBeforeProceedAfter_WithoutOnException()
    {
        var sut = new RecordingInterceptor();
        var context = new FakeInvocationContext();

        await sut.InterceptAsync(context);

        sut.Calls.Should().Equal("before", "after");
        context.ProceedCount.Should().Be(1);
    }

    [Fact]
    public async Task ExceptionPath_RunsOnExceptionThenAfter_AndRethrows()
    {
        var sut = new RecordingInterceptor();
        var context = new FakeInvocationContext(
            () => throw new InvalidOperationException("boom"));

        var act = async () => await sut.InterceptAsync(context);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("boom");
        sut.Calls.Should().Equal("before", "onexception", "after");
    }
}
```

- [ ] **Step 3: 运行测试确认失败**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Tw.Core.Tests.csproj`
Expected: 编译失败，`SyncInterceptorBase` 不存在。

- [ ] **Step 4: 实现同步基类**

`SyncInterceptorBase.cs`：

```csharp
namespace Tw.DynamicProxy.Abstractions;

/// <summary>
/// 同步拦截器基类，按 Before / Proceed / OnException / After 编排
/// </summary>
/// <remarks>
/// 仅用于同步目标方法；误用于异步目标时由 <see cref="IInvocationContext.Proceed"/> 在运行期抛出明确异常。
/// </remarks>
public abstract class SyncInterceptorBase : IInterceptor
{
    /// <inheritdoc />
    public ValueTask InterceptAsync(IInvocationContext context)
    {
        Before(context);
        try
        {
            context.Proceed();
        }
        catch (Exception ex)
        {
            OnException(context, ex);
            throw;
        }
        finally
        {
            After(context);
        }

        return ValueTask.CompletedTask;
    }

    /// <summary>目标方法执行前调用</summary>
    /// <param name="context">调用上下文</param>
    protected virtual void Before(IInvocationContext context)
    {
    }

    /// <summary>目标方法执行后调用，无论是否抛异常都在 finally 中执行</summary>
    /// <param name="context">调用上下文</param>
    protected virtual void After(IInvocationContext context)
    {
    }

    /// <summary>目标方法抛异常时调用，默认不吞异常</summary>
    /// <param name="context">调用上下文</param>
    /// <param name="exception">目标方法抛出的异常</param>
    protected virtual void OnException(IInvocationContext context, Exception exception)
    {
    }
}
```

- [ ] **Step 5: 运行测试确认通过**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Tw.Core.Tests.csproj`
Expected: PASS。

- [ ] **Step 6: 写失败测试（异步基类）**

新建 `backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/DynamicProxy/InterceptorBaseTests.cs`：

```csharp
using FluentAssertions;
using Tw.Core.Tests.DynamicProxy.Fakes;
using Tw.DynamicProxy.Abstractions;
using Xunit;

namespace Tw.Core.Tests.DynamicProxy;

public class InterceptorBaseTests
{
    private sealed class RecordingInterceptor : InterceptorBase
    {
        public List<string> Calls { get; } = [];

        protected override ValueTask BeforeAsync(IInvocationContext context)
        {
            Calls.Add("before");
            return ValueTask.CompletedTask;
        }

        protected override ValueTask AfterAsync(IInvocationContext context)
        {
            Calls.Add("after");
            return ValueTask.CompletedTask;
        }

        protected override ValueTask OnExceptionAsync(IInvocationContext context, Exception exception)
        {
            Calls.Add("onexception");
            return ValueTask.CompletedTask;
        }
    }

    [Fact]
    public async Task HappyPath_RunsBeforeProceedAfter_WithoutOnException()
    {
        var sut = new RecordingInterceptor();
        var context = new FakeInvocationContext();

        await sut.InterceptAsync(context);

        sut.Calls.Should().Equal("before", "after");
        context.ProceedCount.Should().Be(1);
    }

    [Fact]
    public async Task ExceptionPath_RunsOnExceptionThenAfter_AndRethrows()
    {
        var sut = new RecordingInterceptor();
        var context = new FakeInvocationContext(
            () => throw new InvalidOperationException("boom"));

        var act = async () => await sut.InterceptAsync(context);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("boom");
        sut.Calls.Should().Equal("before", "onexception", "after");
    }
}
```

- [ ] **Step 7: 运行测试确认失败**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Tw.Core.Tests.csproj`
Expected: 编译失败，`InterceptorBase` 不存在。

- [ ] **Step 8: 实现异步基类**

`InterceptorBase.cs`：

```csharp
namespace Tw.DynamicProxy.Abstractions;

/// <summary>
/// 异步拦截器基类，按 BeforeAsync / ProceedAsync / OnExceptionAsync / AfterAsync 编排
/// </summary>
public abstract class InterceptorBase : IInterceptor
{
    /// <inheritdoc />
    public async ValueTask InterceptAsync(IInvocationContext context)
    {
        await BeforeAsync(context);
        try
        {
            await context.ProceedAsync();
        }
        catch (Exception ex)
        {
            await OnExceptionAsync(context, ex);
            throw;
        }
        finally
        {
            await AfterAsync(context);
        }
    }

    /// <summary>目标方法执行前调用</summary>
    /// <param name="context">调用上下文</param>
    /// <returns>表示前置逻辑完成的 <see cref="ValueTask"/></returns>
    protected virtual ValueTask BeforeAsync(IInvocationContext context) => ValueTask.CompletedTask;

    /// <summary>目标方法执行后调用，无论是否抛异常都在 finally 中执行</summary>
    /// <param name="context">调用上下文</param>
    /// <returns>表示后置逻辑完成的 <see cref="ValueTask"/></returns>
    protected virtual ValueTask AfterAsync(IInvocationContext context) => ValueTask.CompletedTask;

    /// <summary>目标方法抛异常时调用，默认不吞异常</summary>
    /// <param name="context">调用上下文</param>
    /// <param name="exception">目标方法抛出的异常</param>
    /// <returns>表示异常处理完成的 <see cref="ValueTask"/></returns>
    protected virtual ValueTask OnExceptionAsync(IInvocationContext context, Exception exception) =>
        ValueTask.CompletedTask;
}
```

- [ ] **Step 9: 运行测试确认通过**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Tw.Core.Tests.csproj`
Expected: PASS。

- [ ] **Step 10: 提交**

```bash
git add backend/dotnet/BuildingBlocks/src/Tw.Core/DynamicProxy/Abstractions/SyncInterceptorBase.cs backend/dotnet/BuildingBlocks/src/Tw.Core/DynamicProxy/Abstractions/InterceptorBase.cs backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/DynamicProxy
git commit -m "feat(core): add sync and async interceptor base classes"
```

---

## Task 10: 更新 Tw.Core package-charter.yaml

**Files:**
- Modify: `backend/dotnet/BuildingBlocks/src/Tw.Core/package-charter.yaml`

> 依据 [shared-package-charter.md](../../engineering-standards/03-project-and-code/shared-package-charter.md)：`public_capabilities` 用迁移后命名空间，新增两个抽象命名空间；`dependency_rules.allow` 声明允许的轻量契约包，`forbid` 增列 Autofac / Castle。

- [ ] **Step 1: 修改 charter 的 public_capabilities 与 dependency_rules**

把 `public_capabilities` 与 `dependency_rules` 两段替换为：

```yaml
public_capabilities:
  - Tw.Context
  - Tw.Check
  - Tw.Collections
  - Tw.Configuration.Abstractions
  - Tw.DependencyInjection.Abstractions
  - Tw.DynamicProxy.Abstractions
  - Tw.Core.Primitives
  - Tw.Reflection
  - Tw.Core.Security.Cryptography
  - Tw.Exceptions
  - Tw.Extensions
  - Tw.Utilities
dependency_rules:
  forbid:
    - "Microsoft.AspNetCore.*"
    - "Microsoft.EntityFrameworkCore*"
    - "Autofac*"
    - "Castle.*"
  allow:
    - "Microsoft.Extensions.DependencyInjection.Abstractions"
    - "Microsoft.Extensions.Configuration.Abstractions"
    - "Microsoft.Extensions.Options"
```

- [ ] **Step 2: 校验 charter 与现有依赖一致**

Run: `dotnet build backend/dotnet/BuildingBlocks/src/Tw.Core/Tw.Core.csproj`
Expected: 构建成功；人工核对 `Tw.Core.csproj` 的三个 `PackageReference` 都在 `allow` 列表内，且无 `Autofac` / `Castle` 引用。

- [ ] **Step 3: 提交**

```bash
git add backend/dotnet/BuildingBlocks/src/Tw.Core/package-charter.yaml
git commit -m "docs(core): update charter for DI/options/AOP abstractions"
```

---

## Task 11: 更新 Tw.Core 使用文档索引

**Files:**
- Modify: `docs/shared-packages/dotnet/Tw.Core/README.md`

> P0 只产出抽象，完整 How-to 使用文档随引擎阶段（P2/P3/P4）落地。本任务在 Tw.Core 索引页登记新增抽象能力，保持从索引可定位。先读现有 README 结构，按其既有格式追加，不要臆造章节。

- [ ] **Step 1: 读现有 README**

Run: `cat docs/shared-packages/dotnet/Tw.Core/README.md`
确认其现有的能力清单格式（表格或列表）。

- [ ] **Step 2: 追加抽象能力条目**

在 README 的能力清单中，按现有格式追加以下四项抽象命名空间（描述列照现有风格简述）：
- `Tw.DependencyInjection.Abstractions` — DI 自动注册标记接口与特性（引擎执行见 `Tw.DependencyInjection`，P1+ 落地）
- `Tw.Configuration.Abstractions` — 配置 Options 契约与特性（绑定执行见 `Tw.DependencyInjection`，P3 落地）
- `Tw.DynamicProxy.Abstractions` — AOP 拦截契约与基类（承载执行见 `Tw.DependencyInjection`，P4 落地）
- `Tw.Reflection` — 类型查找与反射缓存（由 `Tw.Core.Reflection` 迁移）

若 README 原有列出 `Tw.Core.Configuration` / `Tw.Core.Reflection`，同步更新为迁移后名称。

- [ ] **Step 3: 提交**

```bash
git add docs/shared-packages/dotnet/Tw.Core/README.md
git commit -m "docs(core): index DI/options/AOP abstractions in Tw.Core readme"
```

---

## 阶段收尾验证

- [ ] **全量测试**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Tw.Core.Tests.csproj`
Expected: 全部 PASS。

- [ ] **依赖边界自检**

确认 `Tw.Core.csproj` 不含 `Autofac` / `Castle` / `Microsoft.AspNetCore.*` 引用；四个能力命名空间的文件夹路径与命名空间一一对应。

- [ ] **P0 完成标志**

`Tw.Core` 提供 DI / 配置 Options / AOP 的全部框架无关抽象，历史命名空间已迁移，charter 与索引同步。P1（扫描地基 + `Tw.DependencyInjection` 包）可在此基础上开工。
