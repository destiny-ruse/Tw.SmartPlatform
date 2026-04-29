# Auto Registration Core Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build the P0 DI auto-registration core for lifecycle markers, service exposure metadata, conflict resolution, and Autofac registration.

**Architecture:** `Tw.Core.DependencyInjection` owns all container-neutral metadata and planning. `Tw.Core.DependencyInjection.Autofac` is the only layer that writes to Autofac `ContainerBuilder`, so scanning and conflict decisions stay testable without a container.

**Tech Stack:** .NET 10, Tw.Core, Autofac, xUnit, FluentAssertions

---

## Execution Order

Run after `2026-04-29-00-auto-registration-dependency-admission.md`. This plan must pass before `2026-04-29-02-options-auto-registration.md`.

## Source Inputs

- Design sections: § 2.3, § 3.2, § 3.3, § 3.5, § 4, § 5, § 10.1
- Knowledge discovery: reuse provider module `backend.dotnet.building-blocks.core`, evidence path `backend/dotnet/BuildingBlocks/src/Tw.Core`
- Standards: `rules.naming-dotnet#rules` version `1.1.0`, `docs/standards/rules/naming-dotnet.md`; `rules.comments-dotnet#rules` version `1.3.0`, `docs/standards/rules/comments-dotnet.md`; `rules.error-handling#rules` version `1.2.0`, `docs/standards/rules/error-handling.md`; `rules.test-strategy#rules` version `1.1.0`, `docs/standards/rules/test-strategy.md`

## File Structure

- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/DependencyInjection/ITransientDependency.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/DependencyInjection/IScopedDependency.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/DependencyInjection/ISingletonDependency.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/DependencyInjection/ExposeServicesAttribute.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/DependencyInjection/KeyedServiceAttribute.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/DependencyInjection/ReplaceServiceAttribute.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/DependencyInjection/DisableAutoRegistrationAttribute.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/DependencyInjection/CollectionServiceAttribute.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/DependencyInjection/AutoRegistrationOptions.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/DependencyInjection/AutoRegistrationException.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/DependencyInjection/AutoRegistrationAssemblySelector.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/DependencyInjection/ServiceExposureResolver.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/DependencyInjection/ServiceRegistrationCandidate.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/DependencyInjection/ServiceRegistrationEntry.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/DependencyInjection/ServiceRegistrationPlanner.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/DependencyInjection/AssemblyTopologySorter.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/DependencyInjection/Autofac/AutoRegistrationModule.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/DependencyInjection/Autofac/AutofacAutoRegistrationExtensions.cs`
- Create: `backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/DependencyInjection/ServiceExposureResolverTests.cs`
- Create: `backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/DependencyInjection/ServiceRegistrationPlannerTests.cs`
- Create: `backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/DependencyInjection/AssemblyTopologySorterTests.cs`
- Create: `backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/DependencyInjection/AutofacAutoRegistrationTests.cs`
- Modify: `backend/dotnet/BuildingBlocks/src/Tw.Core/Reflection/TypeFinder.cs` only if tests prove existing filtering prevents startup assembly fallback.

## Tasks

### Task 1: Add Lifecycle Markers and Registration Attributes

**Files:**
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/DependencyInjection/ITransientDependency.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/DependencyInjection/IScopedDependency.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/DependencyInjection/ISingletonDependency.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/DependencyInjection/ExposeServicesAttribute.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/DependencyInjection/KeyedServiceAttribute.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/DependencyInjection/ReplaceServiceAttribute.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/DependencyInjection/DisableAutoRegistrationAttribute.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/DependencyInjection/CollectionServiceAttribute.cs`
- Test: `backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/DependencyInjection/ServiceExposureResolverTests.cs`

- [ ] **Step 1: Write failing attribute tests**

