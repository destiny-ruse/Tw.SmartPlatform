# ASP.NET Core MVC Adapter Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build P1 `Tw.AspNetCore` infrastructure entrypoint and MVC/Web API Entry-scope adapter.

**Architecture:** `Tw.AspNetCore` owns ASP.NET Core integration and reuses `Tw.Core` AOP abstractions. MVC/Web API entry interception is implemented as an MVC filter registered through `IConfigureOptions<MvcOptions>`, while service-to-service interception remains the Castle mode from the previous plan.

**Tech Stack:** .NET 10, ASP.NET Core MVC, Autofac.Extensions.DependencyInjection, Tw.Core, xUnit, FluentAssertions

---

## Execution Order

Run after `2026-04-29-03-service-aop-castle.md`. This plan must pass before `2026-04-29-05-cross-cutting-quality-gates.md`.

## Source Inputs

- Design sections: § 2.1, § 3.1, § 5.6, § 6.4, § 6.5, § 10.2 items 4-6 and 9-12
- Knowledge discovery: reuse capability `backend.capability.asp-net-core`; provider module `backend.dotnet.building-blocks.asp-net-core`; `reuse.use_when`: Web host or unified service startup conventions are needed; `reuse.do_not_reimplement`: do not duplicate Web infrastructure in services
- Standards: `rules.repo-layout#rules` version `1.1.0`, `docs/standards/rules/repo-layout.md`; `rules.comments-dotnet#rules` version `1.3.0`, `docs/standards/rules/comments-dotnet.md`; `rules.test-strategy#rules` version `1.1.0`, `docs/standards/rules/test-strategy.md`

## File Structure

- Modify: `backend/dotnet/BuildingBlocks/src/Tw.AspNetCore/Tw.AspNetCore.csproj`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.AspNetCore/DependencyInjection/TwAspNetCoreInfrastructureExtensions.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.AspNetCore/Cancellation/HttpContextCancellationTokenProvider.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.AspNetCore/Aop/IHttpRequestFeature.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.AspNetCore/Aop/HttpRequestFeature.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.AspNetCore/Aop/Mvc/EntryChainCache.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.AspNetCore/Aop/Mvc/TwMvcAopFilter.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.AspNetCore/Aop/Mvc/TwMvcAopOptionsSetup.cs`
- Create: `backend/dotnet/BuildingBlocks/tests/Tw.AspNetCore.Tests/Tw.AspNetCore.Tests.csproj`
- Create: `backend/dotnet/BuildingBlocks/tests/Tw.AspNetCore.Tests/Aop/MvcEntryChainCacheTests.cs`
- Create: `backend/dotnet/BuildingBlocks/tests/Tw.AspNetCore.Tests/Aop/TwMvcAopFilterTests.cs`
- Create: `backend/dotnet/BuildingBlocks/tests/Tw.AspNetCore.Tests/DependencyInjection/TwAspNetCoreInfrastructureExtensionsTests.cs`

## Tasks

### Task 1: Add Tw.AspNetCore Test Project

**Files:**
- Create: `backend/dotnet/BuildingBlocks/tests/Tw.AspNetCore.Tests/Tw.AspNetCore.Tests.csproj`

- [ ] **Step 1: Create test project file**

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="xunit" />
    <PackageReference Include="xunit.runner.visualstudio">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
    <PackageReference Include="FluentAssertions" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\Tw.AspNetCore\Tw.AspNetCore.csproj" />
    <ProjectReference Include="..\Tw.TestBase\Tw.TestBase.csproj" />
  </ItemGroup>

</Project>
```

- [ ] **Step 2: Run empty test project restore**

Run:

```powershell
dotnet restore backend/dotnet/BuildingBlocks/tests/Tw.AspNetCore.Tests/Tw.AspNetCore.Tests.csproj --use-lock-file
```

Expected: restore succeeds and creates `backend/dotnet/BuildingBlocks/tests/Tw.AspNetCore.Tests/packages.lock.json`.

- [ ] **Step 3: Commit**

```bash
git add backend/dotnet/BuildingBlocks/tests/Tw.AspNetCore.Tests/Tw.AspNetCore.Tests.csproj backend/dotnet/BuildingBlocks/tests/Tw.AspNetCore.Tests/packages.lock.json
git commit -m "test: add aspnetcore building block tests"
```

