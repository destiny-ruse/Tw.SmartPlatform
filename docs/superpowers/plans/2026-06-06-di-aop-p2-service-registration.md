# P2 DI 注册 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 在 `Tw.DependencyInjection` 落地服务参与判定、生命周期解析、暴露契约、keyed service、open generic、非 keyed 单实现仲裁，以及 `AddServiceRegistration(IConfiguration)` 注册入口。

**Architecture:** P2 复用 P1 的程序集发现与拓扑结果，新增“元数据规划器 + `IServiceCollection` 执行器”。规划器只读取类型元数据并产出不可变计划；执行器把计划写入 `IServiceCollection`，最终由 P1 的 `UseAutofac()` 接管宿主服务提供程序。Options 自动绑定、AOP 承载、ASP.NET Core host 聚合不在本阶段实现。

**Tech Stack:** C# / .NET 10、Microsoft.Extensions.DependencyInjection keyed service、Microsoft.Extensions.Configuration.Binder、Autofac 9.x、Autofac.Extensions.DependencyInjection 11.x、xunit、FluentAssertions、中央包管理（CPM）、NuGet 锁定文件。

**对应 spec：** [docs/superpowers/specs/2026-06-06-di-options-aop-design.md](../specs/2026-06-06-di-options-aop-design.md)，阶段 P2。

**前置阶段：**
- P0 抽象地基已提供 `Tw.DependencyInjection.Abstractions`、`Tw.Configuration.Abstractions`、`Tw.DynamicProxy.Abstractions`、`Tw.Reflection`
- P1 扫描地基已提供 `ServiceRegistrationOptions`、`AssemblyDiscoverer`、`AssemblyTopologySorter`、`UseAutofac()`、`ServiceRegistrationReport` 骨架

**前置规范（实现前必读）：**
- [docs/engineering-standards/03-project-and-code/language-specific/dotnet-core.md](../../engineering-standards/03-project-and-code/language-specific/dotnet-core.md)（命名空间=RootNamespace+文件夹、跨程序集不共享命名空间、XML 文档注释、共享包扩展命名）
- [docs/engineering-standards/03-project-and-code/shared-package-charter.md](../../engineering-standards/03-project-and-code/shared-package-charter.md)（公开能力变更同步 charter 与 docs/shared-packages 使用文档）
- [docs/engineering-standards/04-quality/testing-standards.md](../../engineering-standards/04-quality/testing-standards.md)（核心决策逻辑必须有自动化测试）

**通用约定：**
- 公开类型与公开成员必须带 DocFX XML 文档注释。
- 内部规划类型位于 `Tw.DependencyInjection.Registration`，测试项目通过既有 `InternalsVisibleTo` 访问。
- 测试命名 `成员_预期[_条件]`，命名空间 `Tw.DependencyInjection.Tests.Registration` 或 `Tw.DependencyInjection.Tests.Hosting`。
- 单元测试命令：`dotnet test backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests/Tw.DependencyInjection.Tests.csproj`
- 回归测试命令：`dotnet test backend/dotnet/Tw.SmartPlatform.slnx`

---

## 文件结构

**修改现有文件：**
- `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Tw.DependencyInjection.csproj`：新增配置绑定与 DI 抽象引用
- `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/ServiceRegistrationOptions.cs`：新增 `AssemblyPriorities`
- `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Diagnostics/ServiceRegistrationReport.cs`：扩展候选、最终注册、superseded、跳过、冲突段落
- `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Discovery/AssemblyDiscoverer.cs`：结果追加引用可达图，供平级程序集仲裁失败规则使用
- `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/package-charter.yaml`：公开能力追加 DI 自动注册
- `docs/shared-packages/dotnet/Tw.DependencyInjection/README.md`：追加服务注册 How-to 链接

**新增引擎文件：**
- `Registration/ServiceRegistrationPlan.cs`：内部计划对象
- `Registration/ServiceCandidate.cs`：内部候选模型
- `Registration/ServiceExposure.cs`：内部暴露模型
- `Registration/ServiceRegistrationPlanner.cs`：扫描类型、解析生命周期、暴露契约、计算优先级、执行仲裁
- `Registration/ServiceRegistrationExecutor.cs`：把计划写入 `IServiceCollection`
- `ServiceCollectionRegistrationExtensions.cs`：公开 `AddServiceRegistration(this IServiceCollection, IConfiguration)`
- `Registration/DependencyLifetimeMapper.cs`：`DependencyLifetime` 到 `ServiceLifetime` 映射
- `Registration/ServiceTypeInspector.cs`：参与注册判定、生命周期标记检测、Options 类型跳过
- `Registration/ServiceExposureResolver.cs`：显式暴露、默认暴露、keyed 暴露、open generic 暴露
- `Registration/ServicePriorityResolver.cs`：拓扑、程序集优先级、类型优先级与范围校验
- `Registration/AssemblyReachabilityGraph.cs`：程序集依赖可达关系
- `Registration/ConstructorKeyedServiceValidator.cs`：校验 `[FromKeyedServices]` 指向已规划 key

**新增/修改诊断公开 record：**
- `Diagnostics/ServiceCandidateDiagnostic.cs`
- `Diagnostics/PlannedServiceRegistrationDiagnostic.cs`
- `Diagnostics/SupersededServiceCandidateDiagnostic.cs`
- `Diagnostics/SkippedServiceTypeDiagnostic.cs`
- `Diagnostics/ServiceConflictDiagnostic.cs`

**新增测试：**
- `tests/Tw.DependencyInjection.Tests/Registration/DependencyLifetimeResolverTests.cs`
- `tests/Tw.DependencyInjection.Tests/Registration/ServiceExposureResolverTests.cs`
- `tests/Tw.DependencyInjection.Tests/Registration/ServicePriorityResolverTests.cs`
- `tests/Tw.DependencyInjection.Tests/Registration/ServiceRegistrationPlannerTests.cs`
- `tests/Tw.DependencyInjection.Tests/Registration/ServiceRegistrationExecutorTests.cs`
- `tests/Tw.DependencyInjection.Tests/Hosting/AddServiceRegistrationIntegrationTests.cs`

**新增文档：**
- `docs/shared-packages/dotnet/Tw.DependencyInjection/service-registration.md`

---

## Task 1: 依赖与扫描选项扩展

**Files:**
- Modify: `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Tw.DependencyInjection.csproj`
- Modify: `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/ServiceRegistrationOptions.cs`
- Test: `backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests/Discovery/AssemblyFilterTests.cs`

- [ ] **Step 1: 写失败测试**

在 `AssemblyFilterTests.cs` 追加：

```csharp
[Fact]
public void Options_DefaultsAssemblyPrioritiesToEmptyDictionary()
{
    var options = new ServiceRegistrationOptions();

    options.AssemblyPriorities.Should().BeEmpty();
}
```

- [ ] **Step 2: 运行测试确认失败**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests/Tw.DependencyInjection.Tests.csproj`

Expected: 编译失败，`ServiceRegistrationOptions.AssemblyPriorities` 不存在。

- [ ] **Step 3: 修改 csproj**

在 `Tw.DependencyInjection.csproj` 的包引用中追加：

```xml
    <PackageReference Include="Microsoft.Extensions.Configuration.Binder" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" />
```

- [ ] **Step 4: 扩展 ServiceRegistrationOptions**

在 `ServiceRegistrationOptions` 中追加：

```csharp
/// <summary>程序集级显式优先级配置，key 为程序集名，value 越大优先级越高</summary>
public IDictionary<string, int> AssemblyPriorities { get; } =
    new Dictionary<string, int>(StringComparer.Ordinal);
```

- [ ] **Step 5: 运行测试确认通过**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests/Tw.DependencyInjection.Tests.csproj`

Expected: PASS。

- [ ] **Step 6: 提交**

```bash
git add backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Tw.DependencyInjection.csproj backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/ServiceRegistrationOptions.cs backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests/Discovery/AssemblyFilterTests.cs
git commit -m "feat(di): add assembly priority options"
```

---

## Task 2: 诊断模型扩展

**Files:**
- Create: `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Diagnostics/ServiceCandidateDiagnostic.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Diagnostics/PlannedServiceRegistrationDiagnostic.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Diagnostics/SupersededServiceCandidateDiagnostic.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Diagnostics/SkippedServiceTypeDiagnostic.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Diagnostics/ServiceConflictDiagnostic.cs`
- Modify: `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Diagnostics/ServiceRegistrationReport.cs`
- Test: `backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests/Registration/ServiceRegistrationPlannerTests.cs`

- [ ] **Step 1: 写失败测试**