```csharp
[Fact]
public void Registration_Attributes_Expose_Metadata()
{
    var expose = new ExposeServicesAttribute(typeof(IFirstService), typeof(ISecondService))
    {
        IncludeSelf = true
    };
    var keyed = new KeyedServiceAttribute("premium");
    var replace = new ReplaceServiceAttribute { Order = 10 };
    var collection = new CollectionServiceAttribute { Order = 20 };

    expose.ServiceTypes.Should().Equal(typeof(IFirstService), typeof(ISecondService));
    expose.IncludeSelf.Should().BeTrue();
    keyed.Key.Should().Be("premium");
    replace.Order.Should().Be(10);
    collection.Order.Should().Be(20);
}

private interface IFirstService;
private interface ISecondService;
```

- [ ] **Step 2: Run test to verify it fails**

Run:

```powershell
dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Tw.Core.Tests.csproj --filter Registration_Attributes_Expose_Metadata
```

Expected: FAIL because the attributes do not exist.

- [ ] **Step 3: Create lifecycle marker interfaces**

```csharp
namespace Tw.Core.DependencyInjection;

/// <summary>标记服务应按瞬时生命周期注册</summary>
public interface ITransientDependency;

/// <summary>标记服务应按作用域生命周期注册</summary>
public interface IScopedDependency;

/// <summary>标记服务应按单例生命周期注册</summary>
public interface ISingletonDependency;
```

- [ ] **Step 4: Create registration attributes**

```csharp
namespace Tw.Core.DependencyInjection;

/// <summary>声明自动注册时要暴露的服务类型</summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class ExposeServicesAttribute(params Type[] serviceTypes) : Attribute
{
    /// <summary>显式暴露的服务类型</summary>
    public IReadOnlyList<Type> ServiceTypes { get; } = serviceTypes;

    /// <summary>是否同时暴露实现类型本身</summary>
    public bool IncludeSelf { get; init; }
}

/// <summary>声明服务应注册为 Autofac keyed service</summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class KeyedServiceAttribute(object key) : Attribute
{
    /// <summary>服务键</summary>
    public object Key { get; } = key;
}

/// <summary>声明当前服务替换已存在的同服务注册</summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class ReplaceServiceAttribute : Attribute
{
    /// <summary>同程序集冲突时的优先级，数值越大越优先</summary>
    public int Order { get; init; }
}

/// <summary>声明当前类型不参与 DI 自动注册</summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class DisableAutoRegistrationAttribute : Attribute;

/// <summary>声明当前服务作为集合实现保留</summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class CollectionServiceAttribute : Attribute
{
    /// <summary>集合解析时的稳定排序值，数值越大越靠后</summary>
    public int Order { get; init; }
}
```

- [ ] **Step 5: Run test to verify it passes**

Run:

```powershell
dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Tw.Core.Tests.csproj --filter Registration_Attributes_Expose_Metadata
```

Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add backend/dotnet/BuildingBlocks/src/Tw.Core/DependencyInjection backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/DependencyInjection
git commit -m "feat: add auto registration metadata attributes"
```

### Task 2: Resolve Exposed Service Types

**Files:**
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/DependencyInjection/ServiceExposureResolver.cs`
- Test: `backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/DependencyInjection/ServiceExposureResolverTests.cs`

- [ ] **Step 1: Write failing exposure tests**

```csharp
[Fact]
public void Resolve_Uses_Business_Interfaces_By_Default()
{
    var services = ServiceExposureResolver.Resolve(typeof(DefaultOrderService));

    services.Should().Equal(typeof(IOrderService), typeof(IQueryService));
}

[Fact]
public void Resolve_Falls_Back_To_Implementation_Type_When_No_Business_Interface()
{
    var services = ServiceExposureResolver.Resolve(typeof(PureWorker));

    services.Should().Equal(typeof(PureWorker));
}

[Fact]
public void Resolve_Honors_ExposeServices_And_IncludeSelf()
{
    var services = ServiceExposureResolver.Resolve(typeof(ExplicitOrderService));

    services.Should().Equal(typeof(IOrderService), typeof(ExplicitOrderService));
}

private interface IOrderService;
private interface IQueryService;
private sealed class DefaultOrderService : IOrderService, IQueryService, IScopedDependency;
private sealed class PureWorker : ITransientDependency;

[ExposeServices(typeof(IOrderService), IncludeSelf = true)]
private sealed class ExplicitOrderService : IOrderService, IQueryService, IScopedDependency;
```

