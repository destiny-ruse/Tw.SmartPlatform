# Service AOP Castle Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build P1 service-level AOP for DI service calls using Castle DynamicProxy and Autofac registration integration.

**Architecture:** `Tw.Core.Aop` exposes the framework-neutral invocation context and interceptor API. `Tw.Core.Aop.Castle` adapts Castle DynamicProxy and `Castle.Core.AsyncInterceptor` into the unified context, while auto-registration only enables proxies for services whose service chain is non-empty.

**Tech Stack:** .NET 10, Tw.Core, Autofac.Extras.DynamicProxy, Castle.Core.AsyncInterceptor, xUnit, FluentAssertions

---

## Execution Order

Run after `2026-04-29-02-options-auto-registration.md`. This plan must pass before `2026-04-29-04-aspnetcore-mvc-adapter.md`.

## Source Inputs

- Design sections: § 3.1, § 3.6, § 5.6, § 6.1-6.4, § 6.6-6.8, § 10.2 items 1-10
- Standards: `rules.naming-dotnet#rules` version `1.1.0`, `docs/standards/rules/naming-dotnet.md`; `rules.comments-dotnet#rules` version `1.3.0`, `docs/standards/rules/comments-dotnet.md`; `rules.error-handling#rules` version `1.2.0`, `docs/standards/rules/error-handling.md`; `rules.test-strategy#rules` version `1.1.0`, `docs/standards/rules/test-strategy.md`

## File Structure

- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/Cancellation/ICancellationTokenProvider.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/Cancellation/ICurrentCancellationTokenAccessor.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/Cancellation/AmbientCancellationTokenAccessor.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/Cancellation/NullCancellationTokenProvider.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/Aop/IInvocationContext.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/Aop/IUnaryInvocationContext.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/Aop/IAsyncStreamInvocationContext.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/Aop/InterceptorBase.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/Aop/InterceptorScope.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/Aop/IAutoMatchInterceptor.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/Aop/InterceptAttribute.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/Aop/IgnoreInterceptorsAttribute.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/Aop/InvocationFeatureCollection.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/Aop/ServiceChainCache.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/Aop/Castle/CastleAsyncInterceptorAdapter.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/Aop/Castle/CastleUnaryInvocationContext.cs`
- Modify: `backend/dotnet/BuildingBlocks/src/Tw.Core/DependencyInjection/AutoRegistrationOptions.cs`
- Modify: `backend/dotnet/BuildingBlocks/src/Tw.Core/DependencyInjection/Autofac/AutoRegistrationModule.cs`
- Create: `backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Aop/CancellationTokenProviderTests.cs`
- Create: `backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Aop/ServiceChainCacheTests.cs`
- Create: `backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Aop/CastleAsyncInterceptorTests.cs`

## Tasks

### Task 1: Add Cancellation Token Infrastructure

**Files:**
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/Cancellation/ICancellationTokenProvider.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/Cancellation/ICurrentCancellationTokenAccessor.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/Cancellation/AmbientCancellationTokenAccessor.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/Cancellation/NullCancellationTokenProvider.cs`
- Test: `backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Aop/CancellationTokenProviderTests.cs`

- [ ] **Step 1: Write failing cancellation tests**

```csharp
[Fact]
public void AmbientCancellationTokenAccessor_Restores_Previous_Token()
{
    var accessor = new AmbientCancellationTokenAccessor();
    using var firstSource = new CancellationTokenSource();
    using var secondSource = new CancellationTokenSource();

    using (accessor.Use(firstSource.Token))
    {
        accessor.Token.Should().Be(firstSource.Token);
        using (accessor.Use(secondSource.Token))
        {
            accessor.Token.Should().Be(secondSource.Token);
        }

        accessor.Token.Should().Be(firstSource.Token);
    }

    accessor.Token.Should().Be(CancellationToken.None);
}
```

- [ ] **Step 2: Run test to verify it fails**

Run:

```powershell
dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Tw.Core.Tests.csproj --filter AmbientCancellationTokenAccessor_Restores_Previous_Token
```

Expected: FAIL because cancellation types do not exist.

- [ ] **Step 3: Create cancellation interfaces and implementations**