新建 `ServiceRegistrationPlannerTests.cs`：

```csharp
using FluentAssertions;
using Tw.DependencyInjection.Abstractions;
using Tw.DependencyInjection.Diagnostics;
using Xunit;

namespace Tw.DependencyInjection.Tests.Registration;

public class ServiceRegistrationPlannerTests
{
    [Fact]
    public void Report_ExposesRegistrationPlanningSections()
    {
        var candidate = new ServiceCandidateDiagnostic(
            ImplementationTypeName: "Sample.OrderService",
            ServiceTypeName: "Sample.IOrderService",
            Key: null,
            Lifetime: DependencyLifetime.Scoped,
            AssemblyName: "Sample",
            FinalPriority: 0,
            Status: "selected");

        var registration = new PlannedServiceRegistrationDiagnostic(
            ServiceTypeName: "Sample.IOrderService",
            ImplementationTypeName: "Sample.OrderService",
            Key: null,
            Lifetime: DependencyLifetime.Scoped,
            FinalPriority: 0);

        var report = new ServiceRegistrationReport(
            scannedAssemblies: ["Sample"],
            excludedAssemblies: [],
            topology: [],
            candidates: [candidate],
            registrations: [registration],
            supersededCandidates: [],
            skippedTypes: [],
            conflicts: []);

        report.Candidates.Should().ContainSingle().Which.Status.Should().Be("selected");
        report.Registrations.Should().ContainSingle().Which.ServiceTypeName.Should().Be("Sample.IOrderService");
        report.SupersededCandidates.Should().BeEmpty();
        report.SkippedTypes.Should().BeEmpty();
        report.Conflicts.Should().BeEmpty();
    }
}
```

- [ ] **Step 2: 运行测试确认失败**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests/Tw.DependencyInjection.Tests.csproj`

Expected: 编译失败，诊断 record 或 `ServiceRegistrationReport` 构造函数重载不存在。

- [ ] **Step 3: 新增诊断 record**

`ServiceCandidateDiagnostic.cs`：

```csharp
using Tw.DependencyInjection.Abstractions;

namespace Tw.DependencyInjection.Diagnostics;

/// <summary>服务注册候选诊断项</summary>
public sealed record ServiceCandidateDiagnostic(
    string ImplementationTypeName,
    string ServiceTypeName,
    object? Key,
    DependencyLifetime Lifetime,
    string AssemblyName,
    long FinalPriority,
    string Status);
```

`PlannedServiceRegistrationDiagnostic.cs`：

```csharp
using Tw.DependencyInjection.Abstractions;

namespace Tw.DependencyInjection.Diagnostics;

/// <summary>最终写入容器的服务注册诊断项</summary>
public sealed record PlannedServiceRegistrationDiagnostic(
    string ServiceTypeName,
    string ImplementationTypeName,
    object? Key,
    DependencyLifetime Lifetime,
    long FinalPriority);
```

`SupersededServiceCandidateDiagnostic.cs`：

```csharp
namespace Tw.DependencyInjection.Diagnostics;

/// <summary>被单实现仲裁淘汰的候选诊断项</summary>
public sealed record SupersededServiceCandidateDiagnostic(
    string ServiceTypeName,
    string ImplementationTypeName,
    object? Key,
    string SelectedImplementationTypeName,
    long FinalPriority,
    long SelectedFinalPriority);
```

`SkippedServiceTypeDiagnostic.cs`：

```csharp
namespace Tw.DependencyInjection.Diagnostics;

/// <summary>扫描到但未参与普通服务注册的类型诊断项</summary>
public sealed record SkippedServiceTypeDiagnostic(
    string TypeName,
    string Reason);
```

`ServiceConflictDiagnostic.cs`：

```csharp
namespace Tw.DependencyInjection.Diagnostics;

/// <summary>导致启动失败的服务注册冲突诊断项</summary>
public sealed record ServiceConflictDiagnostic(
    string ServiceTypeName,
    object? Key,
    IReadOnlyList<string> ImplementationTypeNames,
    string Reason);
```

- [ ] **Step 4: 扩展 ServiceRegistrationReport**

保留 P1 三参数构造函数，并追加完整构造函数与属性：

```csharp
public ServiceRegistrationReport(
    IReadOnlyList<string> scannedAssemblies,
    IReadOnlyList<string> excludedAssemblies,
    IReadOnlyList<AssemblyTopologyEntry> topology)
    : this(
        scannedAssemblies,
        excludedAssemblies,
        topology,
        candidates: [],
        registrations: [],
        supersededCandidates: [],
        skippedTypes: [],
        conflicts: [])
{
}

public ServiceRegistrationReport(
    IReadOnlyList<string> scannedAssemblies,
    IReadOnlyList<string> excludedAssemblies,
    IReadOnlyList<AssemblyTopologyEntry> topology,
    IReadOnlyList<ServiceCandidateDiagnostic> candidates,
    IReadOnlyList<PlannedServiceRegistrationDiagnostic> registrations,
    IReadOnlyList<SupersededServiceCandidateDiagnostic> supersededCandidates,
    IReadOnlyList<SkippedServiceTypeDiagnostic> skippedTypes,
    IReadOnlyList<ServiceConflictDiagnostic> conflicts)
{
    ScannedAssemblies = scannedAssemblies;
    ExcludedAssemblies = excludedAssemblies;
    Topology = topology;
    Candidates = candidates;
    Registrations = registrations;
    SupersededCandidates = supersededCandidates;
    SkippedTypes = skippedTypes;
    Conflicts = conflicts;
}

/// <summary>服务注册候选</summary>
public IReadOnlyList<ServiceCandidateDiagnostic> Candidates { get; }

/// <summary>最终注册项</summary>
public IReadOnlyList<PlannedServiceRegistrationDiagnostic> Registrations { get; }

/// <summary>被仲裁淘汰的候选</summary>
public IReadOnlyList<SupersededServiceCandidateDiagnostic> SupersededCandidates { get; }

/// <summary>扫描到但跳过的类型</summary>
public IReadOnlyList<SkippedServiceTypeDiagnostic> SkippedTypes { get; }

/// <summary>规划阶段冲突</summary>
public IReadOnlyList<ServiceConflictDiagnostic> Conflicts { get; }
```

- [ ] **Step 5: 运行测试确认通过**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests/Tw.DependencyInjection.Tests.csproj`

Expected: PASS。

- [ ] **Step 6: 提交**

```bash
git add backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Diagnostics backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests/Registration/ServiceRegistrationPlannerTests.cs
git commit -m "feat(di): extend service registration diagnostics"
```

---

## Task 3: 生命周期解析与参与注册判定

**Files:**
- Create: `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Registration/DependencyLifetimeMapper.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Registration/ServiceTypeInspector.cs`
- Test: `backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests/Registration/DependencyLifetimeResolverTests.cs`

- [ ] **Step 1: 写失败测试**

新建 `DependencyLifetimeResolverTests.cs`：

```csharp
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Tw.Configuration.Abstractions;
using Tw.DependencyInjection.Abstractions;
using Tw.DependencyInjection.Registration;
using Xunit;

namespace Tw.DependencyInjection.Tests.Registration;

public class DependencyLifetimeResolverTests
{
    private sealed class ScopedService : IScopedDependency;
    private sealed class MultiLifetimeService : IScopedDependency, ISingletonDependency;

    [ServiceRegistration(DependencyLifetime.Singleton)]
    private sealed class AttributeLifetimeService : IScopedDependency;

    [ServiceRegistration]
    private sealed class NoLifetimeService;

    private sealed class CacheOptions : IConfigurableOptions;
    private abstract class AbstractService : IScopedDependency;

    [Fact]
    public void ResolveLifetime_UsesMarkerInterface()
    {
        ServiceTypeInspector.TryResolveLifetime(typeof(ScopedService), out var lifetime, out var reason)
            .Should().BeTrue();
        lifetime.Should().Be(DependencyLifetime.Scoped);
        reason.Should().BeNull();
    }

    [Fact]
    public void ResolveLifetime_AttributeOverridesMarker()
    {
        ServiceTypeInspector.TryResolveLifetime(typeof(AttributeLifetimeService), out var lifetime, out _)
            .Should().BeTrue();
        lifetime.Should().Be(DependencyLifetime.Singleton);
    }

    [Fact]
    public void ResolveLifetime_FailsWhenMultipleMarkersDeclared()
    {
        ServiceTypeInspector.TryResolveLifetime(typeof(MultiLifetimeService), out _, out var reason)
            .Should().BeFalse();
        reason.Should().Contain("多个生命周期标记");
    }

    [Fact]
    public void ShouldSkipOrdinaryRegistration_SkipsOptionsAndAbstractTypes()
    {
        ServiceTypeInspector.ShouldSkipOrdinaryRegistration(typeof(CacheOptions), out var optionsReason)
            .Should().BeTrue();
        optionsReason.Should().Contain("Options");

        ServiceTypeInspector.ShouldSkipOrdinaryRegistration(typeof(AbstractService), out var abstractReason)
            .Should().BeTrue();
        abstractReason.Should().Contain("抽象");
    }

    [Fact]
    public void ResolveLifetime_SkipsWhenNoLifetimeDeclared()
    {
        ServiceTypeInspector.TryResolveLifetime(typeof(NoLifetimeService), out _, out var reason)
            .Should().BeFalse();
        reason.Should().Contain("未声明生命周期");
    }

    [Theory]
    [InlineData(DependencyLifetime.Transient, ServiceLifetime.Transient)]
    [InlineData(DependencyLifetime.Scoped, ServiceLifetime.Scoped)]
    [InlineData(DependencyLifetime.Singleton, ServiceLifetime.Singleton)]
    public void Mapper_MapsToMicrosoftServiceLifetime(DependencyLifetime source, ServiceLifetime expected)
    {
        DependencyLifetimeMapper.Map(source).Should().Be(expected);
    }
}
```