- [ ] **Step 2: Run test to verify it fails**

Run:

```powershell
dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Tw.Core.Tests.csproj --filter FullyQualifiedName~ServiceExposureResolverTests
```

Expected: FAIL because `ServiceExposureResolver` does not exist.

- [ ] **Step 3: Create resolver**

```csharp
namespace Tw.Core.DependencyInjection;

using Tw.Core.Reflection;

/// <summary>按自动注册规则计算实现类型应暴露的服务类型</summary>
public static class ServiceExposureResolver
{
    private static readonly HashSet<Type> LifetimeMarkers =
    [
        typeof(ITransientDependency),
        typeof(IScopedDependency),
        typeof(ISingletonDependency)
    ];

    /// <summary>返回实现类型应暴露的服务类型</summary>
    /// <param name="implementationType">参与自动注册的实现类型</param>
    /// <returns>按稳定规则排序后的服务类型集合</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="implementationType"/> 为空时抛出</exception>
    public static IReadOnlyList<Type> Resolve(Type implementationType)
    {
        var checkedType = Check.NotNull(implementationType);
        var expose = checkedType.GetSingleAttributeOrNull<ExposeServicesAttribute>();
        var services = expose is not null
            ? expose.ServiceTypes.ToList()
            : checkedType.GetCachedInterfaces()
                .Where(IsBusinessInterface)
                .Distinct()
                .ToList();

        if (services.Count == 0)
        {
            services.Add(checkedType);
        }

        if (expose?.IncludeSelf == true && !services.Contains(checkedType))
        {
            services.Add(checkedType);
        }

        return services;
    }

    private static bool IsBusinessInterface(Type serviceType)
    {
        if (!serviceType.IsInterface || LifetimeMarkers.Contains(serviceType))
        {
            return false;
        }

        var serviceNamespace = serviceType.Namespace ?? string.Empty;
        return !serviceNamespace.StartsWith("System", StringComparison.Ordinal)
            && !serviceNamespace.StartsWith("Microsoft", StringComparison.Ordinal);
    }
}
```

- [ ] **Step 4: Run tests**

Run:

```powershell
dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Tw.Core.Tests.csproj --filter FullyQualifiedName~ServiceExposureResolverTests
```

Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add backend/dotnet/BuildingBlocks/src/Tw.Core/DependencyInjection/ServiceExposureResolver.cs backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/DependencyInjection/ServiceExposureResolverTests.cs
git commit -m "feat: resolve auto registration exposed services"
```

### Task 3: Plan Final Registrations and Conflicts

**Files:**
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/DependencyInjection/ServiceRegistrationCandidate.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/DependencyInjection/ServiceRegistrationEntry.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/DependencyInjection/ServiceRegistrationPlanner.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/DependencyInjection/AutoRegistrationException.cs`
- Test: `backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/DependencyInjection/ServiceRegistrationPlannerTests.cs`

- [ ] **Step 1: Write failing planner tests**

```csharp
[Fact]
public void Plan_Keeps_Collection_Implementations_In_Order()
{
    var candidates = new[]
    {
        Candidate(typeof(IRule), typeof(FirstRule), collectionOrder: 20),
        Candidate(typeof(IRule), typeof(SecondRule), collectionOrder: 10)
    };

    var entries = ServiceRegistrationPlanner.Plan(candidates);

    entries.Should().HaveCount(2);
    entries.Select(entry => entry.ImplementationType).Should().Equal(typeof(SecondRule), typeof(FirstRule));
}

[Fact]
public void Plan_Fails_When_Collection_And_NonCollection_Are_Mixed()
{
    var candidates = new[]
    {
        Candidate(typeof(IRule), typeof(FirstRule), collectionOrder: 0),
        Candidate(typeof(IRule), typeof(DefaultRule))
    };

    var action = () => ServiceRegistrationPlanner.Plan(candidates);

    action.Should().Throw<AutoRegistrationException>()
        .WithMessage("*集合服务*IRule*DefaultRule*FirstRule*");
}

private static ServiceRegistrationCandidate Candidate(
    Type serviceType,
    Type implementationType,
    object? key = null,
    bool replace = false,
    int replaceOrder = 0,
    int? collectionOrder = null)
{
    return new ServiceRegistrationCandidate(
        serviceType,
        implementationType,
        key,
        LifetimeKind.Scoped,
        replace,
        replaceOrder,
        collectionOrder,
        implementationType.Assembly);
}

private interface IRule;
private sealed class DefaultRule;
private sealed class FirstRule;
private sealed class SecondRule;
```