### Task 2: Add ASP.NET Core Infrastructure Entrypoint

**Files:**
- Create: `backend/dotnet/BuildingBlocks/src/Tw.AspNetCore/DependencyInjection/TwAspNetCoreInfrastructureExtensions.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.AspNetCore/Cancellation/HttpContextCancellationTokenProvider.cs`
- Test: `backend/dotnet/BuildingBlocks/tests/Tw.AspNetCore.Tests/DependencyInjection/TwAspNetCoreInfrastructureExtensionsTests.cs`

- [ ] **Step 1: Write failing entrypoint test**

```csharp
[Fact]
public void AddTwAspNetCoreInfrastructure_Registers_Autofac_And_Http_Cancellation_Provider()
{
    var builder = WebApplication.CreateBuilder();

    builder.AddTwAspNetCoreInfrastructure(options => options.AddAssemblyOf<TestController>());

    builder.Services.Should().Contain(descriptor =>
        descriptor.ServiceType == typeof(ICancellationTokenProvider) &&
        descriptor.ImplementationType == typeof(HttpContextCancellationTokenProvider));
}

private sealed class TestController : ControllerBase;
```

- [ ] **Step 2: Run test to verify it fails**

Run:

```powershell
dotnet test backend/dotnet/BuildingBlocks/tests/Tw.AspNetCore.Tests/Tw.AspNetCore.Tests.csproj --filter AddTwAspNetCoreInfrastructure_Registers_Autofac_And_Http_Cancellation_Provider
```

Expected: FAIL because the entrypoint does not exist.

- [ ] **Step 3: Create HTTP cancellation provider**

```csharp
namespace Tw.AspNetCore.Cancellation;

using Microsoft.AspNetCore.Http;
using Tw.Core.Cancellation;

/// <summary>从当前 HTTP 请求读取取消令牌</summary>
public sealed class HttpContextCancellationTokenProvider(IHttpContextAccessor httpContextAccessor) : ICancellationTokenProvider
{
    /// <inheritdoc />
    public CancellationToken Token => httpContextAccessor.HttpContext?.RequestAborted ?? CancellationToken.None;
}
```

- [ ] **Step 4: Create infrastructure extension**

```csharp
namespace Tw.AspNetCore.DependencyInjection;

using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Tw.AspNetCore.Aop.Mvc;
using Tw.AspNetCore.Cancellation;
using Tw.Core.Cancellation;
using Tw.Core.DependencyInjection;
using Tw.Core.DependencyInjection.Autofac;

/// <summary>提供 Tw 平台 ASP.NET Core 基础设施注册入口</summary>
public static class TwAspNetCoreInfrastructureExtensions
{
    /// <summary>注册 ASP.NET Core 场景所需的 Tw 基础设施</summary>
    public static WebApplicationBuilder AddTwAspNetCoreInfrastructure(
        this WebApplicationBuilder builder,
        Action<AutoRegistrationOptions>? configure = null)
    {
        builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());
        builder.Host.ConfigureContainer<ContainerBuilder>(containerBuilder =>
        {
            containerBuilder.UseAutoRegistration(builder.Services);
        });

        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<ICancellationTokenProvider, HttpContextCancellationTokenProvider>();
        builder.Services.AddSingleton<IConfigureOptions<MvcOptions>, TwMvcAopOptionsSetup>();
        builder.Services.AddAutoRegistration(options =>
        {
            options.AddAssembly(builder.Environment.ApplicationName is { Length: > 0 }
                ? Assembly.Load(builder.Environment.ApplicationName)
                : typeof(TwAspNetCoreInfrastructureExtensions).Assembly);
            configure?.Invoke(options);
        });

        return builder;
    }
}
```

- [ ] **Step 5: Run entrypoint test**

Run:

