# Dotnet Framework P2 DI AOP Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Split the current dependency injection and dynamic proxy implementation into abstraction, container-neutral, Autofac, and Castle packages.

**Architecture:** `Tw.DependencyInjection.Abstractions` owns registration metadata and options-binding contracts. `Tw.DependencyInjection` owns assembly discovery, service planning, and container-neutral registration. `Tw.DependencyInjection.Autofac` owns Autofac integration. `Tw.Castle.Core` owns DynamicProxy adapters and interceptor pipeline execution.

**Tech Stack:** .NET 10, Microsoft.Extensions.DependencyInjection.Abstractions, Microsoft.Extensions.Options, Autofac, Autofac.Extensions.DependencyInjection, Castle.Core, xUnit, AwesomeAssertions

---

## File Structure

- Create: `backend/dotnet/BuildingBlocks/src/Foundation/Tw.DependencyInjection.Abstractions`
- Modify: `backend/dotnet/BuildingBlocks/src/Foundation/Tw.DependencyInjection`
- Create: `backend/dotnet/BuildingBlocks/src/Foundation/Tw.DependencyInjection.Autofac`
- Create: `backend/dotnet/BuildingBlocks/src/Foundation/Tw.Castle.Core`
- Modify: `backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests`
- Create: `backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Abstractions.Tests`
- Create: `backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Autofac.Tests`
- Create: `backend/dotnet/BuildingBlocks/tests/Tw.Castle.Core.Tests`
- Modify: `docs/shared-packages/dotnet/Tw.DependencyInjection/README.md`
- Create: `docs/shared-packages/dotnet/Tw.DependencyInjection.Abstractions/README.md`
- Create: `docs/shared-packages/dotnet/Tw.DependencyInjection.Autofac/README.md`
- Create: `docs/shared-packages/dotnet/Tw.Castle.Core/README.md`

### Task 1: Move DI Metadata To Abstractions Package

**Files:**
- Move: `Tw.Core/DependencyInjection/Abstractions/*` to `Tw.DependencyInjection.Abstractions`
- Move: `Tw.Core/Configuration/Abstractions/*` to `Tw.DependencyInjection.Abstractions/Configuration`
- Create: `backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Abstractions.Tests/DependencyLifetimeTests.cs`

- [ ] **Step 1: Write abstraction shape test**

```csharp
using AwesomeAssertions;
using Tw.DependencyInjection.Abstractions;

namespace Tw.DependencyInjection.Abstractions.Tests;

public sealed class DependencyLifetimeTests
{
    [Fact]
    public void DependencyLifetime_ContainsExpectedValues()
    {
        Enum.GetNames<DependencyLifetime>()
            .Should()
            .BeEquivalentTo("Transient", "Scoped", "Singleton");
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Abstractions.Tests/Tw.DependencyInjection.Abstractions.Tests.csproj`

Expected: FAIL before moved types compile in the new package.

- [ ] **Step 3: Move types and update namespaces**

Use this namespace for moved DI metadata:

```csharp
namespace Tw.DependencyInjection.Abstractions;
```

Use this namespace for moved options metadata:

```csharp
namespace Tw.DependencyInjection.Abstractions.Configuration;
```

- [ ] **Step 4: Update project references**

Add to `Tw.DependencyInjection.csproj`:

```xml
<ProjectReference Include="..\Tw.DependencyInjection.Abstractions\Tw.DependencyInjection.Abstractions.csproj" />
```

- [ ] **Step 5: Run tests**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Abstractions.Tests/Tw.DependencyInjection.Abstractions.Tests.csproj`

Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add backend/dotnet/BuildingBlocks/src/Foundation/Tw.Core backend/dotnet/BuildingBlocks/src/Foundation/Tw.DependencyInjection.Abstractions backend/dotnet/BuildingBlocks/src/Foundation/Tw.DependencyInjection backend/dotnet/BuildingBlocks/tests
git commit -m "refactor: split dependency injection abstractions"
```

### Task 2: Keep Service Registration Planning Container-Neutral

**Files:**
- Modify: `backend/dotnet/BuildingBlocks/src/Foundation/Tw.DependencyInjection/Registration/*`
- Modify: `backend/dotnet/BuildingBlocks/src/Foundation/Tw.DependencyInjection/Discovery/*`
- Create: `backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests/Registration/ContainerNeutralRegistrationTests.cs`

- [ ] **Step 1: Write test that planning does not require Autofac**

