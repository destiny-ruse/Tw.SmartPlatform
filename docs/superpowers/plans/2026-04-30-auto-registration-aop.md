# DI Auto Registration And AOP Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement the first delivery batch from `docs/superpowers/specs/2026-04-29-auto-registration-aop-design.md`: DI auto-registration, options auto-registration, Castle service AOP, and MVC/Web API entry interception.

**Architecture:** Keep `Tw.Core` as the pure core library and introduce `Tw.DependencyInjection` for DI, options, cancellation, invocation context, interceptor chains, and Autofac/Castle integration. `Tw.AspNetCore` composes the new dependency-injection package with ASP.NET Core MVC through `AddTwAspNetCoreInfrastructure()` and an MVC action filter adapter. gRPC, CAP, and Worker adapters are separate later plans and must not block this first delivery batch.

**Tech Stack:** .NET 10, xUnit, FluentAssertions, Microsoft.Extensions.DependencyInjection/Options/Configuration, Autofac, Autofac.Extensions.DependencyInjection, Autofac.Extras.DynamicProxy, Castle.Core, Castle.Core.AsyncInterceptor, ASP.NET Core MVC.

---

## Execution Topology

| ID | Small Plan | Wave | Depends On | Parallel Marker | Outcome |
|---|---|---:|---|---|---|
| `G0` | Dependency admission and package source gate | 0 | none | `PAR-W0` | Approved package versions and exact package management changes are known before code adds production dependencies. |
| `C0` | `Tw.Core` configuration metadata migration | 0 | none | `PAR-W0` | `ConfigurationSectionAttribute` supports named options, direct injection, validate-on-start, and multiple declarations without adding DI dependencies to `Tw.Core`. |
| `S0` | `Tw.DependencyInjection` project scaffold | 1 | `G0` | `SEQ` | New source and test projects are in the solution with locked dependencies and baseline compile tests. |
| `R1` | Service metadata scan and exposure rules | 2 | `S0`, `C0` | `PAR-W2-A` | Lifecycle markers, registration attributes, default exposure, open-generic detection, keyed metadata rejection, and disabled registration are covered by unit tests. |
| `R2` | Registration decision engine and conflict裁决 | 3 | `R1` | `SEQ` | Replacement, collection, topology order, deterministic scan, and conflict errors produce a final registration plan. |
| `O1` | Options auto-registration | 2 | `S0`, `C0` | `PAR-W2-A` | Configuration metadata registers Microsoft Options, validation, named options, and direct injection semantics. |
| `I1` | Invocation context and cancellation token foundation | 2 | `S0` | `PAR-W2-A` | Ambient cancellation and `IInvocationContext` work independently of Castle and MVC. |
| `A1` | Interceptor contracts and chain calculation | 4 | `R2`, `I1` | `PAR-W4-A` | Global, matcher, explicit, and ignore rules calculate stable Service and Entry chains. |
| `A2` | Castle service AOP adapter | 5 | `A1`, `R2` | `SEQ` | Autofac registration emits interface proxies only when Service chains are present and preserves service lifetimes. |
| `H1` | Public host/container extension APIs | 5 | `R2`, `O1`, `A2` | `PAR-W5-A` | `AddAutoRegistration()` and `UseAutoRegistration()` are the manual composition entrypoints. |
| `M1` | ASP.NET Core MVC adapter and infrastructure entry | 5 | `A1`, `I1`, `H1` | `PAR-W5-A` | `AddTwAspNetCoreInfrastructure()` registers MVC Entry interception and HTTP request cancellation/features. |
| `Q1` | Cross-cutting diagnostics, docs, and verification | 6 | `A2`, `H1`, `M1` | `SEQ` | Full solution tests, restore/list/audit evidence, XML comments, diagnostics, and known residual risks are recorded. |
| `P2-GRPC` | gRPC adapter follow-up plan | later | `Q1` | `NEXT-PLAN` | Create a dedicated gRPC plan covering unary and streaming RPC adapters. |
| `P3-CAP` | CAP consumer adapter follow-up spec/plan | later | first-batch release | `NEXT-SPEC` | Define ack, retry, failure, idempotency, and message feature semantics before planning implementation. |
| `P3-WORKER` | Worker/HostedService adapter follow-up spec/plan | later | first-batch release | `NEXT-SPEC` | Define `ExecuteAsync`, per-item interception, `stoppingToken`, and exception semantics before planning implementation. |

### Parallel Rules

- `PAR-W0`: `G0` and `C0` can run concurrently because `C0` only touches `Tw.Core` and tests, while `G0` only records dependency decisions.
- `PAR-W2-A`: `R1`, `O1`, and `I1` can run concurrently after `S0` because they write disjoint files under `Registration/`, `Options/`, and `Invocation/`.
- `R2` is sequential after `R1` because it consumes metadata records and exposure decisions.
- `PAR-W4-A`: `A1` can run while documentation for `O1` is polished, but it must not start before `R2` and `I1`.
- `PAR-W5-A`: `H1` and `M1` can run in parallel only after `A1`; `M1` must wait for the public API shape from `H1` before final compilation.
- `Q1` is a final gate and must run after every first-batch code plan.

---

## File Structure

### Create

- `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Tw.DependencyInjection.csproj`  
  Production package for lifecycle markers, registration metadata, options registration, invocation context, interceptor chain calculation, and Autofac/Castle integration.