```csharp
namespace Tw.Core.Cancellation;

/// <summary>提供当前调用链的取消令牌</summary>
public interface ICancellationTokenProvider
{
    /// <summary>当前调用链取消令牌</summary>
    CancellationToken Token { get; }
}

/// <summary>允许入口层或拦截器临时切换当前取消令牌</summary>
public interface ICurrentCancellationTokenAccessor : ICancellationTokenProvider
{
    /// <summary>设置当前取消令牌，并在释放返回对象时恢复原值</summary>
    IDisposable Use(CancellationToken token);
}

/// <summary>使用 AsyncLocal 保存当前调用链取消令牌</summary>
public sealed class AmbientCancellationTokenAccessor : ICurrentCancellationTokenAccessor
{
    private readonly AsyncLocal<CancellationToken?> _current = new();

    /// <inheritdoc />
    public CancellationToken Token => _current.Value ?? CancellationToken.None;

    /// <inheritdoc />
    public IDisposable Use(CancellationToken token)
    {
        var previous = _current.Value;
        _current.Value = token;
        return new RestoreAction(this, previous);
    }

    private sealed class RestoreAction(AmbientCancellationTokenAccessor accessor, CancellationToken? previous) : IDisposable
    {
        public void Dispose()
        {
            accessor._current.Value = previous;
        }
    }
}

/// <summary>在未接入具体入口层时提供空取消令牌</summary>
public sealed class NullCancellationTokenProvider : ICancellationTokenProvider
{
    /// <inheritdoc />
    public CancellationToken Token => CancellationToken.None;
}
```

- [ ] **Step 4: Run cancellation tests**

Run:

```powershell
dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Tw.Core.Tests.csproj --filter FullyQualifiedName~CancellationTokenProviderTests
```

Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add backend/dotnet/BuildingBlocks/src/Tw.Core/Cancellation backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Aop/CancellationTokenProviderTests.cs
git commit -m "feat: add ambient cancellation token provider"
```

### Task 2: Add Public AOP Abstractions

**Files:**
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/Aop/IInvocationContext.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/Aop/IUnaryInvocationContext.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/Aop/IAsyncStreamInvocationContext.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/Aop/InterceptorBase.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/Aop/InterceptorScope.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/Aop/IAutoMatchInterceptor.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/Aop/InterceptAttribute.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/Aop/IgnoreInterceptorsAttribute.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/Aop/InvocationFeatureCollection.cs`
- Test: `backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Aop/ServiceChainCacheTests.cs`

- [ ] **Step 1: Write failing abstraction tests**

```csharp
[Fact]
public void InterceptAttribute_Only_Allows_Service_Interceptor_Types()
{
    var attribute = new InterceptAttribute(typeof(LoggingInterceptor));

    attribute.InterceptorTypes.Should().Equal(typeof(LoggingInterceptor));
}

[Fact]
public void InvocationFeatureCollection_Returns_Null_When_Feature_Is_Missing()
{
    var features = new InvocationFeatureCollection();

    features.GetFeature<IDisposable>().Should().BeNull();
}

private sealed class LoggingInterceptor : InterceptorBase;
```

- [ ] **Step 2: Run test to verify it fails**

Run:

```powershell
dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Tw.Core.Tests.csproj --filter "InterceptAttribute_Only_Allows_Service_Interceptor_Types|InvocationFeatureCollection_Returns_Null_When_Feature_Is_Missing"
```

Expected: FAIL because AOP abstractions do not exist.

- [ ] **Step 3: Create context interfaces**

```csharp
namespace Tw.Core.Aop;

using System.Reflection;

/// <summary>表示一次拦截调用的公共上下文</summary>
public interface IInvocationContext
{
    Type ServiceType { get; }
    Type ImplementationType { get; }
    MethodInfo Method { get; }
    object?[] Arguments { get; }
    CancellationToken CancellationToken { get; }
    IDictionary<string, object?> Items { get; }
    T? GetFeature<T>() where T : class;
}

/// <summary>表示非流式方法调用的拦截上下文</summary>
public interface IUnaryInvocationContext : IInvocationContext
{
    Type? ResultType { get; }
    object? ReturnValue { get; set; }
    ValueTask<object?> ProceedAsync();
}

/// <summary>表示异步流调用的拦截上下文</summary>
public interface IAsyncStreamInvocationContext<T> : IInvocationContext
{
    IAsyncEnumerable<T> ProceedAsyncEnumerable();
}
```

- [ ] **Step 4: Create interceptor API**