- [ ] **Step 2: 运行测试确认失败**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests/Tw.DependencyInjection.Tests.csproj`

Expected: 编译失败，`ServiceTypeInspector` 与 `DependencyLifetimeMapper` 不存在。

- [ ] **Step 3: 实现 DependencyLifetimeMapper**

```csharp
using Microsoft.Extensions.DependencyInjection;
using Tw.DependencyInjection.Abstractions;

namespace Tw.DependencyInjection.Registration;

internal static class DependencyLifetimeMapper
{
    public static ServiceLifetime Map(DependencyLifetime lifetime) =>
        lifetime switch
        {
            DependencyLifetime.Transient => ServiceLifetime.Transient,
            DependencyLifetime.Scoped => ServiceLifetime.Scoped,
            DependencyLifetime.Singleton => ServiceLifetime.Singleton,
            _ => throw new ArgumentOutOfRangeException(nameof(lifetime), lifetime, "未知服务生命周期"),
        };
}
```

- [ ] **Step 4: 实现 ServiceTypeInspector**

```csharp
using Tw.Configuration.Abstractions;
using Tw.DependencyInjection.Abstractions;

namespace Tw.DependencyInjection.Registration;

internal static class ServiceTypeInspector
{
    public static bool ShouldSkipOrdinaryRegistration(Type type, out string reason)
    {
        if (type.IsInterface)
        {
            reason = "接口不作为普通服务实现注册";
            return true;
        }

        if (type.IsAbstract)
        {
            reason = "抽象类型不作为普通服务实现注册";
            return true;
        }

        if (type.IsGenericType && !type.IsGenericTypeDefinition && type.ContainsGenericParameters)
        {
            reason = "未闭合且非泛型定义的类型不作为普通服务实现注册";
            return true;
        }

        if (typeof(IConfigurableOptions).IsAssignableFrom(type))
        {
            reason = "Options 类型由 P3 Options 装载处理，不作为普通服务注册";
            return true;
        }

        if (type.GetCustomAttributes(typeof(DisableServiceRegistrationAttribute), inherit: false).Length > 0)
        {
            reason = "类型标记 DisableServiceRegistration";
            return true;
        }

        reason = string.Empty;
        return false;
    }

    public static bool IsRegistrationParticipant(Type type)
    {
        return type.GetCustomAttributes(typeof(ServiceRegistrationAttribute), inherit: false).Length > 0
            || typeof(ITransientDependency).IsAssignableFrom(type)
            || typeof(IScopedDependency).IsAssignableFrom(type)
            || typeof(ISingletonDependency).IsAssignableFrom(type);
    }

    public static bool TryResolveLifetime(
        Type type,
        out DependencyLifetime lifetime,
        out string? failureReason)
    {
        var markerLifetimes = new List<DependencyLifetime>();
        if (typeof(ITransientDependency).IsAssignableFrom(type))
        {
            markerLifetimes.Add(DependencyLifetime.Transient);
        }

        if (typeof(IScopedDependency).IsAssignableFrom(type))
        {
            markerLifetimes.Add(DependencyLifetime.Scoped);
        }

        if (typeof(ISingletonDependency).IsAssignableFrom(type))
        {
            markerLifetimes.Add(DependencyLifetime.Singleton);
        }

        if (markerLifetimes.Count > 1)
        {
            lifetime = default;
            failureReason = "类型声明多个生命周期标记";
            return false;
        }

        var registration = type.GetCustomAttributes(typeof(ServiceRegistrationAttribute), inherit: false)
            .OfType<ServiceRegistrationAttribute>()
            .SingleOrDefault();

        if (registration?.Lifetime is not null)
        {
            lifetime = registration.Lifetime.Value;
            failureReason = null;
            return true;
        }

        if (markerLifetimes.Count == 1)
        {
            lifetime = markerLifetimes[0];
            failureReason = null;
            return true;
        }

        lifetime = default;
        failureReason = "类型未声明生命周期";
        return false;
    }
}
```

- [ ] **Step 5: 运行测试确认通过**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests/Tw.DependencyInjection.Tests.csproj`

Expected: PASS。

- [ ] **Step 6: 提交**

```bash
git add backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Registration backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests/Registration/DependencyLifetimeResolverTests.cs
git commit -m "feat(di): add service type and lifetime inspection"
```

---

## Task 4: 暴露契约解析

**Files:**
- Create: `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Registration/ServiceExposure.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Registration/ServiceExposureResolver.cs`
- Test: `backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests/Registration/ServiceExposureResolverTests.cs`

- [ ] **Step 1: 写失败测试**

新建 `ServiceExposureResolverTests.cs`：

```csharp
using FluentAssertions;
using Tw.DependencyInjection.Abstractions;
using Tw.DependencyInjection.Registration;
using Xunit;

namespace Tw.DependencyInjection.Tests.Registration;

public class ServiceExposureResolverTests
{
    private interface IOrderService;
    private interface IRepository<TEntity>;

    private sealed class OrderService : IOrderService, IScopedDependency;

    [ExposeServices(typeof(IOrderService), IncludeSelf = true)]
    private sealed class ExplicitOrderService : IOrderService, IScopedDependency;

    [ExposeKeyedService(typeof(IOrderService), "primary")]
    private sealed class KeyedOrderService : IOrderService, IScopedDependency;

    private sealed class Repository<TEntity> : IRepository<TEntity>, IScopedDependency;

    [Fact]
    public void Resolve_DefaultExposesSelfAndMatchingInterface()
    {
        var exposures = ServiceExposureResolver.Resolve(typeof(OrderService));

        exposures.Should().Contain(e => e.ServiceType == typeof(OrderService) && e.Key is null);
        exposures.Should().Contain(e => e.ServiceType == typeof(IOrderService) && e.Key is null);
        exposures.Should().NotContain(e => e.ServiceType == typeof(IScopedDependency));
    }

    [Fact]
    public void Resolve_ExplicitExposeServicesHonorsIncludeSelf()
    {
        var exposures = ServiceExposureResolver.Resolve(typeof(ExplicitOrderService));

        exposures.Should().Contain(e => e.ServiceType == typeof(IOrderService) && e.Key is null);
        exposures.Should().Contain(e => e.ServiceType == typeof(ExplicitOrderService) && e.Key is null);
    }

    [Fact]
    public void Resolve_KeyedExposureCarriesKey()
    {
        var exposures = ServiceExposureResolver.Resolve(typeof(KeyedOrderService));

        exposures.Should().ContainSingle(e => e.ServiceType == typeof(IOrderService) && Equals(e.Key, "primary"));
    }

    [Fact]
    public void Resolve_OpenGenericExposesGenericInterfaceDefinition()
    {
        var exposures = ServiceExposureResolver.Resolve(typeof(Repository<>));

        exposures.Should().Contain(e => e.ServiceType == typeof(IRepository<>) && e.Key is null);
    }
}
```

- [ ] **Step 2: 运行测试确认失败**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests/Tw.DependencyInjection.Tests.csproj`

Expected: 编译失败，`ServiceExposureResolver` 不存在。

- [ ] **Step 3: 实现 ServiceExposure**

```csharp
namespace Tw.DependencyInjection.Registration;