- `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Dependency/ITransientDependency.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Dependency/IScopedDependency.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Dependency/ISingletonDependency.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Registration/AutoRegistrationOptions.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Registration/AutoRegistrationServiceCollectionExtensions.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Registration/AutoRegistrationContainerBuilderExtensions.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Registration/AutoRegistrationModule.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Registration/ServiceRegistrationScanner.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Registration/ServiceRegistrationPlanner.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Registration/ServiceRegistrationDescriptor.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Registration/ServiceRegistrationDiagnostics.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Registration/Attributes/ExposeServicesAttribute.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Registration/Attributes/KeyedServiceAttribute.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Registration/Attributes/ReplaceServiceAttribute.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Registration/Attributes/DisableAutoRegistrationAttribute.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Registration/Attributes/CollectionServiceAttribute.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Options/OptionsAutoRegistrationExtensions.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Options/OptionsRegistrationScanner.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Options/OptionsRegistrationDescriptor.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Cancellation/ICancellationTokenProvider.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Cancellation/ICurrentCancellationTokenAccessor.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Cancellation/CurrentCancellationTokenAccessor.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Cancellation/NullCancellationTokenProvider.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Invocation/IInvocationContext.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Invocation/IUnaryInvocationContext.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Invocation/InvocationContext.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Invocation/InvocationFeatureCollection.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Interception/InterceptorBase.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Interception/IInterceptorMatcher.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Interception/InterceptorScope.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Interception/InterceptAttribute.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Interception/IgnoreInterceptorsAttribute.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Interception/InterceptorRegistration.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Interception/ServiceChainCache.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Interception/EntryChainCache.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Interception/Castle/CastleAsyncInterceptorAdapter.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Interception/Castle/CastleProxyRegistrationExtensions.cs`
- `backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests/Tw.DependencyInjection.Tests.csproj`
- `backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests/Registration/*.cs`
- `backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests/Options/*.cs`
- `backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests/Invocation/*.cs`
- `backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests/Interception/*.cs`
- `backend/dotnet/BuildingBlocks/tests/Tw.AspNetCore.Tests/Tw.AspNetCore.Tests.csproj`
- `backend/dotnet/BuildingBlocks/tests/Tw.AspNetCore.Tests/Mvc/*.cs`

### Modify

- `backend/dotnet/Tw.SmartPlatform.slnx`  
  Add `Tw.DependencyInjection`, `Tw.DependencyInjection.Tests`, and `Tw.AspNetCore.Tests`.
- `backend/dotnet/Build/Packages.ThirdParty.props`  
  Add exact `PackageVersion` entries for approved Autofac/Castle packages.
- `backend/dotnet/Build/Packages.Microsoft.props`  
  Add exact Microsoft.Extensions package versions if the project cannot compile through shared framework references.
- `backend/dotnet/BuildingBlocks/src/Tw.Core/Configuration/ConfigurationSectionAttribute.cs`  
  Extend metadata while keeping `Tw.Core` free of DI, Autofac, Castle, ASP.NET Core, and gRPC references.
- `backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/ConfigurationTests.cs`  
  Replace the old single-attribute expectation with multi-attribute and property tests.
- `backend/dotnet/BuildingBlocks/src/Tw.AspNetCore/Tw.AspNetCore.csproj`  
  Add a project reference to `Tw.DependencyInjection`; keep `Microsoft.NET.Sdk.Web`.
- `backend/dotnet/BuildingBlocks/src/Tw.AspNetCore/*.cs`  
  Add ASP.NET Core composition, HTTP cancellation provider, HTTP feature, and MVC action filter adapter.
- `backend/dotnet/BuildingBlocks/*/packages.lock.json`  
  Regenerate only for projects affected by new or changed references.

---

## `G0`: Dependency Admission And Package Source Gate

**Files:**
- Modify: `backend/dotnet/Build/Packages.ThirdParty.props`
- Modify: `backend/dotnet/Build/Packages.Microsoft.props`
- Read: `backend/dotnet/NuGet.Config`
- Read: `docs/standards/processes/dependency-onboarding.md`
- Read: `docs/standards/rules/dependency-policy.md`

- [ ] **Step 1: Record dependency admission evidence before package edits**

Run:

```powershell
dotnet nuget list source --configfile backend/dotnet/NuGet.Config
```

Expected: output contains `Huawei` and `nuget.org`.

- [ ] **Step 2: Verify package source mapping remains explicit**

Run:

```powershell
dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Tw.Core.Tests.csproj --filter NuGetConfig_Maps_All_Configured_Sources_When_CentralPackageManagement_Uses_Multiple_Sources
```

Expected: `Passed!`; any existing NU1507 warning must be recorded as pre-existing if it appears.

- [ ] **Step 3: Add exact third-party package versions**

Patch `backend/dotnet/Build/Packages.ThirdParty.props` with the approved versions from the dependency admission record:

```xml
<Project>
  <ItemGroup>
    <PackageVersion Include="Autofac" Version="9.1.0" />
    <PackageVersion Include="Autofac.Extensions.DependencyInjection" Version="11.0.0" />
    <PackageVersion Include="Autofac.Extras.DynamicProxy" Version="7.1.0" />
    <PackageVersion Include="Castle.Core" Version="5.2.1" />
    <PackageVersion Include="Castle.Core.AsyncInterceptor" Version="2.1.0" />
  </ItemGroup>
</Project>
```

- [ ] **Step 4: Add Microsoft package versions only if needed by compile**

If `Tw.DependencyInjection` cannot compile without explicit package references, patch `backend/dotnet/Build/Packages.Microsoft.props` with exact versions aligned to the .NET 10 SDK:

```xml
<PackageVersion Include="Microsoft.Extensions.Configuration.Abstractions" Version="10.0.0" />
<PackageVersion Include="Microsoft.Extensions.Configuration.Binder" Version="10.0.0" />
<PackageVersion Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="10.0.0" />
<PackageVersion Include="Microsoft.Extensions.Hosting.Abstractions" Version="10.0.0" />
<PackageVersion Include="Microsoft.Extensions.Options" Version="10.0.0" />
<PackageVersion Include="Microsoft.Extensions.Options.ConfigurationExtensions" Version="10.0.0" />
<PackageVersion Include="Microsoft.Extensions.Options.DataAnnotations" Version="10.0.0" />
```