```csharp
using AwesomeAssertions;
using Tw.DependencyInjection.Registration;

namespace Tw.DependencyInjection.Tests.Registration;

public sealed class ContainerNeutralRegistrationTests
{
    [Fact]
    public void ServiceRegistrationPlan_DoesNotExposeAutofacTypes()
    {
        typeof(ServiceRegistrationPlan).Assembly
            .GetReferencedAssemblies()
            .Select(name => name.Name)
            .Should()
            .NotContain("Autofac");
    }
}
```

- [ ] **Step 2: Run test to verify current coupling**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests/Tw.DependencyInjection.Tests.csproj --filter ContainerNeutralRegistrationTests`

Expected: FAIL while `Tw.DependencyInjection` still references Autofac packages.

- [ ] **Step 3: Remove Autofac package references from `Tw.DependencyInjection.csproj`**

The project keeps only:

```xml
<PackageReference Include="Microsoft.Extensions.Configuration.Binder" />
<PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" />
<PackageReference Include="Microsoft.Extensions.Hosting.Abstractions" />
<PackageReference Include="Microsoft.Extensions.DependencyModel" />
<PackageReference Include="Microsoft.Extensions.Options.ConfigurationExtensions" />
<ProjectReference Include="..\Tw.DependencyInjection.Abstractions\Tw.DependencyInjection.Abstractions.csproj" />
```

- [ ] **Step 4: Move Autofac-specific executor files**

Move `AutofacServiceRegistrationExecutor.cs`, `AutofacHostBuilderExtensions.cs`, and `AutofacServiceRegistrationExtensions.cs` to `Tw.DependencyInjection.Autofac`.

- [ ] **Step 5: Run container-neutral tests**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests/Tw.DependencyInjection.Tests.csproj`

Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add backend/dotnet/BuildingBlocks/src/Foundation/Tw.DependencyInjection backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests
git commit -m "refactor: keep dependency injection planning container neutral"
```

### Task 3: Create Autofac Integration Package

**Files:**
- Create: `backend/dotnet/BuildingBlocks/src/Foundation/Tw.DependencyInjection.Autofac/Tw.DependencyInjection.Autofac.csproj`
- Move: Autofac integration files from `Tw.DependencyInjection`
- Create: `backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Autofac.Tests/AutofacHostBuilderExtensionsTests.cs`

- [ ] **Step 1: Write Autofac integration test**

```csharp
using Autofac.Extensions.DependencyInjection;
using AwesomeAssertions;
using Microsoft.Extensions.Hosting;
using Tw.DependencyInjection.Autofac;

namespace Tw.DependencyInjection.Autofac.Tests;

public sealed class AutofacHostBuilderExtensionsTests
{
    [Fact]
    public void UseAutofac_ConfiguresAutofacServiceProviderFactory()
    {
        var builder = Host.CreateDefaultBuilder();

        var result = builder.UseAutofac();

        result.Should().BeSameAs(builder);
    }
}
```

- [ ] **Step 2: Create package project**

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>true</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Autofac" />
    <PackageReference Include="Autofac.Extensions.DependencyInjection" />
    <PackageReference Include="Microsoft.Extensions.Hosting.Abstractions" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\Tw.DependencyInjection\Tw.DependencyInjection.csproj" />
  </ItemGroup>
</Project>
```

- [ ] **Step 3: Rename extension method**

The public extension method must be:

```csharp
public static IHostBuilder UseAutofac(this IHostBuilder builder)
```

It must not expose `UseTwHostStartup` or `AddTw...` names.

- [ ] **Step 4: Run tests**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Autofac.Tests/Tw.DependencyInjection.Autofac.Tests.csproj`

Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add backend/dotnet/BuildingBlocks/src/Foundation/Tw.DependencyInjection.Autofac backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Autofac.Tests
git commit -m "feat: add autofac dependency injection adapter"
```

### Task 4: Create Castle Core AOP Package

**Files:**
- Move: `Tw.Core/DynamicProxy/Abstractions/*` to `Tw.Castle.Core/Abstractions`
- Move: `Tw.DependencyInjection/DynamicProxy/*` to `Tw.Castle.Core`
- Create: `backend/dotnet/BuildingBlocks/tests/Tw.Castle.Core.Tests/InterceptorPipelineTests.cs`

- [ ] **Step 1: Write interception pipeline test**