```csharp
namespace Tw.Core.Aop;

/// <summary>拦截器作用域</summary>
public enum InterceptorScope
{
    /// <summary>Castle 代理的 DI 服务调用</summary>
    Service,

    /// <summary>入口 Adapter 调用边界</summary>
    Entry
}

/// <summary>拦截器基类</summary>
public abstract class InterceptorBase
{
    /// <summary>拦截非流式调用</summary>
    public virtual async ValueTask InterceptAsync(IUnaryInvocationContext context)
    {
        await context.ProceedAsync();
    }

    /// <summary>拦截异步流调用</summary>
    public virtual IAsyncEnumerable<T> InterceptAsyncEnumerable<T>(IAsyncStreamInvocationContext<T> context)
    {
        return context.ProceedAsyncEnumerable();
    }
}

/// <summary>声明拦截器可按类型自动匹配目标</summary>
public interface IAutoMatchInterceptor
{
    InterceptorScope Scope { get; }
    int Order => 0;
    bool Match(Type serviceType, Type implementationType);
    bool MatchEntry(Type controllerType) => false;
}
```

- [ ] **Step 5: Create attributes and feature collection**

```csharp
namespace Tw.Core.Aop;

/// <summary>声明服务应显式启用指定拦截器</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = true, Inherited = true)]
public sealed class InterceptAttribute(params Type[] interceptorTypes) : Attribute
{
    /// <summary>显式指定的拦截器类型</summary>
    public IReadOnlyList<Type> InterceptorTypes { get; } = interceptorTypes;
}

/// <summary>声明应忽略全局和自动匹配拦截器</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = true, Inherited = true)]
public sealed class IgnoreInterceptorsAttribute(params Type[] interceptorTypes) : Attribute
{
    /// <summary>要忽略的拦截器类型；空集合表示忽略全部</summary>
    public IReadOnlyList<Type> InterceptorTypes { get; } = interceptorTypes;
}

/// <summary>保存入口 Adapter 注入的场景特定对象</summary>
public sealed class InvocationFeatureCollection
{
    private readonly Dictionary<Type, object> _features = [];

    /// <summary>加入一个场景特定对象</summary>
    public void Set<T>(T feature) where T : class
    {
        _features[typeof(T)] = feature;
    }

    /// <summary>获取一个场景特定对象</summary>
    public T? GetFeature<T>() where T : class
    {
        return _features.TryGetValue(typeof(T), out var value) ? (T)value : null;
    }
}
```

- [ ] **Step 6: Run abstraction tests**

Run:

```powershell
dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Tw.Core.Tests.csproj --filter FullyQualifiedName~ServiceChainCacheTests
```

Expected: PASS for the new abstraction assertions.

- [ ] **Step 7: Commit**

```bash
git add backend/dotnet/BuildingBlocks/src/Tw.Core/Aop backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Aop/ServiceChainCacheTests.cs
git commit -m "feat: add aop invocation abstractions"
```

### Task 3: Build Service Chain Cache

**Files:**
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/Aop/ServiceChainCache.cs`
- Modify: `backend/dotnet/BuildingBlocks/src/Tw.Core/DependencyInjection/AutoRegistrationOptions.cs`
- Test: `backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Aop/ServiceChainCacheTests.cs`

- [ ] **Step 1: Add failing chain tests**

```csharp
[Fact]
public void ServiceChainCache_Orders_Global_AutoMatch_And_Explicit_Interceptors()
{
    var options = new AutoRegistrationOptions();
    options.AddGlobalInterceptor<GlobalInterceptor>(InterceptorScope.Service, order: 10);
    var registration = new ServiceRegistrationEntry(
        typeof(IOrderService),
        typeof(OrderService),
        Key: null,
        LifetimeKind.Scoped,
        IsCollectionService: false,
        typeof(OrderService).Assembly);

    var cache = ServiceChainCache.Build([registration], [new AutoMatchInterceptor()], options);

    cache.GetInterceptors(typeof(OrderService))
        .Select(type => type.Name)
        .Should()
        .Equal(nameof(GlobalInterceptor), nameof(AutoMatchInterceptor), nameof(ExplicitInterceptor));
}

private interface IOrderService;

[Intercept(typeof(ExplicitInterceptor))]
private sealed class OrderService : IOrderService, IScopedDependency;

private sealed class GlobalInterceptor : InterceptorBase;
private sealed class ExplicitInterceptor : InterceptorBase;
private sealed class AutoMatchInterceptor : InterceptorBase, IAutoMatchInterceptor
{
    public InterceptorScope Scope => InterceptorScope.Service;
    public bool Match(Type serviceType, Type implementationType) => true;
}
```

- [ ] **Step 2: Run test to verify it fails**

Run:

```powershell
dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Tw.Core.Tests.csproj --filter ServiceChainCache_Orders_Global_AutoMatch_And_Explicit_Interceptors
```

Expected: FAIL because chain cache does not exist.

- [ ] **Step 3: Extend options with global interceptors**

Add a `GlobalInterceptorDescriptor` record and methods:

```csharp
public sealed record GlobalInterceptorDescriptor(Type InterceptorType, InterceptorScope Scope, int Order);