- [ ] **Step 2: Run tests to verify they fail**

Run:

```powershell
dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Tw.Core.Tests.csproj --filter FullyQualifiedName~ServiceRegistrationPlannerTests
```

Expected: FAIL because planner types do not exist.

- [ ] **Step 3: Create planning records**

```csharp
namespace Tw.Core.DependencyInjection;

using System.Reflection;

/// <summary>自动注册服务生命周期</summary>
public enum LifetimeKind
{
    /// <summary>瞬时生命周期</summary>
    Transient,

    /// <summary>作用域生命周期</summary>
    Scoped,

    /// <summary>单例生命周期</summary>
    Singleton
}

/// <summary>扫描阶段发现的候选服务注册</summary>
public sealed record ServiceRegistrationCandidate(
    Type ServiceType,
    Type ImplementationType,
    object? Key,
    LifetimeKind Lifetime,
    bool ReplacesExisting,
    int ReplaceOrder,
    int? CollectionOrder,
    Assembly Assembly);

/// <summary>裁决后写入 Autofac 的最终注册</summary>
public sealed record ServiceRegistrationEntry(
    Type ServiceType,
    Type ImplementationType,
    object? Key,
    LifetimeKind Lifetime,
    bool IsCollectionService,
    Assembly Assembly);
```

- [ ] **Step 4: Create exception type**

```csharp
namespace Tw.Core.DependencyInjection;

using Tw.Core.Exceptions;

/// <summary>表示自动注册元数据无效或服务裁决失败</summary>
public sealed class AutoRegistrationException(string message) : TwException(message);
```

- [ ] **Step 5: Create planner**

Implement `ServiceRegistrationPlanner.Plan(IReadOnlyCollection<ServiceRegistrationCandidate> candidates)` with these exact rules:

```text
1. Group by (ServiceType, Key).
2. If every candidate in a group has CollectionOrder, return all candidates sorted by CollectionOrder then ImplementationType.FullName.
3. If some but not all candidates have CollectionOrder, throw AutoRegistrationException with every implementation type full name in the message.
4. If a group has one candidate, return it.
5. If candidates contain ReplacesExisting, choose the highest ReplaceOrder; if tied, throw AutoRegistrationException with tied type names.
6. If candidates contain no replacement marker and more than one candidate remains, throw AutoRegistrationException with every implementation type full name.
```

- [ ] **Step 6: Run planner tests**

Run:

```powershell
dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Tw.Core.Tests.csproj --filter FullyQualifiedName~ServiceRegistrationPlannerTests
```

Expected: PASS.

- [ ] **Step 7: Commit**

```bash
git add backend/dotnet/BuildingBlocks/src/Tw.Core/DependencyInjection backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/DependencyInjection/ServiceRegistrationPlannerTests.cs
git commit -m "feat: plan auto registration conflicts"
```

### Task 4: Scan Assemblies Into Candidates

**Files:**
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/DependencyInjection/AutoRegistrationOptions.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/DependencyInjection/AutoRegistrationAssemblySelector.cs`
- Test: `backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/DependencyInjection/ServiceRegistrationPlannerTests.cs`

- [ ] **Step 1: Write failing scan tests**

```csharp
[Fact]
public void Scan_Finds_Lifecycle_Types_And_Skips_Disabled_Types()
{
    var options = new AutoRegistrationOptions();
    options.AddAssemblyOf<EnabledService>();

    var candidates = AutoRegistrationAssemblySelector.Scan(options);

    candidates.Select(candidate => candidate.ImplementationType)
        .Should()
        .Contain(typeof(EnabledService))
        .And.NotContain(typeof(DisabledService));
}