- [ ] **Step 5: Restore and record lock-file state**

Run:

```powershell
dotnet restore backend/dotnet/Tw.SmartPlatform.slnx --use-lock-file
dotnet restore backend/dotnet/Tw.SmartPlatform.slnx --locked-mode
```

Expected: restore completes; lock files are created or updated only for projects touched by this plan.

- [ ] **Step 6: Commit dependency gate**

```powershell
git add backend/dotnet/Build/Packages.ThirdParty.props backend/dotnet/Build/Packages.Microsoft.props backend/dotnet/NuGet.Config backend/dotnet/BuildingBlocks/**/packages.lock.json
git commit -m "chore(dotnet): admit auto registration dependencies"
```

---

## `C0`: `Tw.Core` Configuration Metadata Migration

**Files:**
- Modify: `backend/dotnet/BuildingBlocks/src/Tw.Core/Configuration/ConfigurationSectionAttribute.cs`
- Modify: `backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/ConfigurationTests.cs`
- Verify: `backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Reflection/TypeFinderTests.cs`

- [ ] **Step 1: Write failing tests for the expanded attribute contract**

Replace `ConfigurationSectionAttribute_Targets_Classes` assertions and add constructor/property tests:

```csharp
[Fact]
public void ConfigurationSectionAttribute_Targets_Classes_And_Allows_Multiple_Declarations()
{
    var usage = typeof(ConfigurationSectionAttribute)
        .GetCustomAttributes(typeof(AttributeUsageAttribute), inherit: false)
        .Should()
        .ContainSingle()
        .Subject
        .Should()
        .BeOfType<AttributeUsageAttribute>()
        .Subject;

    usage.ValidOn.Should().Be(AttributeTargets.Class);
    usage.AllowMultiple.Should().BeTrue();
    usage.Inherited.Should().BeTrue();
}

[Fact]
public void ConfigurationSectionAttribute_Stores_Options_Metadata()
{
    var attribute = new ConfigurationSectionAttribute(
        "Auth",
        OptionsName = "Primary",
        ValidateOnStart = true,
        DirectInject = true);

    attribute.Name.Should().Be("Auth");
    attribute.OptionsName.Should().Be("Primary");
    attribute.ValidateOnStart.Should().BeTrue();
    attribute.DirectInject.Should().BeTrue();
}
```

- [ ] **Step 2: Run tests and confirm failure**

Run:

```powershell
dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Tw.Core.Tests.csproj --filter "ConfigurationSectionAttribute"
```

Expected: failure because `AllowMultiple` is still false and new properties do not exist.

- [ ] **Step 3: Implement the attribute expansion**

Patch `ConfigurationSectionAttribute.cs` to this public shape:

```csharp
namespace Tw.Core.Configuration;

/// <summary>
/// 标识应绑定到选项类型的配置节。
/// </summary>
/// <param name="name">调用方在选项绑定期间使用的非空配置节名称。</param>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
public sealed class ConfigurationSectionAttribute(string name) : Attribute
{
    /// <summary>
    /// 与被标注选项类型关联的配置节名称。
    /// </summary>
    public string Name { get; } = Check.NotNullOrWhiteSpace(name);

    /// <summary>
    /// Microsoft Options 命名实例名称；为 <see langword="null"/> 时表示默认实例。
    /// </summary>
    public string? OptionsName { get; init; }

    /// <summary>
    /// 是否在主机构建完成后立即验证该配置实例。
    /// </summary>
    public bool? ValidateOnStart { get; init; }

    /// <summary>
    /// 是否允许直接解析选项类型本体，值来自 <c>IOptionsMonitor&lt;TOptions&gt;.CurrentValue</c>。
    /// </summary>
    public bool DirectInject { get; init; }
}
```

- [ ] **Step 4: Preserve the no-DI dependency constraint**

Run:

```powershell
dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Tw.Core.Tests.csproj --filter "ConfigurationSectionAttribute|TypeFinderExtensions_Do_Not_Require_DependencyInjection"
```

Expected: all selected tests pass.

- [ ] **Step 5: Commit core metadata migration**

```powershell
git add backend/dotnet/BuildingBlocks/src/Tw.Core/Configuration/ConfigurationSectionAttribute.cs backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/ConfigurationTests.cs
git commit -m "feat(core): expand configuration section metadata"
```

---

## `S0`: `Tw.DependencyInjection` Project Scaffold

**Files:**
- Create: `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Tw.DependencyInjection.csproj`
- Create: `backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests/Tw.DependencyInjection.Tests.csproj`
- Modify: `backend/dotnet/Tw.SmartPlatform.slnx`

- [ ] **Step 1: Create failing solution reference by adding projects to `.slnx`**

Add the project entries under the existing BuildingBlocks folders:

```xml
<Project Path="BuildingBlocks/src/Tw.DependencyInjection/Tw.DependencyInjection.csproj" />
<Project Path="BuildingBlocks/tests/Tw.DependencyInjection.Tests/Tw.DependencyInjection.Tests.csproj" />
```

Run:

```powershell
dotnet build backend/dotnet/Tw.SmartPlatform.slnx --no-restore
```

Expected: failure because the project files do not exist yet.

- [ ] **Step 2: Create the production project**