private readonly List<GlobalInterceptorDescriptor> _globalInterceptors = [];

public IReadOnlyList<GlobalInterceptorDescriptor> GlobalInterceptors => _globalInterceptors;

public AutoRegistrationOptions AddGlobalInterceptor<TInterceptor>(
    InterceptorScope scope = InterceptorScope.Service,
    int order = 0)
    where TInterceptor : InterceptorBase
{
    _globalInterceptors.Add(new GlobalInterceptorDescriptor(typeof(TInterceptor), scope, order));
    return this;
}
```

- [ ] **Step 4: Create service chain cache**

Implement `ServiceChainCache.Build(...)` so it:

```text
1. Indexes by implementationType.
2. Adds global interceptors where Scope == Service sorted by Order then InterceptorType.FullName.
3. Adds IAutoMatchInterceptor instances where Scope == Service and Match(serviceType, implementationType) is true, sorted by Order then type name.
4. Removes global and auto-match interceptors suppressed by IgnoreInterceptorsAttribute on class or exposed service interface.
5. Adds explicit InterceptAttribute interceptor types after suppression, because explicit interceptors are not suppressed.
6. Deduplicates interceptor types while preserving first appearance.
```

- [ ] **Step 5: Run chain tests**

Run:

```powershell
dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Tw.Core.Tests.csproj --filter FullyQualifiedName~ServiceChainCacheTests
```

Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add backend/dotnet/BuildingBlocks/src/Tw.Core/Aop/ServiceChainCache.cs backend/dotnet/BuildingBlocks/src/Tw.Core/DependencyInjection/AutoRegistrationOptions.cs backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Aop/ServiceChainCacheTests.cs
git commit -m "feat: compute service interceptor chains"
```

### Task 4: Adapt Castle Async Interception

**Files:**
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/Aop/Castle/CastleAsyncInterceptorAdapter.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/Aop/Castle/CastleUnaryInvocationContext.cs`
- Test: `backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Aop/CastleAsyncInterceptorTests.cs`

- [ ] **Step 1: Write failing proceed semantics tests**

```csharp
[Fact]
public async Task Interceptor_Can_Short_Circuit_Task_Result()
{
    var service = BuildProxy<IPriceService, PriceService>(new ShortCircuitInterceptor(42));

    var price = await service.GetPriceAsync();

    price.Should().Be(42);
}

[Fact]
public async Task ProceedAsync_Throws_When_Called_Twice()
{
    var service = BuildProxy<IPriceService, PriceService>(new DoubleProceedInterceptor());

    var action = async () => await service.GetPriceAsync();

    await action.Should().ThrowAsync<AutoRegistrationException>()
        .WithMessage("*ProceedAsync*只能调用一次*");
}

private interface IPriceService
{
    Task<int> GetPriceAsync();
}

private sealed class PriceService : IPriceService
{
    public Task<int> GetPriceAsync() => Task.FromResult(10);
}
```

- [ ] **Step 2: Run test to verify it fails**

Run:

```powershell
dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Tw.Core.Tests.csproj --filter FullyQualifiedName~CastleAsyncInterceptorTests
```

Expected: FAIL because Castle adapter does not exist.

- [ ] **Step 3: Create unary invocation context**

Implement `CastleUnaryInvocationContext` so it:

```text
1. Wraps Castle IInvocation, service type, implementation type, feature collection, ICancellationTokenProvider, and ICurrentCancellationTokenAccessor.
2. Exposes ResultType using ReflectionCache.GetAsyncResultType for Task and ValueTask return shapes.
3. Finds the first CancellationToken argument and temporarily applies it through ICurrentCancellationTokenAccessor.Use before proceeding.
4. Throws AutoRegistrationException with message "ProceedAsync 只能调用一次。" on duplicate ProceedAsync.
5. Allows short circuit when an interceptor sets ReturnValue and does not call ProceedAsync.
6. Validates ReturnValue against ResultType before Castle return conversion.
```

- [ ] **Step 4: Create Castle adapter**

Implement `CastleAsyncInterceptorAdapter` over `AsyncInterceptorBase` so it:

```text
1. Resolves interceptor instances per invocation from ILifetimeScope.
2. Executes InterceptorBase instances in chain order.
3. Supports sync, Task, Task<T>, ValueTask, and ValueTask<T> return shapes.
4. Propagates target exceptions without wrapping them in framework exceptions.
5. Converts ReturnValue back to the method's declared return shape.
```

- [ ] **Step 5: Run Castle tests**

Run:

```powershell
dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Tw.Core.Tests.csproj --filter FullyQualifiedName~CastleAsyncInterceptorTests
```

Expected: PASS for sync, `Task`, `Task<T>`, `ValueTask`, `ValueTask<T>`, short-circuit, duplicate proceed, and exception propagation cases.

- [ ] **Step 6: Commit**

```bash
git add backend/dotnet/BuildingBlocks/src/Tw.Core/Aop/Castle backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Aop/CastleAsyncInterceptorTests.cs
git commit -m "feat: adapt castle async interception"
```

### Task 5: Enable Proxies During Autofac Registration

**Files:**
- Modify: `backend/dotnet/BuildingBlocks/src/Tw.Core/DependencyInjection/Autofac/AutoRegistrationModule.cs`
- Test: `backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Aop/CastleAsyncInterceptorTests.cs`

- [ ] **Step 1: Add failing integration test**

```csharp
[Fact]
public async Task AutoRegistration_Enables_Interface_Proxy_When_Service_Chain_Is_Not_Empty()
{
    var services = new ServiceCollection();
    services.AddAutoRegistration(options =>
    {
        options.AddAssemblyOf<DiscountService>();
        options.AddGlobalInterceptor<DiscountInterceptor>(InterceptorScope.Service);
    });

    var builder = new ContainerBuilder();
    builder.Populate(services);
    builder.UseAutoRegistration(services);

    using var container = builder.Build();
    var service = container.Resolve<IDiscountService>();

    (await service.GetDiscountAsync()).Should().Be(30);
}