private interface IEnabledService;
private sealed class EnabledService : IEnabledService, IScopedDependency;

[DisableAutoRegistration]
private sealed class DisabledService : IEnabledService, IScopedDependency;
```

- [ ] **Step 2: Run test to verify it fails**

Run:

```powershell
dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Tw.Core.Tests.csproj --filter Scan_Finds_Lifecycle_Types_And_Skips_Disabled_Types
```

Expected: FAIL because scanner types do not exist.

- [ ] **Step 3: Create options**

```csharp
namespace Tw.Core.DependencyInjection;

using System.Reflection;

/// <summary>配置 DI 自动注册的程序集范围和行为</summary>
public sealed class AutoRegistrationOptions
{
    private readonly List<Assembly> _assemblies = [];

    /// <summary>参与扫描的程序集</summary>
    public IReadOnlyList<Assembly> Assemblies => _assemblies;

    /// <summary>加入包含指定类型的程序集</summary>
    /// <typeparam name="T">用于定位程序集的类型</typeparam>
    /// <returns>当前配置对象</returns>
    public AutoRegistrationOptions AddAssemblyOf<T>()
    {
        return AddAssembly(typeof(T).Assembly);
    }

    /// <summary>加入参与扫描的程序集</summary>
    /// <param name="assembly">参与扫描的程序集</param>
    /// <returns>当前配置对象</returns>
    public AutoRegistrationOptions AddAssembly(Assembly assembly)
    {
        var checkedAssembly = Check.NotNull(assembly);
        if (!_assemblies.Contains(checkedAssembly))
        {
            _assemblies.Add(checkedAssembly);
        }

        return this;
    }
}
```

- [ ] **Step 4: Create scanner**

Implement `AutoRegistrationAssemblySelector.Scan(AutoRegistrationOptions options)` so it:

```text
1. Reuses Tw.Core.Reflection.TypeFinder over options.Assemblies.
2. Keeps non-abstract classes implementing one lifecycle marker.
3. Ignores types marked DisableAutoRegistrationAttribute.
4. Uses ServiceExposureResolver.Resolve(type) to create one candidate per exposed service.
5. Reads KeyedServiceAttribute.Key, ReplaceServiceAttribute.Order, and CollectionServiceAttribute.Order.
6. Maps lifecycle markers to LifetimeKind, failing if a type implements more than one lifecycle marker.
```

- [ ] **Step 5: Run scan tests**

Run:

```powershell
dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Tw.Core.Tests.csproj --filter FullyQualifiedName~ServiceRegistrationPlannerTests
```

Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add backend/dotnet/BuildingBlocks/src/Tw.Core/DependencyInjection/AutoRegistrationOptions.cs backend/dotnet/BuildingBlocks/src/Tw.Core/DependencyInjection/AutoRegistrationAssemblySelector.cs backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/DependencyInjection/ServiceRegistrationPlannerTests.cs
git commit -m "feat: scan auto registration candidates"
```

### Task 5: Register Final Entries With Autofac

**Files:**
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/DependencyInjection/Autofac/AutoRegistrationModule.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/DependencyInjection/Autofac/AutofacAutoRegistrationExtensions.cs`
- Test: `backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/DependencyInjection/AutofacAutoRegistrationTests.cs`

- [ ] **Step 1: Write failing Autofac integration test**

```csharp
[Fact]
public void UseAutoRegistration_Registers_Scoped_Service()
{
    var services = new ServiceCollection();
    services.AddAutoRegistration(options => options.AddAssemblyOf<OrderService>());

    var builder = new ContainerBuilder();
    builder.Populate(services);
    builder.UseAutoRegistration(services);

    using var container = builder.Build();
    using var scope = container.BeginLifetimeScope();

    scope.Resolve<IOrderService>().Should().BeOfType<OrderService>();
}