```csharp
using System.Reflection;
using AwesomeAssertions;
using Tw.Castle.Core;
using Tw.Castle.Core.Abstractions;

namespace Tw.Castle.Core.Tests;

public sealed class InterceptorPipelineTests
{
    [Fact]
    public async Task InvokeAsync_ExecutesInterceptorsInOrder()
    {
        var calls = new List<string>();
        var pipeline = new InterceptorPipeline();
        var interceptors = new IInterceptor[]
        {
            new RecordingInterceptor("one", calls),
            new RecordingInterceptor("two", calls)
        };

        await pipeline.InvokeAsync(new EmptyInvocationContext(calls), interceptors);

        calls.Should().Equal("one-before", "two-before", "target", "two-after", "one-after");
    }

    private sealed class RecordingInterceptor(string name, List<string> calls) : IInterceptor
    {
        public async ValueTask InterceptAsync(IInvocationContext context)
        {
            calls.Add($"{name}-before");
            await context.ProceedAsync();
            calls.Add($"{name}-after");
        }
    }

    private sealed class EmptyInvocationContext(List<string> calls) : IInvocationContext
    {
        public MethodInfo Method => typeof(object).GetMethod(nameof(ToString))!;
        public object? Target => null;
        public object?[] Arguments { get; } = Array.Empty<object?>();
        public IReadOnlyDictionary<string, object?> ArgumentsByName { get; } =
            new Dictionary<string, object?>();
        public object? ReturnValue { get; set; }

        public ValueTask ProceedAsync()
        {
            calls.Add("target");
            return ValueTask.CompletedTask;
        }

        public void Proceed()
        {
            calls.Add("target");
        }
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Castle.Core.Tests/Tw.Castle.Core.Tests.csproj --filter InterceptorPipelineTests`

Expected: FAIL before AOP types move to `Tw.Castle.Core`.

- [ ] **Step 3: Move and rename namespaces**

Use these namespaces:

```csharp
namespace Tw.Castle.Core;
namespace Tw.Castle.Core.Abstractions;
```

- [ ] **Step 4: Add package references**

`Tw.Castle.Core.csproj` includes:

```xml
<PackageReference Include="Castle.Core" />
<ProjectReference Include="..\Tw.DependencyInjection.Abstractions\Tw.DependencyInjection.Abstractions.csproj" />
```

- [ ] **Step 5: Run tests**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Castle.Core.Tests/Tw.Castle.Core.Tests.csproj`

Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add backend/dotnet/BuildingBlocks/src/Foundation/Tw.Castle.Core backend/dotnet/BuildingBlocks/tests/Tw.Castle.Core.Tests
git commit -m "refactor: move dynamic proxy support to castle package"
```

### Task 5: Update ASP.NET Core Host Startup To Use Split Packages

**Files:**
- Modify: `backend/dotnet/BuildingBlocks/src/Web/Tw.AspNetCore/DependencyInjection/HostStartupBuilderExtensions.cs`
- Modify: `backend/dotnet/BuildingBlocks/tests/Tw.AspNetCore.Tests/DependencyInjection/HostStartupBuilderExtensionsTests.cs`

- [ ] **Step 1: Write host startup test for new entry name**

```csharp
using AwesomeAssertions;
using Microsoft.AspNetCore.Builder;
using Tw.AspNetCore;

namespace Tw.AspNetCore.Tests.DependencyInjection;

public sealed class HostStartupBuilderExtensionsTests
{
    [Fact]
    public void UseWebIntegration_ReturnsSameBuilder()
    {
        var builder = WebApplication.CreateBuilder();

        var result = builder.UseWebIntegration();

        result.Should().BeSameAs(builder);
    }
}
```

- [ ] **Step 2: Rename public entry**

Replace `UseTwHostStartup` with:

```csharp
public static WebApplicationBuilder UseWebIntegration(this WebApplicationBuilder builder)
```

- [ ] **Step 3: Remove direct Autofac/Castle coupling**

`Tw.AspNetCore.csproj` references `Tw.DependencyInjection.Autofac` and `Tw.Castle.Core` only if the host integration truly configures those runtime adapters. It does not reference implementation packages from `Tw.Application`, `Tw.Data`, or `Tw.EventBus`.

- [ ] **Step 4: Run ASP.NET Core tests**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.AspNetCore.Tests/Tw.AspNetCore.Tests.csproj`

Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add backend/dotnet/BuildingBlocks/src/Web/Tw.AspNetCore backend/dotnet/BuildingBlocks/tests/Tw.AspNetCore.Tests
git commit -m "refactor: align aspnetcore host startup with split di packages"
```

### Task 6: Update Charters And Documentation For Split DI Packages