Create `Tw.DependencyInjection.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>true</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\Tw.Core\Tw.Core.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Autofac" />
    <PackageReference Include="Autofac.Extensions.DependencyInjection" />
    <PackageReference Include="Autofac.Extras.DynamicProxy" />
    <PackageReference Include="Castle.Core" />
    <PackageReference Include="Castle.Core.AsyncInterceptor" />
    <PackageReference Include="Microsoft.Extensions.Configuration.Abstractions" />
    <PackageReference Include="Microsoft.Extensions.Configuration.Binder" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" />
    <PackageReference Include="Microsoft.Extensions.Hosting.Abstractions" />
    <PackageReference Include="Microsoft.Extensions.Options" />
    <PackageReference Include="Microsoft.Extensions.Options.ConfigurationExtensions" />
    <PackageReference Include="Microsoft.Extensions.Options.DataAnnotations" />
  </ItemGroup>
</Project>
```

- [ ] **Step 3: Create the test project**

Create `Tw.DependencyInjection.Tests.csproj`:

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
    <ProjectReference Include="..\..\src\Tw.DependencyInjection\Tw.DependencyInjection.csproj" />
    <ProjectReference Include="..\Tw.TestBase\Tw.TestBase.csproj" />
  </ItemGroup>
</Project>
```

- [ ] **Step 4: Add lifecycle marker interfaces**

Create the three marker files with XML comments:

```csharp
namespace Tw.DependencyInjection;

/// <summary>
/// 标记实现类型应以瞬时生命周期参与自动注册。
/// </summary>
public interface ITransientDependency;

/// <summary>
/// 标记实现类型应以作用域生命周期参与自动注册。
/// </summary>
public interface IScopedDependency;