internal sealed record ServiceExposure(Type ServiceType, object? Key);
```

- [ ] **Step 4: 实现 ServiceExposureResolver**

```csharp
using Tw.Configuration.Abstractions;
using Tw.DependencyInjection.Abstractions;
using Tw.DynamicProxy.Abstractions;

namespace Tw.DependencyInjection.Registration;

internal static class ServiceExposureResolver
{
    public static IReadOnlyList<ServiceExposure> Resolve(Type implementationType)
    {
        ArgumentNullException.ThrowIfNull(implementationType);

        var exposures = new List<ServiceExposure>();
        var explicitExposes = implementationType
            .GetCustomAttributes(typeof(ExposeServicesAttribute), inherit: false)
            .OfType<ExposeServicesAttribute>()
            .ToList();

        if (explicitExposes.Count > 0)
        {
            foreach (var expose in explicitExposes)
            {
                exposures.AddRange(expose.ServiceTypes.Select(serviceType => new ServiceExposure(NormalizeGenericServiceType(serviceType), null)));
                if (expose.IncludeSelf)
                {
                    exposures.Add(new ServiceExposure(implementationType, null));
                }
            }
        }
        else
        {
            exposures.Add(new ServiceExposure(implementationType, null));
            exposures.AddRange(DefaultInterfaceExposures(implementationType));
        }

        foreach (var keyed in implementationType
            .GetCustomAttributes(typeof(ExposeKeyedServiceAttribute), inherit: false)
            .OfType<ExposeKeyedServiceAttribute>())
        {
            exposures.Add(new ServiceExposure(NormalizeGenericServiceType(keyed.ServiceType), keyed.Key));
        }

        return exposures
            .Distinct()
            .ToList();
    }

    private static IEnumerable<ServiceExposure> DefaultInterfaceExposures(Type implementationType)
    {
        foreach (var interfaceType in implementationType.GetInterfaces())
        {
            if (IsFrameworkOrMarkerInterface(interfaceType))
            {
                continue;
            }

            var normalized = NormalizeGenericServiceType(interfaceType);
            if (IsNameMatchedInterface(implementationType, normalized) || IsOpenGenericContract(implementationType, normalized))
            {
                yield return new ServiceExposure(normalized, null);
            }
        }
    }

    private static Type NormalizeGenericServiceType(Type serviceType)
    {
        return serviceType.IsGenericType ? serviceType.GetGenericTypeDefinition() : serviceType;
    }

    private static bool IsNameMatchedInterface(Type implementationType, Type serviceType)
    {
        if (!serviceType.IsInterface)
        {
            return false;
        }

        var implementationName = implementationType.IsGenericType
            ? implementationType.Name[..implementationType.Name.IndexOf('`')]
            : implementationType.Name;

        var serviceName = serviceType.IsGenericType
            ? serviceType.Name[..serviceType.Name.IndexOf('`')]
            : serviceType.Name;

        return string.Equals(serviceName, "I" + implementationName, StringComparison.Ordinal);
    }

    private static bool IsOpenGenericContract(Type implementationType, Type serviceType)
    {
        return implementationType.IsGenericTypeDefinition
            && serviceType.IsInterface
            && serviceType.IsGenericTypeDefinition;
    }

    private static bool IsFrameworkOrMarkerInterface(Type interfaceType)
    {
        var normalized = NormalizeGenericServiceType(interfaceType);
        return normalized == typeof(ITransientDependency)
            || normalized == typeof(IScopedDependency)
            || normalized == typeof(ISingletonDependency)
            || normalized == typeof(IConfigurableOptions)
            || normalized == typeof(IInterceptor)
            || normalized == typeof(IDisposable)
            || normalized == typeof(IAsyncDisposable);
    }
}
```

- [ ] **Step 5: 运行测试确认通过**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests/Tw.DependencyInjection.Tests.csproj`

Expected: PASS。

- [ ] **Step 6: 提交**

```bash
git add backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Registration/ServiceExposure.cs backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Registration/ServiceExposureResolver.cs backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests/Registration/ServiceExposureResolverTests.cs
git commit -m "feat(di): resolve default explicit and keyed exposures"
```

---

## Task 5: 优先级与程序集可达关系

**Files:**
- Create: `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Registration/AssemblyReachabilityGraph.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Registration/ServicePriorityResolver.cs`
- Modify: `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Discovery/AssemblyDiscoverer.cs`
- Test: `backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests/Registration/ServicePriorityResolverTests.cs`

- [ ] **Step 1: 写失败测试**

新建 `ServicePriorityResolverTests.cs`：

```csharp
using System.Reflection;
using FluentAssertions;
using Tw.DependencyInjection.Abstractions;
using Tw.DependencyInjection.Registration;
using Xunit;

namespace Tw.DependencyInjection.Tests.Registration;

public class ServicePriorityResolverTests
{
    [ServicePriority(20)]
    [ServiceRegistration(Priority = 20)]
    private sealed class TypePriorityService;

    [ServicePriority(20)]
    [ServiceRegistration(Priority = 10)]
    private sealed class ConflictingTypePriorityService;

    [Fact]
    public void ResolveTypePriority_UsesExplicitPriority()
    {
        ServicePriorityResolver.ResolveTypePriority(typeof(TypePriorityService)).Should().Be(20);
    }

    [Fact]
    public void ResolveTypePriority_FailsWhenTwoAttributesDisagree()
    {
        var act = () => ServicePriorityResolver.ResolveTypePriority(typeof(ConflictingTypePriorityService));

        act.Should().Throw<ServiceRegistrationException>()
            .WithMessage("*类型优先级声明不一致*");
    }

    [Fact]
    public void ResolveAssemblyPriority_ConfigOverridesAttribute()
    {
        var options = new ServiceRegistrationOptions();
        options.AssemblyPriorities.Add(typeof(TypePriorityService).Assembly.GetName().Name!, 50);

        ServicePriorityResolver.ResolveAssemblyPriority(typeof(TypePriorityService).Assembly, options)
            .Should().Be(50);
    }

    [Fact]
    public void CalculateFinalPriority_UsesTopologyBaseAssemblyAndTypePriority()
    {
        ServicePriorityResolver.CalculateFinalPriority(topologyLevel: 2, assemblyPriority: 30, typePriority: 40)
            .Should().Be(2_000_070);
    }

    [Fact]
    public void ReachabilityGraph_DetectsTransitiveDependencyPath()
    {
        var graph = new AssemblyReachabilityGraph(new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
        {
            ["Tw.App"] = ["Tw.Domain"],
            ["Tw.Domain"] = ["Tw.Core"],
            ["Tw.Core"] = [],
        });

        graph.CanReach("Tw.App", "Tw.Core").Should().BeTrue();
        graph.CanReach("Tw.Core", "Tw.App").Should().BeFalse();
    }
}
```

- [ ] **Step 2: 运行测试确认失败**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests/Tw.DependencyInjection.Tests.csproj`

Expected: 编译失败，`ServicePriorityResolver` 与 `AssemblyReachabilityGraph` 不存在。

- [ ] **Step 3: 实现 AssemblyReachabilityGraph**

```csharp
namespace Tw.DependencyInjection.Registration;

internal sealed class AssemblyReachabilityGraph
{
    private readonly IReadOnlyDictionary<string, IReadOnlyList<string>> _referencesByAssemblyName;

    public AssemblyReachabilityGraph(IReadOnlyDictionary<string, IReadOnlyList<string>> referencesByAssemblyName)
    {
        _referencesByAssemblyName = referencesByAssemblyName;
    }