private interface IOrderService;
private sealed class OrderService : IOrderService, IScopedDependency;
```

- [ ] **Step 2: Run test to verify it fails**

Run:

```powershell
dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Tw.Core.Tests.csproj --filter UseAutoRegistration_Registers_Scoped_Service
```

Expected: FAIL because Autofac extensions do not exist.

- [ ] **Step 3: Create service collection and container extensions**

```csharp
namespace Tw.Core.DependencyInjection.Autofac;

using global::Autofac;
using Microsoft.Extensions.DependencyInjection;

/// <summary>提供 DI 自动注册的 IServiceCollection 配置入口</summary>
public static class AutofacAutoRegistrationExtensions
{
    private static readonly object OptionsKey = new();

    /// <summary>保存自动注册配置，供 Autofac ContainerBuilder 阶段读取</summary>
    public static IServiceCollection AddAutoRegistration(
        this IServiceCollection services,
        Action<AutoRegistrationOptions>? configure = null)
    {
        var options = new AutoRegistrationOptions();
        configure?.Invoke(options);
        services.AddSingleton(options);
        return services;
    }

    /// <summary>在 Autofac 容器构建阶段挂载自动注册模块</summary>
    public static ContainerBuilder UseAutoRegistration(this ContainerBuilder builder, IServiceCollection services)
    {
        var descriptor = services.SingleOrDefault(service => service.ServiceType == typeof(AutoRegistrationOptions));
        if (descriptor?.ImplementationInstance is not AutoRegistrationOptions options)
        {
            throw new AutoRegistrationException("调用 UseAutoRegistration 前必须先调用 AddAutoRegistration。");
        }

        builder.RegisterModule(new AutoRegistrationModule(options));
        return builder;
    }
}
```

- [ ] **Step 4: Create Autofac module**

Implement `AutoRegistrationModule.Load(ContainerBuilder builder)` so it:

```text
1. Calls AutoRegistrationAssemblySelector.Scan(options).
2. Calls ServiceRegistrationPlanner.Plan(candidates).
3. Registers closed concrete types with RegisterType(ImplementationType).
4. Registers open generic definitions with RegisterGeneric(ImplementationType).
5. Applies As(ServiceType) or Keyed(Key, ServiceType).
6. Applies InstancePerDependency, InstancePerLifetimeScope, or SingleInstance according to LifetimeKind.
7. Does not register candidates removed by the planner.
```

- [ ] **Step 5: Run Autofac test**

Run:

```powershell
dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Tw.Core.Tests.csproj --filter FullyQualifiedName~AutofacAutoRegistrationTests
```

Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add backend/dotnet/BuildingBlocks/src/Tw.Core/DependencyInjection/Autofac backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/DependencyInjection/AutofacAutoRegistrationTests.cs
git commit -m "feat: register auto registration entries with autofac"
```

### Task 6: Verify P0 Core Acceptance

**Files:**
- Verify: `backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Tw.Core.Tests.csproj`

- [ ] **Step 1: Run focused DI tests**

Run:

```powershell
dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Tw.Core.Tests.csproj --filter FullyQualifiedName~DependencyInjection
```

Expected: PASS.

- [ ] **Step 2: Run full core test suite**

Run:

```powershell
dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Tw.Core.Tests.csproj
```

Expected: PASS.

- [ ] **Step 3: Commit verification notes if any test exception is documented**

If every test passes, no documentation change is needed. If an automated coverage exception is accepted during review, add it to the PR description with reason, impact, replacement verification, and owner.

## Self-Review Checklist

- [ ] All lifecycle marker interfaces exist under `Tw.Core.DependencyInjection`.
- [ ] Default exposure excludes `System.*`, `Microsoft.*`, and lifecycle marker interfaces.
- [ ] `[ExposeServices]`, `IncludeSelf`, keyed services, replacement, disabled registration, collection service, and open generic decisions are covered by tests.
- [ ] Conflict messages use stable `AutoRegistrationException` with simplified Chinese messages.
- [ ] `TypeFinder` and `ReflectionCache` are reused instead of introducing a second reflection engine.
- [ ] Autofac module writes only planner-approved final entries.