/// <summary>
/// 标记实现类型应以单例生命周期参与自动注册。
/// </summary>
public interface ISingletonDependency;
```

- [ ] **Step 5: Build scaffold**

Run:

```powershell
dotnet restore backend/dotnet/Tw.SmartPlatform.slnx --use-lock-file
dotnet build backend/dotnet/Tw.SmartPlatform.slnx --no-restore
```

Expected: build passes with the new empty package and test project.

- [ ] **Step 6: Commit scaffold**

```powershell
git add backend/dotnet/Tw.SmartPlatform.slnx backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests
git commit -m "feat(di): add dependency injection building block"
```

---

## `R1`: Service Metadata Scan And Exposure Rules

**Files:**
- Create/modify: `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Registration/**`
- Test: `backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests/Registration/ServiceRegistrationScannerTests.cs`

- [ ] **Step 1: Write tests for lifecycle detection and default exposure**

Add tests with subjects implementing `IScopedDependency`, a business interface, and no business interface:

```csharp
[Fact]
public void Scanner_Exposes_Business_Interfaces_For_Lifecycle_Implementations()
{
    var descriptors = Scan(typeof(OrderService));

    descriptors.Should().ContainSingle()
        .Which.ServiceTypes.Should().ContainSingle(type => type == typeof(IOrderService));
}

[Fact]
public void Scanner_Exposes_Implementation_Type_When_No_Business_Interface_Exists()
{
    var descriptors = Scan(typeof(ConcreteWorker));

    descriptors.Should().ContainSingle()
        .Which.ServiceTypes.Should().ContainSingle(type => type == typeof(ConcreteWorker));
}
```

- [ ] **Step 2: Write tests for explicit metadata attributes**

Cover `[ExposeServices]`, `IncludeSelf`, `[DisableAutoRegistration]`, `[KeyedService]`, `[CollectionService]`, `[ReplaceService]`, and open generic exposure:

```csharp
[Fact]
public void Scanner_Rejects_Open_Generic_Keyed_Service()
{
    var act = () => Scan(typeof(KeyedRepository<>));

    act.Should().Throw<TwConfigurationException>()
        .WithMessage("*开放泛型*KeyedService*不支持*");
}
```

- [ ] **Step 3: Implement attribute types**

Use public sealed attributes with XML comments and these constructor/property contracts:

```csharp
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
public sealed class ExposeServicesAttribute(params Type[] serviceTypes) : Attribute
{
    public IReadOnlyList<Type> ServiceTypes { get; } = serviceTypes;
    public bool IncludeSelf { get; init; }
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class KeyedServiceAttribute(object key) : Attribute
{
    public object Key { get; } = key;
}
```

Implement the remaining attributes with the contracts from the spec:

```csharp
public sealed class ReplaceServiceAttribute : Attribute { public int Order { get; init; } }
public sealed class DisableAutoRegistrationAttribute : Attribute { }
public sealed class CollectionServiceAttribute : Attribute { public int Order { get; init; } }
```

- [ ] **Step 4: Implement `ServiceRegistrationScanner`**

Rules:

- Include types from `ITypeFinder.FindTypes()` and manually include open generic type definitions from the same assemblies.
- Skip abstract types, interfaces, disabled types, and types without a lifecycle marker.
- Exclude `System.*`, `Microsoft.*`, and `Tw.DependencyInjection` marker interfaces from default business-interface exposure.
- Keep open generic support local to `Tw.DependencyInjection`; do not change `Tw.Core.TypeFinder.FindTypes(Type)` behavior.

- [ ] **Step 5: Run focused scanner tests**

```powershell
dotnet test backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests/Tw.DependencyInjection.Tests.csproj --filter ServiceRegistrationScannerTests
```

Expected: all scanner tests pass.

- [ ] **Step 6: Commit scanner rules**

```powershell
git add backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Registration backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests/Registration
git commit -m "feat(di): scan auto registration metadata"
```

---

## `R2`: Registration Decision Engine And Conflict裁决

**Files:**
- Create/modify: `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Registration/ServiceRegistrationPlanner.cs`
- Create/modify: `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Registration/ServiceRegistrationDescriptor.cs`
- Test: `backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests/Registration/ServiceRegistrationPlannerTests.cs`

- [ ] **Step 1: Write failing tests for replacement and collection semantics**

Cover these cases with explicit assertions:

- `[ReplaceService]` replaces only the intersection of exposed services.
- Replacement with mismatched keyed metadata leaves the original registration intact and records a warning.
- `[CollectionService]` preserves all implementations by `(ServiceType, Key)` and orders by `Order`, then type full name.
- Mixed collection and non-collection implementations for the same `(ServiceType, Key)` throw `TwConfigurationException`.

- [ ] **Step 2: Write failing tests for topology and deterministic ordering**

Use dynamically selected test assemblies or in-test descriptor builders to assert:

```csharp
[Fact]
public void Planner_Throws_With_All_Conflicting_Type_Names_When_Same_Level_Conflict_Remains()
{
    var act = () => Plan(
        Descriptor.For<IOrderService, FirstOrderService>(),
        Descriptor.For<IOrderService, SecondOrderService>());

    act.Should().Throw<TwConfigurationException>()
        .WithMessage("*FirstOrderService*SecondOrderService*");
}
```

- [ ] **Step 3: Implement registration descriptor model**

Minimum fields:

```csharp
public sealed record ServiceRegistrationDescriptor(
    Type ImplementationType,
    Type ServiceType,
    ServiceLifetime Lifetime,
    object? Key,
    bool IsCollection,
    bool IsReplacement,
    int Order,
    string AssemblyName);
```

- [ ] **Step 4: Implement planner decisions**

Rules:

- Group by `(ServiceType, Key)`.
- Reject mixed collection/non-collection groups.
- For collection groups, keep every descriptor in stable order.
- For non-collection groups, apply replacement first, then topology priority, then `Order`.
- Throw `TwConfigurationException` with all conflicting implementation type full names if a single winner cannot be chosen.
- Emit diagnostics warnings for cross-assembly topology wins and keyed replacement misses.

- [ ] **Step 5: Run planner tests twice**

```powershell
dotnet test backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests/Tw.DependencyInjection.Tests.csproj --filter ServiceRegistrationPlannerTests
dotnet test backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests/Tw.DependencyInjection.Tests.csproj --filter ServiceRegistrationPlannerTests
```

Expected: both runs pass and assertion order is stable.

- [ ] **Step 6: Commit planner**

```powershell
git add backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Registration backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests/Registration
git commit -m "feat(di): plan deterministic service registrations"
```

---

## `O1`: Options Auto-Registration

**Files:**
- Create/modify: `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Options/**`
- Test: `backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests/Options/OptionsAutoRegistrationTests.cs`

- [ ] **Step 1: Write tests for section discovery**

Cover:

- `IConfigurableOptions` plus `[ConfigurationSection]`.
- Startup assembly `*Options` / `*Settings` naming convention.
- Class library options require `IConfigurableOptions` or `[ConfigurationSection]`.
- Duplicate default `OptionsName = null` declarations fail.
- Duplicate non-null `OptionsName` declarations fail.

- [ ] **Step 2: Write tests for validation and direct injection**

Use `ServiceCollection`, `ConfigurationBuilder`, and `ValidateOnStart`:

```csharp
[Fact]
public void OptionsAutoRegistration_DirectInject_Uses_OptionsMonitor_CurrentValue()
{
    var services = new ServiceCollection();
    var configuration = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Payment:MerchantId"] = "merchant-001"
        })
        .Build();

    services.AddOptionsAutoRegistration(configuration, typeof(PaymentOptions).Assembly);
    using var provider = services.BuildServiceProvider();

    provider.GetRequiredService<PaymentOptions>().MerchantId.Should().Be("merchant-001");
}
```

- [ ] **Step 3: Implement descriptor scanner**

`OptionsRegistrationScanner` must produce one descriptor per `ConfigurationSectionAttribute` and must compute default sections by removing `Options` or `Settings` suffix only when no explicit attribute is present.

- [ ] **Step 4: Implement registration extension**

Register each descriptor through Microsoft Options:

```csharp
services.AddOptions<TOptions>(optionsName)
    .Bind(configuration.GetSection(sectionName))
    .ValidateDataAnnotations();
```

Call `ValidateOnStart()` when the descriptor or global policy requires startup validation.

- [ ] **Step 5: Implement direct injection**

For non-named descriptors with `DirectInject = true`, register:

```csharp
services.AddTransient(typeof(TOptions), provider =>
    provider.GetRequiredService<IOptionsMonitor<TOptions>>().CurrentValue);
```

Named options must not register `TOptions` directly.

- [ ] **Step 6: Run options tests**

```powershell
dotnet test backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests/Tw.DependencyInjection.Tests.csproj --filter OptionsAutoRegistrationTests
```

Expected: all options registration, duplicate detection, validation, and direct injection tests pass.

- [ ] **Step 7: Commit options registration**

```powershell
git add backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Options backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests/Options
git commit -m "feat(di): register configuration options automatically"
```

---

## `I1`: Invocation Context And Cancellation Foundation

**Files:**
- Create/modify: `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Cancellation/**`
- Create/modify: `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Invocation/**`
- Test: `backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests/Invocation/InvocationContextTests.cs`
- Test: `backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests/Invocation/CancellationTokenProviderTests.cs`

- [ ] **Step 1: Write cancellation accessor tests**

Assert:

- `NullCancellationTokenProvider.Token` returns `CancellationToken.None`.
- `CurrentCancellationTokenAccessor.Use(token)` changes ambient token inside the returned `IDisposable` scope.
- Disposing nested scopes restores the previous token.
- The ambient token crosses `await`.

- [ ] **Step 2: Implement cancellation contracts and ambient provider**

Use `AsyncLocal<CancellationToken?>` in `CurrentCancellationTokenAccessor` and expose it through `ICancellationTokenProvider`.

- [ ] **Step 3: Write invocation feature tests**

Assert:

```csharp
context.Items["TraceId"] = "abc";
context.GetFeature<TestFeature>().Should().BeSameAs(feature);
context.GetFeature<MissingFeature>().Should().BeNull();
```

- [ ] **Step 4: Implement invocation context**

`IInvocationContext` must expose:

```csharp
CancellationToken CancellationToken { get; }
IDictionary<string, object?> Items { get; }
TFeature? GetFeature<TFeature>() where TFeature : class;
```

`IUnaryInvocationContext` must add:

```csharp
MethodInfo Method { get; }
object?[] Arguments { get; }
Type ReturnType { get; }
object? ReturnValue { get; set; }
ValueTask ProceedAsync();
```

- [ ] **Step 5: Run invocation tests**

```powershell
dotnet test backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests/Tw.DependencyInjection.Tests.csproj --filter "InvocationContextTests|CancellationTokenProviderTests"
```

Expected: all selected tests pass.

- [ ] **Step 6: Commit invocation foundation**

```powershell
git add backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Cancellation backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Invocation backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests/Invocation
git commit -m "feat(di): add invocation and cancellation context"
```

---

## `A1`: Interceptor Contracts And Chain Calculation

**Files:**
- Create/modify: `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Interception/**`
- Test: `backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests/Interception/InterceptorChainTests.cs`

- [ ] **Step 1: Write chain order tests**

Assert service chain order:

1. Global `InterceptorScope.Service`
2. `IInterceptorMatcher.MatchService()`
3. Explicit `[Intercept]`

Assert entry chain order:

1. Global `InterceptorScope.Entry`
2. `IInterceptorMatcher.MatchEntry()`

Within each layer, sort by `Order`, then interceptor type full name.

- [ ] **Step 2: Write scope isolation tests**

Service-scope matcher must not enter Entry chains. Entry-scope matcher must not enter Service chains.

- [ ] **Step 3: Write ignore tests**

Assert `[IgnoreInterceptors]` suppresses global and matcher interceptors but does not suppress explicit `[Intercept]`. Interface-level ignore must apply to implementing classes.

- [ ] **Step 4: Implement public interception contracts**

Use the public shapes from the design:

```csharp
public enum InterceptorScope
{
    Service = 0,
    Entry = 1
}

public interface IInterceptorMatcher
{
    Type InterceptorType { get; }
    InterceptorScope Scope { get; }
    int Order => 0;
    bool MatchService(Type serviceType, Type implementationType) => false;
    bool MatchEntry(Type entryType) => false;
}
```

- [ ] **Step 5: Implement `InterceptorBase`**

`InterceptorBase` must provide override points for unary and async-enumerable flows:

```csharp
public abstract class InterceptorBase
{
    public virtual ValueTask InterceptAsync(IUnaryInvocationContext context)
        => context.ProceedAsync();

    public virtual IAsyncEnumerable<T> InterceptAsyncEnumerable<T>(
        IInvocationContext context,
        IAsyncEnumerable<T> source)
        => source;
}
```

- [ ] **Step 6: Implement caches**

`ServiceChainCache` precomputes by implementation type. `EntryChainCache` lazily computes by controller or entry type. Both caches must return immutable interceptor registration arrays.

- [ ] **Step 7: Run chain tests**

```powershell
dotnet test backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests/Tw.DependencyInjection.Tests.csproj --filter InterceptorChainTests
```

Expected: all chain, scope, and ignore tests pass.

- [ ] **Step 8: Commit chain calculation**

```powershell
git add backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Interception backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests/Interception
git commit -m "feat(di): calculate interceptor chains"
```

---

## `A2`: Castle Service AOP Adapter

**Files:**
- Create/modify: `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Interception/Castle/**`
- Modify: `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Registration/AutoRegistrationModule.cs`
- Test: `backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests/Interception/CastleServiceInterceptorTests.cs`

- [ ] **Step 1: Write integration tests for sync and async return types**

Cover sync, `Task`, `Task<T>`, `ValueTask`, `ValueTask<T>`, exceptions, short-circuit return values, and duplicate `ProceedAsync()` failure.

- [ ] **Step 2: Write proxy registration tests**

Assert:

- Interface service with non-empty chain is proxied.
- Empty chain is not proxied.
- Concrete-only service emits a warning and is not proxied by default.
- Explicit class proxy mode warns for non-virtual methods.

- [ ] **Step 3: Implement Castle async adapter**

Use `IAsyncInterceptor` from `Castle.Core.AsyncInterceptor`. Runtime interceptor instances must be resolved from Autofac for each invocation using the `InterceptorType` stored in the chain.

- [ ] **Step 4: Implement `ProceedAsync()` single-call enforcement**

The invocation context must track a boolean flag. A second call throws `TwConfigurationException` with a simplified Chinese message naming the target method.

- [ ] **Step 5: Implement return value validation**

When an interceptor short-circuits, validate `ReturnValue` against the target return type. A wrong type throws `TwConfigurationException` with the target method and expected return type, without dumping argument values.

- [ ] **Step 6: Integrate proxy registration into Autofac module**

`AutoRegistrationModule` must:

- Register all planned services with Autofac lifetimes.
- Attach interface interceptors only when the Service chain is non-empty.
- Preserve `InstancePerDependency`, `InstancePerLifetimeScope`, and `SingleInstance`.
- Register collection services without collapsing `IEnumerable<T>`.
- Apply keyed registrations by `(ServiceType, Key)`.

- [ ] **Step 7: Run Castle tests**

```powershell
dotnet test backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests/Tw.DependencyInjection.Tests.csproj --filter CastleServiceInterceptorTests
```

Expected: all Castle service interception tests pass.

- [ ] **Step 8: Commit Castle adapter**

```powershell
git add backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Interception/Castle backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Registration backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests/Interception
git commit -m "feat(di): add castle service interception"
```

---

## `H1`: Public Host And Container Extension APIs

**Files:**
- Create/modify: `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Registration/AutoRegistrationOptions.cs`
- Create/modify: `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Registration/AutoRegistrationServiceCollectionExtensions.cs`
- Create/modify: `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Registration/AutoRegistrationContainerBuilderExtensions.cs`
- Test: `backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests/Registration/AutoRegistrationExtensionTests.cs`

- [ ] **Step 1: Write API tests for call ordering**

Assert `UseAutoRegistration(builder.Services)` throws when `AddAutoRegistration()` was not called first.

- [ ] **Step 2: Write Host integration test**

Use `Host.CreateApplicationBuilder()`, `AutofacServiceProviderFactory`, `AddAutoRegistration()`, and `ConfigureContainer<ContainerBuilder>()` to resolve a scoped service without manual `AddScoped()`.

- [ ] **Step 3: Implement `AutoRegistrationOptions`**

Public methods:

```csharp
public AutoRegistrationOptions AddAssembly(Assembly assembly);
public AutoRegistrationOptions AddAssemblyOf<T>();
public AutoRegistrationOptions EnableOptionsAutoRegistration(bool enabled = true);
public AutoRegistrationOptions AddGlobalInterceptor<TInterceptor>(
    InterceptorScope scope,
    int order = 0)
    where TInterceptor : InterceptorBase;
```

- [ ] **Step 4: Implement `AddAutoRegistration()`**

Signature:

```csharp
public static IServiceCollection AddAutoRegistration(
    this IServiceCollection services,
    IConfiguration configuration,
    Action<AutoRegistrationOptions>? configure = null);
```

The implementation stores one options instance in `IServiceCollection`, registers cancellation defaults, registers matcher types, and calls options auto-registration when enabled.

- [ ] **Step 5: Implement `UseAutoRegistration()`**

Signature:

```csharp
public static ContainerBuilder UseAutoRegistration(
    this ContainerBuilder builder,
    IServiceCollection services);
```

The implementation reads the stored options and registers `AutoRegistrationModule`.

- [ ] **Step 6: Run extension tests**

```powershell
dotnet test backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests/Tw.DependencyInjection.Tests.csproj --filter AutoRegistrationExtensionTests
```

Expected: API ordering and Host integration tests pass.

- [ ] **Step 7: Commit public extension APIs**

```powershell
git add backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Registration backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests/Registration
git commit -m "feat(di): expose auto registration host APIs"
```

---

## `M1`: ASP.NET Core MVC Adapter And Infrastructure Entry

**Files:**
- Modify: `backend/dotnet/BuildingBlocks/src/Tw.AspNetCore/Tw.AspNetCore.csproj`
- Create/modify: `backend/dotnet/BuildingBlocks/src/Tw.AspNetCore/AddTwAspNetCoreInfrastructureExtensions.cs`
- Create/modify: `backend/dotnet/BuildingBlocks/src/Tw.AspNetCore/Cancellation/HttpContextCancellationTokenProvider.cs`
- Create/modify: `backend/dotnet/BuildingBlocks/src/Tw.AspNetCore/Features/IHttpRequestFeature.cs`
- Create/modify: `backend/dotnet/BuildingBlocks/src/Tw.AspNetCore/Mvc/TwEntryInterceptorFilter.cs`
- Create/modify: `backend/dotnet/BuildingBlocks/src/Tw.AspNetCore/Mvc/TwMvcOptionsSetup.cs`
- Create: `backend/dotnet/BuildingBlocks/tests/Tw.AspNetCore.Tests/Tw.AspNetCore.Tests.csproj`
- Test: `backend/dotnet/BuildingBlocks/tests/Tw.AspNetCore.Tests/Mvc/TwEntryInterceptorFilterTests.cs`

- [ ] **Step 1: Add project references and test project**

`Tw.AspNetCore.csproj` must reference:

```xml
<ItemGroup>
  <ProjectReference Include="..\Tw.DependencyInjection\Tw.DependencyInjection.csproj" />
</ItemGroup>
```

Create `Tw.AspNetCore.Tests.csproj` with references to `Tw.AspNetCore`, `Tw.DependencyInjection`, and `Tw.TestBase`.

- [ ] **Step 2: Write MVC adapter tests**

Assert:

- `IConfigureOptions<MvcOptions>` adds exactly one global filter for entry interception.
- `IInvocationContext.GetFeature<IHttpRequestFeature>()` returns non-null inside the MVC action invocation.
- The filter wraps action execution, not result serialization.
- `HttpContext.RequestAborted` becomes the ambient cancellation token when no method parameter token exists.

- [ ] **Step 3: Implement HTTP cancellation provider**

`HttpContextCancellationTokenProvider` reads `IHttpContextAccessor.HttpContext?.RequestAborted` and falls back to the ambient/default provider when no HTTP context exists.

- [ ] **Step 4: Implement HTTP feature**

```csharp
public interface IHttpRequestFeature
{
    HttpContext HttpContext { get; }
    ActionContext ActionContext { get; }
}
```

- [ ] **Step 5: Implement MVC filter**

Use `IAsyncActionFilter`. The filter must create an Entry invocation context, add the HTTP feature, apply `EntryChainCache`, call `ActionExecutionDelegate`, and set the action result from the MVC pipeline result.

- [ ] **Step 6: Implement infrastructure entry**

Signature:

```csharp
public static WebApplicationBuilder AddTwAspNetCoreInfrastructure(
    this WebApplicationBuilder builder,
    Action<AutoRegistrationOptions>? configure = null);
```

The implementation must:

- Call `builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory())`.
- Call `builder.Services.AddAutoRegistration(builder.Configuration, configure)`.
- Call `builder.Host.ConfigureContainer<ContainerBuilder>(containerBuilder => containerBuilder.UseAutoRegistration(builder.Services))`.
- Register `IHttpContextAccessor`.
- Register `HttpContextCancellationTokenProvider` as the effective `ICancellationTokenProvider` for ASP.NET Core.
- Register `IConfigureOptions<MvcOptions>` for the MVC filter.

- [ ] **Step 7: Run ASP.NET Core tests**

```powershell
dotnet test backend/dotnet/BuildingBlocks/tests/Tw.AspNetCore.Tests/Tw.AspNetCore.Tests.csproj --filter "TwEntryInterceptorFilterTests"
```

Expected: MVC adapter and infrastructure tests pass.

- [ ] **Step 8: Commit ASP.NET Core adapter**

```powershell
git add backend/dotnet/BuildingBlocks/src/Tw.AspNetCore backend/dotnet/BuildingBlocks/tests/Tw.AspNetCore.Tests backend/dotnet/Tw.SmartPlatform.slnx
git commit -m "feat(aspnetcore): add mvc entry interception"
```

---

## `Q1`: Diagnostics, Documentation, And Verification Gate

**Files:**
- Modify/create: `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/README.md`
- Modify/create: `backend/dotnet/BuildingBlocks/src/Tw.AspNetCore/README.md`
- Verify: all touched source and test files

- [ ] **Step 1: Add diagnostics tests**

Assert development diagnostics include scan, sort, register, AOP, total elapsed timings, service counts, intercepted counts, and warnings. Assert production configuration can suppress diagnostics output.

- [ ] **Step 2: Verify public XML comments**

Run:

```powershell
dotnet build backend/dotnet/Tw.SmartPlatform.slnx --no-restore
```

Expected: no nullable warnings; public API XML comments are present by code review and comments-dotnet standard.

- [ ] **Step 3: Run focused first-batch test suites**

```powershell
dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Tw.Core.Tests.csproj
dotnet test backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests/Tw.DependencyInjection.Tests.csproj
dotnet test backend/dotnet/BuildingBlocks/tests/Tw.AspNetCore.Tests/Tw.AspNetCore.Tests.csproj
```

Expected: all three test projects pass.

- [ ] **Step 4: Run full dotnet solution tests**

```powershell
dotnet test backend/dotnet/Tw.SmartPlatform.slnx
```

Expected: all solution tests pass; any pre-existing NU1507 warning is recorded with scope and follow-up owner.

- [ ] **Step 5: Verify dependency closure**

Run:

```powershell
dotnet list backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Tw.DependencyInjection.csproj package --include-transitive
dotnet list backend/dotnet/BuildingBlocks/src/Tw.AspNetCore/Tw.AspNetCore.csproj package --include-transitive
```

Expected: Autofac/Castle packages appear only where needed; `Tw.Core` still has no DI, Autofac, Castle, ASP.NET Core, or gRPC package dependency.

- [ ] **Step 6: Restore locked mode**

```powershell
dotnet restore backend/dotnet/Tw.SmartPlatform.slnx --locked-mode
```

Expected: restore succeeds without modifying lock files.

- [ ] **Step 7: Record residual risks**

Add a short section to the PR description or implementation notes:

- gRPC adapter is intentionally deferred to `P2-GRPC`.
- CAP and Worker adapters require separate specs before implementation.
- Method-level `[IgnoreInterceptors]` is out of scope.
- Open generic keyed services are rejected at startup.
- MVC adapter covers action invocation boundary, not result serialization after the action result leaves MVC action execution.

- [ ] **Step 8: Commit final gate artifacts**

```powershell
git add backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/README.md backend/dotnet/BuildingBlocks/src/Tw.AspNetCore/README.md backend/dotnet/BuildingBlocks/**/packages.lock.json
git commit -m "docs(di): record auto registration verification"
```

---

## Self-Review

### Spec Coverage

- P0 automatic registration, default exposure, explicit exposure, disable, keyed, replace, collection, topology, deterministic scan, options metadata, named options, validation, and direct injection are covered by `C0`, `S0`, `R1`, `R2`, and `O1`.
- P1 service AOP, invocation context, cancellation token priority, matcher lifecycle, chain caches, ignore rules, Castle interface proxying, empty-chain skip, Host entry APIs, and MVC adapter are covered by `I1`, `A1`, `A2`, `H1`, and `M1`.
- Cross-cutting dependency admission, package source mapping, exact package versions, lock files, diagnostics, XML comments, and verification are covered by `G0` and `Q1`.
- P2 gRPC is intentionally split into `P2-GRPC` after `Q1`.
- P3 CAP and Worker adapters are intentionally split into separate future specs/plans.

### Placeholder Scan

Placeholder scan passed. Deferred work is named as explicit follow-up plan/spec IDs.

### Type Consistency

Public names match the design document: `AddAutoRegistration`, `UseAutoRegistration`, `AddTwAspNetCoreInfrastructure`, `ITransientDependency`, `IScopedDependency`, `ISingletonDependency`, `ConfigurationSectionAttribute`, `IConfigurableOptions`, `ICancellationTokenProvider`, `ICurrentCancellationTokenAccessor`, `IInvocationContext`, `IUnaryInvocationContext`, `InterceptorBase`, `IInterceptorMatcher`, `InterceptorScope`, `InterceptAttribute`, and `IgnoreInterceptorsAttribute`.