```powershell
dotnet test backend/dotnet/BuildingBlocks/tests/Tw.AspNetCore.Tests/Tw.AspNetCore.Tests.csproj --filter FullyQualifiedName~TwAspNetCoreInfrastructureExtensionsTests
```

Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add backend/dotnet/BuildingBlocks/src/Tw.AspNetCore/DependencyInjection backend/dotnet/BuildingBlocks/src/Tw.AspNetCore/Cancellation backend/dotnet/BuildingBlocks/tests/Tw.AspNetCore.Tests/DependencyInjection
git commit -m "feat: add aspnetcore infrastructure entrypoint"
```

### Task 3: Build Entry Chain Cache

**Files:**
- Create: `backend/dotnet/BuildingBlocks/src/Tw.AspNetCore/Aop/Mvc/EntryChainCache.cs`
- Test: `backend/dotnet/BuildingBlocks/tests/Tw.AspNetCore.Tests/Aop/MvcEntryChainCacheTests.cs`

- [ ] **Step 1: Write failing entry chain tests**

```csharp
[Fact]
public void EntryChainCache_Uses_Global_And_MatchEntry_Only()
{
    var options = new AutoRegistrationOptions();
    options.AddGlobalInterceptor<GlobalEntryInterceptor>(InterceptorScope.Entry);
    var autoMatch = new ControllerEntryInterceptor();

    var cache = EntryChainCache.Build([autoMatch], options);

    cache.GetInterceptors(typeof(OrdersController))
        .Should()
        .Equal(typeof(GlobalEntryInterceptor), typeof(ControllerEntryInterceptor));
}

private sealed class OrdersController : ControllerBase;
private sealed class GlobalEntryInterceptor : InterceptorBase;
private sealed class ControllerEntryInterceptor : InterceptorBase, IAutoMatchInterceptor
{
    public InterceptorScope Scope => InterceptorScope.Entry;
    public bool Match(Type serviceType, Type implementationType) => false;
    public bool MatchEntry(Type controllerType) => controllerType == typeof(OrdersController);
}
```

- [ ] **Step 2: Run test to verify it fails**

Run:

```powershell
dotnet test backend/dotnet/BuildingBlocks/tests/Tw.AspNetCore.Tests/Tw.AspNetCore.Tests.csproj --filter EntryChainCache_Uses_Global_And_MatchEntry_Only
```

Expected: FAIL because entry chain cache does not exist.

- [ ] **Step 3: Create entry chain cache**

Implement `EntryChainCache` so it:

```text
1. Indexes by controller type.
2. Adds global interceptors where Scope == Entry sorted by Order then type name.
3. Adds IAutoMatchInterceptor where Scope == Entry and MatchEntry(controllerType) is true.
4. Never calls Match(serviceType, implementationType) for Entry chain decisions.
5. Honors IgnoreInterceptorsAttribute on controller classes for global and automatic interceptors.
6. Does not use InterceptAttribute for controllers and records a startup diagnostic warning when the attribute is present.
```

- [ ] **Step 4: Run chain tests**

Run:

```powershell
dotnet test backend/dotnet/BuildingBlocks/tests/Tw.AspNetCore.Tests/Tw.AspNetCore.Tests.csproj --filter FullyQualifiedName~MvcEntryChainCacheTests
```

Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add backend/dotnet/BuildingBlocks/src/Tw.AspNetCore/Aop/Mvc/EntryChainCache.cs backend/dotnet/BuildingBlocks/tests/Tw.AspNetCore.Tests/Aop/MvcEntryChainCacheTests.cs
git commit -m "feat: compute mvc entry interceptor chains"
```

### Task 4: Add MVC Action Filter Adapter

**Files:**
- Create: `backend/dotnet/BuildingBlocks/src/Tw.AspNetCore/Aop/IHttpRequestFeature.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.AspNetCore/Aop/HttpRequestFeature.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.AspNetCore/Aop/Mvc/TwMvcAopFilter.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.AspNetCore/Aop/Mvc/TwMvcAopOptionsSetup.cs`
- Test: `backend/dotnet/BuildingBlocks/tests/Tw.AspNetCore.Tests/Aop/TwMvcAopFilterTests.cs`

- [ ] **Step 1: Write failing filter test**