    public bool CanReach(string fromAssemblyName, string toAssemblyName)
    {
        var visited = new HashSet<string>(StringComparer.Ordinal);
        return Visit(fromAssemblyName);

        bool Visit(string current)
        {
            if (!visited.Add(current))
            {
                return false;
            }

            if (!_referencesByAssemblyName.TryGetValue(current, out var references))
            {
                return false;
            }

            foreach (var reference in references)
            {
                if (string.Equals(reference, toAssemblyName, StringComparison.Ordinal) || Visit(reference))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
```

- [ ] **Step 4: 实现 ServicePriorityResolver**

```csharp
using Tw.DependencyInjection.Abstractions;

namespace Tw.DependencyInjection.Registration;

internal static class ServicePriorityResolver
{
    public const int TopologyLevelStep = 1_000_000;
    public const int ExplicitPriorityMin = -100_000;
    public const int ExplicitPriorityMax = 100_000;

    public static int ResolveAssemblyPriority(Assembly assembly, ServiceRegistrationOptions options)
    {
        var assemblyName = assembly.GetName().Name
            ?? throw new ServiceRegistrationException("程序集名称为空，无法解析程序集优先级");

        if (options.AssemblyPriorities.TryGetValue(assemblyName, out var configuredPriority))
        {
            ValidateExplicitPriority(configuredPriority, "程序集优先级");
            return configuredPriority;
        }

        var attribute = assembly
            .GetCustomAttributes(typeof(TwAssemblyPriorityAttribute))
            .OfType<TwAssemblyPriorityAttribute>()
            .SingleOrDefault();

        var priority = attribute?.Priority ?? 0;
        ValidateExplicitPriority(priority, "程序集优先级");
        return priority;
    }

    public static int ResolveTypePriority(Type type)
    {
        var servicePriority = type
            .GetCustomAttributes(typeof(ServicePriorityAttribute), inherit: false)
            .OfType<ServicePriorityAttribute>()
            .SingleOrDefault();

        var registrationPriority = type
            .GetCustomAttributes(typeof(ServiceRegistrationAttribute), inherit: false)
            .OfType<ServiceRegistrationAttribute>()
            .SingleOrDefault()
            ?.Priority;

        if (servicePriority is not null && registrationPriority is not null && servicePriority.Priority != registrationPriority.Value)
        {
            throw new ServiceRegistrationException($"类型优先级声明不一致: {type.FullName}");
        }

        var priority = servicePriority?.Priority ?? registrationPriority ?? 0;
        ValidateExplicitPriority(priority, "类型优先级");
        return priority;
    }

    public static long CalculateFinalPriority(int topologyLevel, int assemblyPriority, int typePriority)
    {
        ValidateExplicitPriority(assemblyPriority, "程序集优先级");
        ValidateExplicitPriority(typePriority, "类型优先级");
        return (long)topologyLevel * TopologyLevelStep + assemblyPriority + typePriority;
    }

    private static void ValidateExplicitPriority(int priority, string label)
    {
        if (priority is < ExplicitPriorityMin or > ExplicitPriorityMax)
        {
            throw new ServiceRegistrationException($"{label}超出允许范围 {ExplicitPriorityMin}..{ExplicitPriorityMax}: {priority}");
        }
    }
}
```

在文件顶部补 `using System.Reflection;`。

- [ ] **Step 5: 扩展 AssemblyDiscoverer 结果**

把 `AssemblyDiscoveryResult` 改为：

```csharp
internal sealed record AssemblyDiscoveryResult(
    IReadOnlyList<Assembly> OrderedAssemblies,
    ServiceRegistrationReport Report,
    AssemblyReachabilityGraph ReachabilityGraph);
```

在 `Discover` 中构造 descriptors 后追加引用表：

```csharp
var referencesByAssemblyName = descriptors.ToDictionary(
    descriptor => descriptor.Name,
    descriptor => descriptor.ReferencedAssemblyNames
        .Where(includedSet.Contains)
        .ToList()
        as IReadOnlyList<string>,
    StringComparer.Ordinal);
```

返回值改为：

```csharp
return new AssemblyDiscoveryResult(
    orderedAssemblies,
    report,
    new AssemblyReachabilityGraph(referencesByAssemblyName));
```

- [ ] **Step 6: 运行测试确认通过**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests/Tw.DependencyInjection.Tests.csproj`

Expected: PASS。

- [ ] **Step 7: 提交**

```bash
git add backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Registration/AssemblyReachabilityGraph.cs backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Registration/ServicePriorityResolver.cs backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Discovery/AssemblyDiscoverer.cs backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests/Registration/ServicePriorityResolverTests.cs
git commit -m "feat(di): add service priority and assembly reachability"
```

---

## Task 6: 注册规划与单实现仲裁

**Files:**
- Create: `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Registration/ServiceCandidate.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Registration/ServiceRegistrationPlan.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Registration/ServiceRegistrationPlanner.cs`
- Test: `backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests/Registration/ServiceRegistrationPlannerTests.cs`

- [ ] **Step 1: 追加失败测试**

在 `ServiceRegistrationPlannerTests.cs` 追加：

```csharp
using System.Reflection;
using Tw.DependencyInjection.Registration;

private interface IPaymentProvider;

private sealed class DefaultPaymentProvider : IPaymentProvider, IScopedDependency;

[ServicePriority(10)]
private sealed class PreferredPaymentProvider : IPaymentProvider, IScopedDependency;

[ExposeKeyedService(typeof(IPaymentProvider), "wechat")]
private sealed class WechatPaymentProvider : IPaymentProvider, IScopedDependency;

[ExposeKeyedService(typeof(IPaymentProvider), "wechat")]
[ServicePriority(5)]
private sealed class PreferredWechatPaymentProvider : IPaymentProvider, IScopedDependency;

[Fact]
public void Planner_SelectsHighestPriorityNonKeyedCandidate()
{
    var plan = PlanTypes(typeof(DefaultPaymentProvider), typeof(PreferredPaymentProvider));

    plan.Registrations.Should().ContainSingle(r =>
        r.ServiceType == typeof(IPaymentProvider)
        && r.ImplementationType == typeof(PreferredPaymentProvider)
        && r.Key is null);
    plan.Report.SupersededCandidates.Should().ContainSingle(s =>
        s.ImplementationTypeName.Contains(nameof(DefaultPaymentProvider), StringComparison.Ordinal));
}

[Fact]
public void Planner_ArbitratesKeyedCandidatesPerKey()
{
    var plan = PlanTypes(typeof(WechatPaymentProvider), typeof(PreferredWechatPaymentProvider));

    plan.Registrations.Should().ContainSingle(r =>
        r.ServiceType == typeof(IPaymentProvider)
        && r.ImplementationType == typeof(PreferredWechatPaymentProvider)
        && Equals(r.Key, "wechat"));
}

[Fact]
public void Planner_ThrowsWhenFinalPriorityTies()
{
    var act = () => PlanTypes(typeof(DefaultPaymentProvider), typeof(DefaultPaymentProviderClone));

    act.Should().Throw<ServiceRegistrationException>()
        .WithMessage("*最终优先级相同*");
}

private sealed class DefaultPaymentProviderClone : IPaymentProvider, IScopedDependency;

private static ServiceRegistrationPlan PlanTypes(params Type[] types)
{
    var assembly = typeof(ServiceRegistrationPlannerTests).Assembly;
    return ServiceRegistrationPlanner.Plan(
        assemblies: [assembly],
        typesByAssemblyName: new Dictionary<string, IReadOnlyList<Type>>(StringComparer.Ordinal)
        {
            [assembly.GetName().Name!] = types,
        },
        topologyLevelsByAssemblyName: new Dictionary<string, int>(StringComparer.Ordinal)
        {
            [assembly.GetName().Name!] = 0,
        },
        reachabilityGraph: new AssemblyReachabilityGraph(new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
        {
            [assembly.GetName().Name!] = [],
        }),
        options: new ServiceRegistrationOptions());
}
```

- [ ] **Step 2: 运行测试确认失败**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests/Tw.DependencyInjection.Tests.csproj`

Expected: 编译失败，`ServiceRegistrationPlanner`、`ServiceRegistrationPlan` 不存在。

- [ ] **Step 3: 实现 ServiceCandidate 与 ServiceRegistrationPlan**

`ServiceCandidate.cs`：

```csharp
using Tw.DependencyInjection.Abstractions;

namespace Tw.DependencyInjection.Registration;

internal sealed record ServiceCandidate(
    Type ServiceType,
    Type ImplementationType,
    object? Key,
    DependencyLifetime Lifetime,
    string AssemblyName,
    int TopologyLevel,
    int AssemblyPriority,
    int TypePriority,
    long FinalPriority,
    int DiscoveryOrder);
```

`ServiceRegistrationPlan.cs`：

```csharp
using Tw.DependencyInjection.Diagnostics;

namespace Tw.DependencyInjection.Registration;

internal sealed record ServiceRegistrationPlan(
    IReadOnlyList<ServiceCandidate> Registrations,
    ServiceRegistrationReport Report);
```

- [ ] **Step 4: 实现 ServiceRegistrationPlanner**

实现要点：

```csharp
internal static class ServiceRegistrationPlanner
{
    public static ServiceRegistrationPlan Plan(
        IReadOnlyList<Assembly> assemblies,
        IReadOnlyDictionary<string, IReadOnlyList<Type>> typesByAssemblyName,
        IReadOnlyDictionary<string, int> topologyLevelsByAssemblyName,
        AssemblyReachabilityGraph reachabilityGraph,
        ServiceRegistrationOptions options)
    {
        // 1. 按程序集拓扑顺序和类型 FullName 排序生成稳定 DiscoveryOrder
        // 2. 对每个类型执行 skip、participant、lifetime、exposure、priority
        // 3. 按 (ServiceType, Key) 分组
        // 4. 每组选择 FinalPriority 最高者
        // 5. 相同 FinalPriority 启动失败
        // 6. 两候选仅由拓扑基础值区分且程序集互不可达时启动失败
        // 7. 返回最终 registrations 与完整诊断报告
    }
}
```

候选构造时使用：

```csharp
var finalPriority = ServicePriorityResolver.CalculateFinalPriority(
    topologyLevel,
    assemblyPriority,
    typePriority);
```

仲裁失败消息使用：

```csharp
throw new ServiceRegistrationException(
    $"服务契约 {serviceType.FullName} 的候选最终优先级相同，无法仲裁唯一实现");
```

平级无依赖边失败条件：

```csharp
var onlyTopologyDiffers = left.AssemblyPriority == right.AssemblyPriority
    && left.TypePriority == right.TypePriority
    && left.TopologyLevel != right.TopologyLevel;

var assembliesArePeers = !reachabilityGraph.CanReach(left.AssemblyName, right.AssemblyName)
    && !reachabilityGraph.CanReach(right.AssemblyName, left.AssemblyName);

if (onlyTopologyDiffers && assembliesArePeers)
{
    throw new ServiceRegistrationException(
        $"服务契约 {serviceType.FullName} 的候选位于平级程序集，必须通过程序集或类型优先级显式仲裁");
}
```

- [ ] **Step 5: 运行测试确认通过**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests/Tw.DependencyInjection.Tests.csproj`

Expected: PASS。

- [ ] **Step 6: 提交**

```bash
git add backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Registration/ServiceCandidate.cs backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Registration/ServiceRegistrationPlan.cs backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Registration/ServiceRegistrationPlanner.cs backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests/Registration/ServiceRegistrationPlannerTests.cs
git commit -m "feat(di): plan service registrations with single implementation arbitration"
```

---

## Task 7: IServiceCollection 执行器

**Files:**
- Create: `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Registration/ServiceRegistrationExecutor.cs`
- Test: `backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests/Registration/ServiceRegistrationExecutorTests.cs`

- [ ] **Step 1: 写失败测试**

新建 `ServiceRegistrationExecutorTests.cs`：

```csharp
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Tw.DependencyInjection.Abstractions;
using Tw.DependencyInjection.Diagnostics;
using Tw.DependencyInjection.Registration;
using Xunit;

namespace Tw.DependencyInjection.Tests.Registration;

public class ServiceRegistrationExecutorTests
{
    private interface IOrderService;
    private sealed class OrderService : IOrderService;
    private interface IPaymentProvider;
    private sealed class WechatPaymentProvider : IPaymentProvider;

    [Fact]
    public void Apply_RegistersNonKeyedWinner()
    {
        var services = new ServiceCollection();
        var plan = CreatePlan(new ServiceCandidate(
            typeof(IOrderService),
            typeof(OrderService),
            Key: null,
            DependencyLifetime.Scoped,
            AssemblyName: "Sample",
            TopologyLevel: 0,
            AssemblyPriority: 0,
            TypePriority: 0,
            FinalPriority: 0,
            DiscoveryOrder: 0));

        ServiceRegistrationExecutor.Apply(services, plan);

        services.Should().ContainSingle(d => d.ServiceType == typeof(IOrderService));
    }

    [Fact]
    public void Apply_RegistersKeyedServiceAndEnumerableEntry()
    {
        var services = new ServiceCollection();
        var plan = CreatePlan(new ServiceCandidate(
            typeof(IPaymentProvider),
            typeof(WechatPaymentProvider),
            Key: "wechat",
            DependencyLifetime.Scoped,
            AssemblyName: "Sample",
            TopologyLevel: 0,
            AssemblyPriority: 0,
            TypePriority: 0,
            FinalPriority: 0,
            DiscoveryOrder: 0));

        ServiceRegistrationExecutor.Apply(services, plan);
        using var provider = services.BuildServiceProvider();

        provider.GetRequiredKeyedService<IPaymentProvider>("wechat")
            .Should().BeOfType<WechatPaymentProvider>();
        provider.GetServices<KeyedServiceEntry<IPaymentProvider>>()
            .Should().ContainSingle(e => Equals(e.Key, "wechat") && e.Service is WechatPaymentProvider);
    }

    private static ServiceRegistrationPlan CreatePlan(params ServiceCandidate[] registrations)
    {
        return new ServiceRegistrationPlan(
            registrations,
            new ServiceRegistrationReport([], [], []));
    }
}
```

- [ ] **Step 2: 运行测试确认失败**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests/Tw.DependencyInjection.Tests.csproj`

Expected: 编译失败，`ServiceRegistrationExecutor` 不存在。

- [ ] **Step 3: 实现 ServiceRegistrationExecutor**

实现要点：

```csharp
using Microsoft.Extensions.DependencyInjection;
using Tw.DependencyInjection.Abstractions;

namespace Tw.DependencyInjection.Registration;

internal static class ServiceRegistrationExecutor
{
    public static void Apply(IServiceCollection services, ServiceRegistrationPlan plan)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(plan);

        foreach (var registration in plan.Registrations)
        {
            if (registration.Key is null)
            {
                RemoveExistingNonKeyedDescriptors(services, registration.ServiceType);
                AddNonKeyed(services, registration);
            }
            else
            {
                AddKeyed(services, registration);
                AddKeyedEntry(services, registration);
            }
        }
    }

    private static void AddNonKeyed(IServiceCollection services, ServiceCandidate registration)
    {
        services.Add(ServiceDescriptor.Describe(
            registration.ServiceType,
            registration.ImplementationType,
            DependencyLifetimeMapper.Map(registration.Lifetime)));
    }

    private static void AddKeyed(IServiceCollection services, ServiceCandidate registration)
    {
        var lifetime = DependencyLifetimeMapper.Map(registration.Lifetime);
        services.Add(ServiceDescriptor.DescribeKeyed(
            registration.ServiceType,
            registration.Key,
            registration.ImplementationType,
            lifetime));
    }

    private static void AddKeyedEntry(IServiceCollection services, ServiceCandidate registration)
    {
        var entryType = typeof(KeyedServiceEntry<>).MakeGenericType(registration.ServiceType);
        services.Add(ServiceDescriptor.Describe(
            entryType,
            provider =>
            {
                var service = provider.GetRequiredKeyedService(registration.ServiceType, registration.Key);
                return Activator.CreateInstance(entryType, registration.Key!, service)!;
            },
            DependencyLifetimeMapper.Map(registration.Lifetime)));
    }

    private static void RemoveExistingNonKeyedDescriptors(IServiceCollection services, Type serviceType)
    {
        for (var index = services.Count - 1; index >= 0; index--)
        {
            if (services[index].ServiceType == serviceType && services[index].ServiceKey is null)
            {
                services.RemoveAt(index);
            }
        }
    }
}
```

- [ ] **Step 4: 运行测试确认通过**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests/Tw.DependencyInjection.Tests.csproj`

Expected: PASS。

- [ ] **Step 5: 提交**

```bash
git add backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Registration/ServiceRegistrationExecutor.cs backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests/Registration/ServiceRegistrationExecutorTests.cs
git commit -m "feat(di): execute planned registrations into IServiceCollection"
```

---

## Task 8: AddServiceRegistration 入口与 keyed 构造函数校验

**Files:**
- Create: `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Registration/ConstructorKeyedServiceValidator.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/ServiceCollectionRegistrationExtensions.cs`
- Test: `backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests/Hosting/AddServiceRegistrationIntegrationTests.cs`

- [ ] **Step 1: 写失败集成测试**

新建 `AddServiceRegistrationIntegrationTests.cs`：

```csharp
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tw.DependencyInjection;
using Tw.DependencyInjection.Abstractions;
using Tw.DependencyInjection.Diagnostics;
using Xunit;

namespace Tw.DependencyInjection.Tests.Hosting;

public class AddServiceRegistrationIntegrationTests
{
    private interface IOrderService;
    private sealed class OrderService : IOrderService, IScopedDependency;

    private interface IPaymentProvider;

    [ExposeKeyedService(typeof(IPaymentProvider), "wechat")]
    private sealed class WechatPaymentProvider : IPaymentProvider, IScopedDependency;

    private sealed class CheckoutService : IScopedDependency
    {
        public CheckoutService([FromKeyedServices("wechat")] IPaymentProvider provider)
        {
            Provider = provider;
        }

        public IPaymentProvider Provider { get; }
    }

    [Fact]
    public void AddServiceRegistration_RegistersDiscoveredServicesAndReport()
    {
        var services = new ServiceCollection();
        var configuration = ConfigurationForThisTestAssembly();

        services.AddServiceRegistration(configuration);
        using var provider = services.BuildServiceProvider();

        provider.GetRequiredService<IOrderService>().Should().BeOfType<OrderService>();
        provider.GetRequiredKeyedService<IPaymentProvider>("wechat").Should().BeOfType<WechatPaymentProvider>();
        provider.GetRequiredService<CheckoutService>().Provider.Should().BeOfType<WechatPaymentProvider>();
        provider.GetRequiredService<ServiceRegistrationReport>().Registrations.Should().NotBeEmpty();
    }

    private static IConfiguration ConfigurationForThisTestAssembly()
    {
        var assemblyName = typeof(AddServiceRegistrationIntegrationTests).Assembly.GetName().Name!;
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Tw:DependencyInjection:IncludeAssemblies:0"] = assemblyName,
            })
            .Build();
    }
}
```

- [ ] **Step 2: 运行测试确认失败**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests/Tw.DependencyInjection.Tests.csproj`

Expected: 编译失败，`AddServiceRegistration` 不存在。

- [ ] **Step 3: 实现 ConstructorKeyedServiceValidator**

```csharp
using Microsoft.Extensions.DependencyInjection;

namespace Tw.DependencyInjection;

internal static class ConstructorKeyedServiceValidator
{
    public static void Validate(IReadOnlyList<ServiceCandidate> registrations)
    {
        var keyedContracts = registrations
            .Where(r => r.Key is not null)
            .Select(r => (r.ServiceType, r.Key))
            .ToHashSet();

        foreach (var registration in registrations)
        {
            foreach (var constructor in registration.ImplementationType.GetConstructors())
            {
                foreach (var parameter in constructor.GetParameters())
                {
                    var keyed = parameter
                        .GetCustomAttributes(typeof(FromKeyedServicesAttribute), inherit: false)
                        .OfType<FromKeyedServicesAttribute>()
                        .SingleOrDefault();

                    if (keyed is null)
                    {
                        continue;
                    }

                    if (!keyedContracts.Contains((parameter.ParameterType, keyed.Key)))
                    {
                        throw new ServiceRegistrationException(
                            $"构造函数参数 {registration.ImplementationType.FullName}.{parameter.Name} 指向未注册 keyed service: {parameter.ParameterType.FullName} / {keyed.Key}");
                    }
                }
            }
        }
    }
}
```

- [ ] **Step 4: 实现 AddServiceRegistration**

```csharp
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tw.DependencyInjection.Diagnostics;
using Tw.DependencyInjection.Discovery;

namespace Tw.DependencyInjection.Registration;

/// <summary>
/// 自动发现并注册服务的 <see cref="IServiceCollection"/> 扩展
/// </summary>
public static class ServiceCollectionRegistrationExtensions
{
    /// <summary>
    /// 根据配置节 <c>Tw:DependencyInjection</c> 发现程序集并注册服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">应用配置根</param>
    /// <returns>同一服务集合，便于链式调用</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> 或 <paramref name="configuration"/> 为 null 时抛出</exception>
    public static IServiceCollection AddServiceRegistration(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var options = new ServiceRegistrationOptions();
        configuration.GetSection("Tw:DependencyInjection").Bind(options);

        var discovery = AssemblyDiscoverer.Discover(options, new RuntimeAssemblySource());
        var topologyLevels = discovery.Report.Topology.ToDictionary(
            entry => entry.AssemblyName,
            entry => entry.Level,
            StringComparer.Ordinal);

        var typesByAssemblyName = discovery.OrderedAssemblies.ToDictionary(
            assembly => assembly.GetName().Name!,
            assembly => SafeGetTypes(assembly),
            StringComparer.Ordinal);

        var plan = ServiceRegistrationPlanner.Plan(
            discovery.OrderedAssemblies,
            typesByAssemblyName,
            topologyLevels,
            discovery.ReachabilityGraph,
            options);

        ConstructorKeyedServiceValidator.Validate(plan.Registrations);
        ServiceRegistrationExecutor.Apply(services, plan);
        services.AddSingleton(plan.Report);
        return services;
    }

    private static IReadOnlyList<Type> SafeGetTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.Where(type => type is not null).Select(type => type!).ToList();
        }
    }
}
```

- [ ] **Step 5: 补充内部命名空间 using**

在 `ServiceCollectionRegistrationExtensions.cs` 顶部包含：

```csharp
using Tw.DependencyInjection.Registration;
```

公开扩展文件位于项目根目录，命名空间为 `Tw.DependencyInjection`，满足命名空间与文件夹路径规则，并保证消费端通过 `using Tw.DependencyInjection;` 发现 `AddServiceRegistration`。

- [ ] **Step 6: 运行测试确认通过**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests/Tw.DependencyInjection.Tests.csproj`

Expected: PASS。

- [ ] **Step 7: 提交**

```bash
git add backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Registration backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/ServiceCollectionRegistrationExtensions.cs backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests/Hosting/AddServiceRegistrationIntegrationTests.cs
git commit -m "feat(di): add service registration entry point"
```

---

## Task 9: open generic 与 `[FromKeyedServices]` 缺失 key 回归

**Files:**
- Modify: `backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests/Hosting/AddServiceRegistrationIntegrationTests.cs`

- [ ] **Step 1: 追加 open generic 集成测试**

在 `AddServiceRegistrationIntegrationTests.cs` 追加：

```csharp
private interface IRepository<TEntity>;
private sealed class Repository<TEntity> : IRepository<TEntity>, IScopedDependency;
private sealed class OrderEntity;

[Fact]
public void AddServiceRegistration_RegistersOpenGenericContract()
{
    var services = new ServiceCollection();
    var configuration = ConfigurationForThisTestAssembly();

    services.AddServiceRegistration(configuration);
    using var provider = services.BuildServiceProvider();

    provider.GetRequiredService<IRepository<OrderEntity>>()
        .Should().BeOfType<Repository<OrderEntity>>();
}
```

- [ ] **Step 2: 追加缺失 keyed service 启动失败测试**

追加：

```csharp
private interface IMissingProvider;

private sealed class MissingKeyConsumer : IScopedDependency
{
    public MissingKeyConsumer([FromKeyedServices("missing")] IMissingProvider provider)
    {
        Provider = provider;
    }

    public IMissingProvider Provider { get; }
}

[Fact]
public void AddServiceRegistration_ThrowsWhenFromKeyedServicesReferencesMissingKey()
{
    var services = new ServiceCollection();
    var configuration = ConfigurationForThisTestAssembly();

    var act = () => services.AddServiceRegistration(configuration);

    act.Should().Throw<ServiceRegistrationException>()
        .WithMessage("*未注册 keyed service*");
}
```

- [ ] **Step 3: 将缺失 keyed 测试隔离到独立测试程序集或局部类型来源**

`MissingKeyConsumer` 会让同一测试程序集内所有成功集成测试失败。把 Task 8 的 `AddServiceRegistration` 内部发现逻辑保留，另在测试中通过 `ServiceRegistrationPlanner.Plan` 构造只含 `MissingKeyConsumer` 的局部类型来源并直接调用 `ConstructorKeyedServiceValidator.Validate`：

```csharp
[Fact]
public void ConstructorKeyedServiceValidator_ThrowsWhenFromKeyedServicesReferencesMissingKey()
{
    var plan = ServiceRegistrationPlanner.Plan(
        assemblies: [typeof(MissingKeyConsumer).Assembly],
        typesByAssemblyName: new Dictionary<string, IReadOnlyList<Type>>(StringComparer.Ordinal)
        {
            [typeof(MissingKeyConsumer).Assembly.GetName().Name!] = [typeof(MissingKeyConsumer)],
        },
        topologyLevelsByAssemblyName: new Dictionary<string, int>(StringComparer.Ordinal)
        {
            [typeof(MissingKeyConsumer).Assembly.GetName().Name!] = 0,
        },
        reachabilityGraph: new AssemblyReachabilityGraph(new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
        {
            [typeof(MissingKeyConsumer).Assembly.GetName().Name!] = [],
        }),
        options: new ServiceRegistrationOptions());

    var act = () => ConstructorKeyedServiceValidator.Validate(plan.Registrations);

    act.Should().Throw<ServiceRegistrationException>()
        .WithMessage("*未注册 keyed service*");
}
```

删除直接调用 `services.AddServiceRegistration(configuration)` 的缺失 key 测试，保留 validator 测试。

- [ ] **Step 4: 运行测试确认通过**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests/Tw.DependencyInjection.Tests.csproj`

Expected: PASS。

- [ ] **Step 5: 提交**

```bash
git add backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests/Hosting/AddServiceRegistrationIntegrationTests.cs
git commit -m "test(di): cover open generic and missing keyed registrations"
```

---

## Task 10: 文档、charter 与索引联动

**Files:**
- Modify: `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/package-charter.yaml`
- Modify: `docs/shared-packages/dotnet/Tw.DependencyInjection/README.md`
- Create: `docs/shared-packages/dotnet/Tw.DependencyInjection/service-registration.md`

- [ ] **Step 1: 更新 charter**

在 `in_scope` 追加：

```yaml
  - 自动服务注册参与判定、生命周期解析与服务暴露
  - 非 keyed 单实现仲裁、keyed service 与 open generic 注册
```

在 `public_capabilities` 保持：

```yaml
  - Tw.DependencyInjection
```

`out_of_scope` 保留 Options 自动装载与 AOP 承载边界。

- [ ] **Step 2: 更新 README 索引**

在 `docs/shared-packages/dotnet/Tw.DependencyInjection/README.md` 的能力索引追加：

```markdown
- [服务自动注册](service-registration.md)：生命周期标记、显式暴露、keyed service、open generic 与单实现仲裁（P2 落地）。
```

- [ ] **Step 3: 创建 service-registration How-to**

新建 `service-registration.md`：

```markdown
# 服务自动注册

## 能力定位

`Tw.DependencyInjection` 在 P2 阶段提供服务自动注册。业务类型只依赖 `Tw.Core` 中的 `Tw.DependencyInjection.Abstractions` 标记接口与特性，组合根调用 `AddServiceRegistration(IConfiguration)` 完成扫描、规划与注册。

## 注册入口

```csharp
using Tw.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseAutofac();
builder.Services.AddServiceRegistration(builder.Configuration);
```

`AddServiceRegistration` 读取 `Tw:DependencyInjection` 配置节，复用程序集扫描结果，生成 `ServiceRegistrationReport` 并注册为 singleton。

## 生命周期

服务类型通过以下任一方式声明生命周期：

```csharp
public sealed class OrderService : IOrderService, IScopedDependency
{
}

[ServiceRegistration(DependencyLifetime.Singleton)]
public sealed class CacheService : ICacheService
{
}
```

同一类型不得同时实现多个生命周期标记。未声明生命周期的类型不会注册。

## 暴露服务

默认暴露实现类自身，以及与实现类命名匹配的接口：

```csharp
public interface IOrderService
{
}

public sealed class OrderService : IOrderService, IScopedDependency
{
}
```

显式暴露使用 `[ExposeServices]`：

```csharp
[ExposeServices(typeof(IOrderService), IncludeSelf = true)]
public sealed class CustomOrderService : IOrderService, IScopedDependency
{
}
```

## Keyed Service

同一契约存在多个实现时使用 keyed service：

```csharp
[ExposeKeyedService(typeof(IPaymentProvider), "wechat")]
public sealed class WechatPaymentProvider : IPaymentProvider, IScopedDependency
{
}

public sealed class CheckoutService : IScopedDependency
{
    public CheckoutService([FromKeyedServices("wechat")] IPaymentProvider provider)
    {
        Provider = provider;
    }

    public IPaymentProvider Provider { get; }
}
```

需要枚举某契约的全部 keyed 实现时，注入 `IEnumerable<KeyedServiceEntry<TService>>`：

```csharp
public sealed class PaymentRouter : IScopedDependency
{
    public PaymentRouter(IEnumerable<KeyedServiceEntry<IPaymentProvider>> providers)
    {
        Providers = providers.ToList();
    }

    public IReadOnlyList<KeyedServiceEntry<IPaymentProvider>> Providers { get; }
}
```

## 单实现仲裁

非 keyed 契约最终只注册一个实现。多个候选通过 `TopologyBaseValue + AssemblyPriority + TypePriority` 仲裁，优先级高者胜出，落选候选记录到 `ServiceRegistrationReport.SupersededCandidates`。

程序集优先级配置：

```json
{
  "Tw": {
    "DependencyInjection": {
      "AssemblyPriorities": {
        "Tw.Order.Application": 100
      }
    }
  }
}
```

类型优先级：

```csharp
[ServicePriority(20)]
public sealed class PreferredOrderService : IOrderService, IScopedDependency
{
}
```

最终优先级相同，或平级程序集只靠拓扑顺序产生胜者时，启动失败。

## 注意事项

- `Replace = true`、`ReplaceServices`、`TryReplace` 不属于本包服务注册模型。
- Options 自动装载由 P3 提供，Options 类型不作为普通服务注册。
- AOP 动态代理由 P4 提供，P2 只完成服务注册。
- 诊断报告只输出类型、契约、key、优先级和原因，不输出配置值或方法参数值。
```

- [ ] **Step 4: 运行文档索引自检**

Run: `rg -n "服务自动注册|service-registration|AddServiceRegistration|Replace = true" docs/shared-packages/dotnet/Tw.DependencyInjection backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection`

Expected: 命中 README、How-to、注册入口代码和“Replace = true”禁止说明；源码中不出现 `Replace = true` 注册路径。

- [ ] **Step 5: 提交**

```bash
git add backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/package-charter.yaml docs/shared-packages/dotnet/Tw.DependencyInjection/README.md docs/shared-packages/dotnet/Tw.DependencyInjection/service-registration.md
git commit -m "docs(di): document automatic service registration"
```

---

## Task 11: 全量验证

**Files:**
- Verify only

- [ ] **Step 1: P2 专项测试**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests/Tw.DependencyInjection.Tests.csproj`

Expected: PASS。

- [ ] **Step 2: P0 回归测试**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Tw.Core.Tests.csproj`

Expected: PASS。

- [ ] **Step 3: BuildingBlocks 全量回归**

Run: `dotnet test backend/dotnet/Tw.SmartPlatform.slnx`

Expected: PASS。

- [ ] **Step 4: 依赖边界自检**

Run: `rg -n "Autofac|Castle|Microsoft.AspNetCore" backend/dotnet/BuildingBlocks/src/Tw.Core`

Expected: 无命中。

Run: `rg -n "Replace\\s*=\\s*true|ReplaceServices|TryReplace" backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests`

Expected: 无注册路径命中；文档中的禁止说明不在该源码命令范围内。

- [ ] **Step 5: P2 完成标志**

`Tw.DependencyInjection` 能从 `Tw:DependencyInjection` 配置节读取扫描与程序集优先级选项，发现参与注册的服务类型，注册非 keyed 单实现服务、keyed service、`IEnumerable<KeyedServiceEntry<TService>>` 与 open generic，并在冲突时启动失败。P3（Options 装载）和 P4（AOP 承载）可在此基础上开工。

---

## Self-Review

**Spec 覆盖（P2 行：参与注册、生命周期、暴露、keyed、open generic、单实现仲裁、`AddServiceRegistration()`）：**
- 参与注册判定与跳过规则 → Task 3 + Task 6
- 生命周期标记与 `[ServiceRegistration]` 生命周期覆盖 → Task 3
- 显式暴露、默认命名匹配、open generic 暴露 → Task 4
- keyed service、`KeyedServiceEntry<TService>` 枚举、`[FromKeyedServices]` 校验 → Task 4 + Task 7 + Task 8 + Task 9
- 最终优先级、程序集优先级配置、类型优先级、范围校验 → Task 5
- 非 keyed 单实现仲裁、superseded 诊断、优先级相同失败、平级程序集失败 → Task 6
- `Replace = true` 禁止路径 → Task 10 + Task 11 自检
- `AddServiceRegistration(IConfiguration)` → Task 8
- 使用文档与 charter 联动 → Task 10

**类型一致性核对：** `ServiceRegistrationOptions.AssemblyPriorities`、`ServiceCandidate`、`ServiceRegistrationPlan`、`ServiceExposure`、`ServiceRegistrationPlanner.Plan`、`ServiceRegistrationExecutor.Apply`、`ConstructorKeyedServiceValidator.Validate`、`AddServiceRegistration` 在定义任务与消费/测试任务中签名一致。

**占位扫描：** 本计划未保留空白待填内容或未指向具体文件的泛化步骤。Task 6 的规划器实现步骤给出完整算法约束、失败条件和诊断输出要求，同任务测试覆盖选择、淘汰与冲突路径。