**Files:**
- Create: `backend/dotnet/BuildingBlocks/src/Foundation/Tw.DependencyInjection.Abstractions/package-charter.yaml`
- Modify: `backend/dotnet/BuildingBlocks/src/Foundation/Tw.DependencyInjection/package-charter.yaml`
- Create: `backend/dotnet/BuildingBlocks/src/Foundation/Tw.DependencyInjection.Autofac/package-charter.yaml`
- Create: `backend/dotnet/BuildingBlocks/src/Foundation/Tw.Castle.Core/package-charter.yaml`
- Create: `docs/shared-packages/dotnet/Tw.DependencyInjection.Abstractions/README.md`
- Modify: `docs/shared-packages/dotnet/Tw.DependencyInjection/README.md`
- Create: `docs/shared-packages/dotnet/Tw.DependencyInjection.Autofac/README.md`
- Create: `docs/shared-packages/dotnet/Tw.Castle.Core/README.md`

- [ ] **Step 1: Update package charters**

Each charter must include `schema_version`, `package`, `owner`, `responsibility`, non-empty `in_scope`, non-empty `out_of_scope`, non-empty `public_capabilities`, and `dependency_rules`.

`Tw.DependencyInjection.Abstractions/package-charter.yaml`:

```yaml
schema_version: "1.0.0"
package: Tw.DependencyInjection.Abstractions
owner: platform-team
stability: experimental
compatibility: "experimental 阶段不承诺兼容"
responsibility: >
  服务生命周期、自动注册、Options 绑定、拦截元数据和服务暴露抽象。
in_scope:
  - 服务生命周期标记
  - 服务暴露元数据
  - Options 绑定元数据
  - 拦截元数据
out_of_scope:
  - 程序集扫描执行
  - Autofac 容器接管
  - Castle DynamicProxy 代理创建
public_capabilities:
  - Tw.DependencyInjection.Abstractions
dependency_rules:
  forbid:
    - "Autofac*"
    - "Castle*"
    - "Microsoft.AspNetCore.*"
```

`Tw.DependencyInjection.Autofac/package-charter.yaml` must allow `Autofac`, `Autofac.Extensions.DependencyInjection`, and `Autofac.Extras.DynamicProxy`, and forbid ASP.NET Core MVC, CAP, SqlSugar, Quartz, and YARP. `Tw.Castle.Core/package-charter.yaml` must allow `Castle.Core` and forbid Autofac host integration, MVC filters, CAP filters, and gRPC interceptors.

- [ ] **Step 2: Document abstraction package**

```markdown
# Tw.DependencyInjection.Abstractions

Reference documentation for dependency injection metadata, lifecycle markers, service exposure attributes, keyed service metadata, options binding attributes, and interception metadata.

## Capabilities

- `DependencyLifetime`
- `ITransientDependency`
- `IScopedDependency`
- `ISingletonDependency`
- `ExposeServicesAttribute`
- `ExposeKeyedServiceAttribute`
- `ServiceRegistrationAttribute`
- `OptionsSectionAttribute`
- `OptionsNameAttribute`
```

- [ ] **Step 3: Document runtime package**

`docs/shared-packages/dotnet/Tw.DependencyInjection/README.md` must state that the package is container-neutral and does not depend on Autofac or Castle.

- [ ] **Step 4: Document Autofac adapter**

`docs/shared-packages/dotnet/Tw.DependencyInjection.Autofac/README.md` must start with `# Tw.DependencyInjection.Autofac`, describe it as the Autofac runtime container adapter, and include this registration snippet:

```csharp
builder.Host.UseAutofac();
```

- [ ] **Step 5: Document Castle package**

`docs/shared-packages/dotnet/Tw.Castle.Core/README.md` must list interceptor pipeline, attribute selector, and Castle adapter responsibilities.

- [ ] **Step 6: Commit**

```bash
git add backend/dotnet/BuildingBlocks/src/Foundation/Tw.DependencyInjection.Abstractions backend/dotnet/BuildingBlocks/src/Foundation/Tw.DependencyInjection backend/dotnet/BuildingBlocks/src/Foundation/Tw.DependencyInjection.Autofac backend/dotnet/BuildingBlocks/src/Foundation/Tw.Castle.Core docs/shared-packages/dotnet
git commit -m "docs: document split dependency injection packages"
```

## Plan Self-Review

- Spec coverage: DI abstractions, container-neutral registration, Autofac runtime, Castle DynamicProxy, service registration naming, and docs are covered.
- Placeholder scan: no placeholder tokens are present.
- Type consistency: package and namespace names match the final design.