```csharp
[Fact]
public async Task TwMvcAopFilter_Provides_Http_Feature_And_Invokes_Action_Boundary()
{
    var httpContext = new DefaultHttpContext();
    httpContext.TraceIdentifier = "trace-001";
    var interceptor = new CaptureHttpFeatureInterceptor();
    var filter = BuildFilter(interceptor);

    await InvokeActionFilterAsync(filter, httpContext);

    interceptor.TraceIdentifier.Should().Be("trace-001");
}

private sealed class CaptureHttpFeatureInterceptor : InterceptorBase
{
    public string? TraceIdentifier { get; private set; }

    public override async ValueTask InterceptAsync(IUnaryInvocationContext context)
    {
        TraceIdentifier = context.GetFeature<IHttpRequestFeature>()?.HttpContext.TraceIdentifier;
        await context.ProceedAsync();
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run:

```powershell
dotnet test backend/dotnet/BuildingBlocks/tests/Tw.AspNetCore.Tests/Tw.AspNetCore.Tests.csproj --filter TwMvcAopFilter_Provides_Http_Feature_And_Invokes_Action_Boundary
```

Expected: FAIL because HTTP feature and filter do not exist.

- [ ] **Step 3: Create HTTP feature**

```csharp
namespace Tw.AspNetCore.Aop;

using Microsoft.AspNetCore.Http;

/// <summary>提供 MVC/Web API 拦截链中的 HTTP 请求上下文</summary>
public interface IHttpRequestFeature
{
    /// <summary>当前 HTTP 请求上下文</summary>
    HttpContext HttpContext { get; }
}

/// <summary>默认 HTTP 请求特性</summary>
public sealed class HttpRequestFeature(HttpContext httpContext) : IHttpRequestFeature
{
    /// <inheritdoc />
    public HttpContext HttpContext { get; } = httpContext;
}
```

- [ ] **Step 4: Create MVC filter**

Implement `TwMvcAopFilter : IAsyncActionFilter` so it:

```text
1. Resolves EntryChainCache and interceptor instances from request services.
2. Builds an IUnaryInvocationContext for the controller action.
3. Adds HttpRequestFeature before invoking the chain.
4. Uses HttpContext.RequestAborted as ambient token through ICurrentCancellationTokenAccessor.
5. Calls ActionExecutionDelegate from ProceedAsync exactly once.
6. Writes ActionExecutedContext.Result when an interceptor short-circuits with IActionResult.
7. Does not claim to intercept result serialization after the action boundary.
```

- [ ] **Step 5: Create MVC options setup**

Implement `TwMvcAopOptionsSetup : IConfigureOptions<MvcOptions>` so it:

```text
1. Adds TwMvcAopFilter through MvcOptions.Filters.
2. Does not expose a UseInterceptors middleware API.
3. Keeps registration idempotent when Configure is called more than once.
```

- [ ] **Step 6: Run filter tests**

Run:

```powershell
dotnet test backend/dotnet/BuildingBlocks/tests/Tw.AspNetCore.Tests/Tw.AspNetCore.Tests.csproj --filter FullyQualifiedName~TwMvcAopFilterTests
```

Expected: PASS.

- [ ] **Step 7: Commit**

```bash
git add backend/dotnet/BuildingBlocks/src/Tw.AspNetCore/Aop backend/dotnet/BuildingBlocks/tests/Tw.AspNetCore.Tests/Aop
git commit -m "feat: add mvc entry aop adapter"
```

### Task 5: Verify P1 MVC Adapter Acceptance

**Files:**
- Verify: `backend/dotnet/BuildingBlocks/tests/Tw.AspNetCore.Tests/Tw.AspNetCore.Tests.csproj`
- Verify: `backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Tw.Core.Tests.csproj`

- [ ] **Step 1: Run ASP.NET Core tests**

Run:

```powershell
dotnet test backend/dotnet/BuildingBlocks/tests/Tw.AspNetCore.Tests/Tw.AspNetCore.Tests.csproj
```

Expected: PASS.

- [ ] **Step 2: Run core tests**

Run:

```powershell
dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Tw.Core.Tests.csproj
```

Expected: PASS.

## Self-Review Checklist

- [ ] `AddTwAspNetCoreInfrastructure()` is the public ASP.NET Core entrypoint.
- [ ] Autofac provider factory is configured before container registration.
- [ ] MVC adapter is registered through `IConfigureOptions<MvcOptions>`.
- [ ] Entry chains use global Entry interceptors and `MatchEntry()` only.
- [ ] Service scope interceptors do not appear in Entry chains.
- [ ] `IInvocationContext.GetFeature<IHttpRequestFeature>()` is non-null inside MVC adapter and null outside that scenario.
- [ ] Controller `[Intercept]` is warned and ignored rather than treated as Castle Service AOP.