private interface IDiscountService
{
    Task<int> GetDiscountAsync();
}

private sealed class DiscountService : IDiscountService, IScopedDependency
{
    public Task<int> GetDiscountAsync() => Task.FromResult(10);
}

private sealed class DiscountInterceptor : InterceptorBase
{
    public override async ValueTask InterceptAsync(IUnaryInvocationContext context)
    {
        await context.ProceedAsync();
        context.ReturnValue = 30;
    }
}
```

- [ ] **Step 2: Run integration test to verify it fails**

Run:

```powershell
dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Tw.Core.Tests.csproj --filter AutoRegistration_Enables_Interface_Proxy_When_Service_Chain_Is_Not_Empty
```

Expected: FAIL because Autofac registration does not enable Castle interceptors.

- [ ] **Step 3: Update Autofac module**

Update `AutoRegistrationModule` so it:

```text
1. Builds ServiceChainCache after final registration entries are planned.
2. Registers interceptor types in Autofac using their implemented lifecycle marker, defaulting to transient.
3. Skips Castle proxy registration when chain is empty.
4. Uses EnableInterfaceInterceptors for services exposed through public interfaces.
5. Emits a startup diagnostic warning for concrete-only services instead of enabling class proxy by default.
6. Leaves class proxy support disabled in this plan; the diagnostic is verified by plan 05.
```

- [ ] **Step 4: Run AOP tests**

Run:

```powershell
dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Tw.Core.Tests.csproj --filter "FullyQualifiedName~CastleAsyncInterceptorTests|FullyQualifiedName~ServiceChainCacheTests"
```

Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add backend/dotnet/BuildingBlocks/src/Tw.Core/DependencyInjection/Autofac/AutoRegistrationModule.cs backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Aop/CastleAsyncInterceptorTests.cs
git commit -m "feat: enable service aop through autofac"
```

### Task 6: Verify P1 Service AOP Acceptance

**Files:**
- Verify: `backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Tw.Core.Tests.csproj`

- [ ] **Step 1: Run focused AOP tests**

Run:

```powershell
dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Tw.Core.Tests.csproj --filter FullyQualifiedName~Aop
```

Expected: PASS.

- [ ] **Step 2: Run full core tests**

Run:

```powershell
dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Tw.Core.Tests.csproj
```

Expected: PASS.

## Self-Review Checklist

- [ ] `InterceptorScope.Service` and `InterceptorScope.Entry` exist, with no `All` scope.
- [ ] Service chains contain global Service interceptors, `Match()` interceptors, and explicit `[Intercept]` in the required order.
- [ ] Entry scope interceptors never appear in Service chains.
- [ ] `[IgnoreInterceptors]` suppresses global and auto-match interceptors but not explicit `[Intercept]`.
- [ ] `ProceedAsync()` enforces single-call semantics.
- [ ] sync, `Task`, `Task<T>`, `ValueTask`, `ValueTask<T>`, exception propagation, and short-circuit paths are tested.
- [ ] `ICancellationTokenProvider.Token` follows method argument token before ambient token before `CancellationToken.None`.
