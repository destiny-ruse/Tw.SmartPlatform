# Dotnet Framework P0 Package Governance Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Establish the package topology, central dependency versions, charter validation, and architecture guardrails required by the final .NET microservice framework design.

**Architecture:** This phase creates enforceable governance before feature work. It keeps package names, physical folders, solution folders, central versions, package charters, and forbidden dependency rules aligned with the spec.

**Tech Stack:** .NET 10, MSBuild central package management, xUnit, FluentAssertions, PowerShell, YAML charters, `dotnet sln`/`.slnx`

---

## File Structure

- Create: `backend/dotnet/Build/Packages.Framework.props`
- Modify: `backend/dotnet/Directory.Packages.props`
- Modify: `backend/dotnet/Build/Packages.Microsoft.props`
- Modify: `backend/dotnet/Build/Packages.ThirdParty.props`
- Modify: `backend/dotnet/Build/Packages.Tests.props`
- Modify: `backend/dotnet/Tw.SmartPlatform.slnx`
- Create: `backend/dotnet/BuildingBlocks/tests/Tw.Architecture.Tests/Tw.Architecture.Tests.csproj`
- Create: `backend/dotnet/BuildingBlocks/tests/Tw.Architecture.Tests/PackageTopologyTests.cs`
- Create: `backend/dotnet/BuildingBlocks/tests/Tw.Architecture.Tests/PackageCharterTests.cs`
- Create: `backend/dotnet/BuildingBlocks/tests/Tw.Architecture.Tests/ForbiddenReferenceTests.cs`
- Modify: `docs/shared-packages/README.md`
- Modify: `docs/shared-packages/dotnet/README.md`

### Task 1: Add Architecture Test Project

**Files:**
- Create: `backend/dotnet/BuildingBlocks/tests/Tw.Architecture.Tests/Tw.Architecture.Tests.csproj`
- Create: `backend/dotnet/BuildingBlocks/tests/Tw.Architecture.Tests/PackageTopologyTests.cs`
- Modify: `backend/dotnet/Tw.SmartPlatform.slnx`

- [ ] **Step 1: Write the failing package topology test**

```csharp
using FluentAssertions;

namespace Tw.Architecture.Tests;

public sealed class PackageTopologyTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    [Fact]
    public void BuildingBlocks_RuntimeProjects_LiveUnderCapabilityFolders()
    {
        var srcRoot = Path.Combine(RepositoryRoot, "backend", "dotnet", "BuildingBlocks", "src");
        var projectFiles = Directory.GetFiles(srcRoot, "*.csproj", SearchOption.AllDirectories);

        projectFiles.Should().NotBeEmpty();
        projectFiles.Should().OnlyContain(path =>
        {
            var relative = Path.GetRelativePath(srcRoot, path).Replace('\\', '/');
            return relative.Count(ch => ch == '/') == 2;
        }, "runtime projects must use src/<Capability>/<Package>/<Package>.csproj");
    }

    [Fact]
    public void ForbiddenPackages_DoNotExist()
    {
        var forbiddenPackages = new[]
        {
            "Tw.Infrastructure",
            "Tw.Context",
            "Tw.ExecutionPipeline",
            "Tw.Swagger",
            "Tw.ApiVersioning",
            "Tw.Validation",
            "Tw.RateLimiting",
            "Tw.HealthChecks",
            "Tw.ObjectStorage",
            "Tw.Serialization",
            "Tw.Bff",
            "Tw.DynamicApi",
            "Tw.AspNetCore.DynamicApi",
            "Tw.ApplicationConfiguration",
            "Tw.Snowflake",
            "Tw.DistributedLock",
            "Tw.Autofac",
            "Tw.Localization.AspNetCore",
            "Tw.Grpc.AspNetCore",
            "Tw.Cqrs",
            "Tw.UnitOfWork",
            "Tw.Data.Abstractions",
            "Tw.Testing"
        };

        var srcRoot = Path.Combine(RepositoryRoot, "backend", "dotnet", "BuildingBlocks", "src");
        var actualPackages = Directory.GetFiles(srcRoot, "*.csproj", SearchOption.AllDirectories)
            .Select(Path.GetFileNameWithoutExtension)
            .ToHashSet(StringComparer.Ordinal);

        actualPackages.Should().NotIntersectWith(forbiddenPackages);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "AGENTS.md")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("无法定位仓库根目录");
    }
}
```

- [ ] **Step 2: Create the test project**

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
    <PackageReference Include="xunit.v3" />
    <PackageReference Include="xunit.runner.visualstudio" />
    <PackageReference Include="AwesomeAssertions" />
  </ItemGroup>
</Project>
```

- [ ] **Step 3: Run the test to verify it fails on current topology**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Architecture.Tests/Tw.Architecture.Tests.csproj --filter PackageTopologyTests`

Expected: FAIL because existing runtime projects still live directly under `BuildingBlocks/src/<Package>`.

- [ ] **Step 4: Add the project to the solution under `/BuildingBlocks/tests/`**

Modify `backend/dotnet/Tw.SmartPlatform.slnx` by adding:

```xml
<Project Path="BuildingBlocks/tests/Tw.Architecture.Tests/Tw.Architecture.Tests.csproj" />
```

- [ ] **Step 5: Commit**

```bash
git add backend/dotnet/BuildingBlocks/tests/Tw.Architecture.Tests backend/dotnet/Tw.SmartPlatform.slnx
git commit -m "test: add dotnet package topology guard"
```

### Task 2: Move Existing Projects Into Capability Folders

**Files:**
- Move: `backend/dotnet/BuildingBlocks/src/Tw.Core` to `backend/dotnet/BuildingBlocks/src/Foundation/Tw.Core`
- Move: `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection` to `backend/dotnet/BuildingBlocks/src/Foundation/Tw.DependencyInjection`
- Move: `backend/dotnet/BuildingBlocks/src/Tw.AspNetCore` to `backend/dotnet/BuildingBlocks/src/Web/Tw.AspNetCore`
- Move: `backend/dotnet/BuildingBlocks/src/Tw.AspNetCore.Mvc` to `backend/dotnet/BuildingBlocks/src/Web/Tw.AspNetCore.Mvc`
- Move: `backend/dotnet/BuildingBlocks/src/Tw.AspNetCore.Grpc` to `backend/dotnet/BuildingBlocks/src/Web/Tw.AspNetCore.Grpc`
- Move: `backend/dotnet/BuildingBlocks/src/Tw.Localization` to `backend/dotnet/BuildingBlocks/src/Localization/Tw.Localization`
- Rename: `backend/dotnet/BuildingBlocks/src/Tw.Localization.AspNetCore` to `backend/dotnet/BuildingBlocks/src/Web/Tw.AspNetCore.Localization`
- Modify: all affected `ProjectReference` entries under `backend/dotnet/BuildingBlocks/tests`
- Modify: `backend/dotnet/Tw.SmartPlatform.slnx`

- [ ] **Step 1: Move projects with PowerShell**

Run from repository root:

```powershell
New-Item -ItemType Directory -Force -Path backend/dotnet/BuildingBlocks/src/Foundation | Out-Null
New-Item -ItemType Directory -Force -Path backend/dotnet/BuildingBlocks/src/Web | Out-Null
New-Item -ItemType Directory -Force -Path backend/dotnet/BuildingBlocks/src/Localization | Out-Null
Move-Item -LiteralPath backend/dotnet/BuildingBlocks/src/Tw.Core -Destination backend/dotnet/BuildingBlocks/src/Foundation/Tw.Core
Move-Item -LiteralPath backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection -Destination backend/dotnet/BuildingBlocks/src/Foundation/Tw.DependencyInjection
Move-Item -LiteralPath backend/dotnet/BuildingBlocks/src/Tw.AspNetCore -Destination backend/dotnet/BuildingBlocks/src/Web/Tw.AspNetCore
Move-Item -LiteralPath backend/dotnet/BuildingBlocks/src/Tw.AspNetCore.Mvc -Destination backend/dotnet/BuildingBlocks/src/Web/Tw.AspNetCore.Mvc
Move-Item -LiteralPath backend/dotnet/BuildingBlocks/src/Tw.AspNetCore.Grpc -Destination backend/dotnet/BuildingBlocks/src/Web/Tw.AspNetCore.Grpc
Move-Item -LiteralPath backend/dotnet/BuildingBlocks/src/Tw.Localization -Destination backend/dotnet/BuildingBlocks/src/Localization/Tw.Localization
Move-Item -LiteralPath backend/dotnet/BuildingBlocks/src/Tw.Localization.AspNetCore -Destination backend/dotnet/BuildingBlocks/src/Web/Tw.AspNetCore.Localization
```

- [ ] **Step 2: Rename the localization ASP.NET Core project file**

Run:

```powershell
Rename-Item -LiteralPath backend/dotnet/BuildingBlocks/src/Web/Tw.AspNetCore.Localization/Tw.Localization.AspNetCore.csproj -NewName Tw.AspNetCore.Localization.csproj
```

- [ ] **Step 3: Update package charter package names**

Change `backend/dotnet/BuildingBlocks/src/Web/Tw.AspNetCore.Localization/package-charter.yaml`:

```yaml
package: Tw.AspNetCore.Localization
public_capabilities:
  - Tw.AspNetCore.Localization
```

- [ ] **Step 4: Update affected project references**

Replace references using these exact path patterns:

```text
..\..\src\Tw.Core\Tw.Core.csproj -> ..\..\src\Foundation\Tw.Core\Tw.Core.csproj
..\..\src\Tw.DependencyInjection\Tw.DependencyInjection.csproj -> ..\..\src\Foundation\Tw.DependencyInjection\Tw.DependencyInjection.csproj
..\..\src\Tw.Localization\Tw.Localization.csproj -> ..\..\src\Localization\Tw.Localization\Tw.Localization.csproj
..\..\src\Tw.Localization.AspNetCore\Tw.Localization.AspNetCore.csproj -> ..\..\src\Web\Tw.AspNetCore.Localization\Tw.AspNetCore.Localization.csproj
..\..\src\Tw.AspNetCore\Tw.AspNetCore.csproj -> ..\..\src\Web\Tw.AspNetCore\Tw.AspNetCore.csproj
..\..\src\Tw.AspNetCore.Mvc\Tw.AspNetCore.Mvc.csproj -> ..\..\src\Web\Tw.AspNetCore.Mvc\Tw.AspNetCore.Mvc.csproj
..\..\src\Tw.AspNetCore.Grpc\Tw.AspNetCore.Grpc.csproj -> ..\..\src\Web\Tw.AspNetCore.Grpc\Tw.AspNetCore.Grpc.csproj
```

- [ ] **Step 5: Run architecture test**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Architecture.Tests/Tw.Architecture.Tests.csproj --filter PackageTopologyTests`

Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add backend/dotnet/BuildingBlocks backend/dotnet/Tw.SmartPlatform.slnx
git commit -m "refactor: align building block package topology"
```

### Task 3: Centralize Package Versions From The Final Design

**Files:**
- Create: `backend/dotnet/Build/Packages.Framework.props`
- Modify: `backend/dotnet/Directory.Packages.props`
- Modify: `backend/dotnet/Build/Packages.ThirdParty.props`
- Modify: `backend/dotnet/Build/Packages.Tests.props`

- [ ] **Step 1: Create the framework package version file**

```xml
<!-- 用途: 管理 Tw 微服务框架默认依赖版本 -->
<Project>
  <ItemGroup>
    <PackageVersion Include="DotNetCore.CAP" Version="10.0.1" />
    <PackageVersion Include="DotNetCore.CAP.RabbitMQ" Version="10.0.1" />
    <PackageVersion Include="SqlSugarCore" Version="5.1.4.216" />
    <PackageVersion Include="Yitter.IdGenerator" Version="1.0.15" />
    <PackageVersion Include="Newtonsoft.Json" Version="13.0.4" />
    <PackageVersion Include="Microsoft.AspNetCore.Mvc.NewtonsoftJson" Version="10.0.9" />
    <PackageVersion Include="Swashbuckle.AspNetCore" Version="10.2.3" />
    <PackageVersion Include="Swashbuckle.AspNetCore.Newtonsoft" Version="10.2.3" />
    <PackageVersion Include="Asp.Versioning.Mvc" Version="10.0.0" />
    <PackageVersion Include="Asp.Versioning.Mvc.ApiExplorer" Version="10.0.0" />
    <PackageVersion Include="Scriban" Version="7.2.5" />
    <PackageVersion Include="MiniExcel" Version="1.45.0" />
    <PackageVersion Include="DocumentFormat.OpenXml" Version="3.5.1" />
    <PackageVersion Include="ZiggyCreatures.FusionCache" Version="2.6.0" />
    <PackageVersion Include="StackExchange.Redis" Version="3.0.11" />
    <PackageVersion Include="DistributedLock.Redis" Version="1.1.1" />
    <PackageVersion Include="Polly" Version="8.7.0" />
    <PackageVersion Include="Microsoft.Extensions.Http.Resilience" Version="10.7.0" />
    <PackageVersion Include="Microsoft.Extensions.ServiceDiscovery" Version="10.7.0" />
    <PackageVersion Include="Microsoft.Extensions.ServiceDiscovery.Yarp" Version="10.7.0" />
    <PackageVersion Include="Yarp.ReverseProxy" Version="2.3.0" />
    <PackageVersion Include="MediatR" Version="12.5.0" />
    <PackageVersion Include="FluentValidation" Version="12.1.1" />
    <PackageVersion Include="OpenIddict" Version="7.5.0" />
    <PackageVersion Include="Quartz" Version="3.18.2" />
    <PackageVersion Include="Serilog.AspNetCore" Version="10.0.0" />
    <PackageVersion Include="Serilog.Sinks.OpenTelemetry" Version="4.2.0" />
    <PackageVersion Include="OpenTelemetry.Extensions.Hosting" Version="1.16.0" />
    <PackageVersion Include="OpenTelemetry.Exporter.OpenTelemetryProtocol" Version="1.16.0" />
    <PackageVersion Include="OpenTelemetry.Instrumentation.AspNetCore" Version="1.16.0" />
    <PackageVersion Include="OpenTelemetry.Instrumentation.Http" Version="1.16.0" />
    <PackageVersion Include="nacos-sdk-csharp" Version="1.3.10" />
    <PackageVersion Include="nacos-sdk-csharp.Extensions.Configuration" Version="1.3.10" />
    <PackageVersion Include="nacos-sdk-csharp.Extensions.ServiceDiscovery" Version="1.3.10" />
    <PackageVersion Include="NSwag.MSBuild" Version="14.7.1" />
  </ItemGroup>
</Project>
```

- [ ] **Step 2: Import the framework package file**

Add this import to `backend/dotnet/Directory.Packages.props` after `Packages.ThirdParty.props`:

```xml
<Import Project="Build/Packages.Framework.props" />
```

- [ ] **Step 3: Update test packages**

Change `backend/dotnet/Build/Packages.Tests.props` to use:

```xml
<PackageVersion Include="xunit.v3" Version="3.2.2" />
<PackageVersion Include="xunit.runner.visualstudio" Version="3.1.5" />
<PackageVersion Include="Microsoft.NET.Test.Sdk" Version="18.7.0" />
<PackageVersion Include="AwesomeAssertions" Version="9.4.0" />
<PackageVersion Include="NSubstitute" Version="5.3.0" />
<PackageVersion Include="coverlet.collector" Version="10.0.1" />
<PackageVersion Include="Testcontainers" Version="4.13.0" />
<PackageVersion Include="Microsoft.AspNetCore.Mvc.Testing" Version="10.0.9" />
<PackageVersion Include="Aspire.Hosting.Testing" Version="13.4.6" />
<PackageVersion Include="WireMock.Net" Version="2.12.0" />
<PackageVersion Include="Respawn" Version="7.0.0" />
<PackageVersion Include="ReportGenerator" Version="5.5.10" />
<PackageVersion Include="dotnet-stryker" Version="4.16.0" />
```

- [ ] **Step 4: Update existing test project references**

Replace `FluentAssertions` package references with:

```xml
<PackageReference Include="AwesomeAssertions" />
```

Replace `xunit` package references with:

```xml
<PackageReference Include="xunit.v3" />
```

- [ ] **Step 5: Restore packages**

Run: `dotnet restore backend/dotnet/Tw.SmartPlatform.slnx`

Expected: restore succeeds and lock files update.

- [ ] **Step 6: Commit**

```bash
git add backend/dotnet/Directory.Packages.props backend/dotnet/Build backend/dotnet/BuildingBlocks
git commit -m "build: centralize framework package versions"
```

### Task 4: Validate Package Charters

**Files:**
- Create: `backend/dotnet/BuildingBlocks/tests/Tw.Architecture.Tests/PackageCharterTests.cs`
- Modify: package charters under `backend/dotnet/BuildingBlocks/src/**/package-charter.yaml`

- [ ] **Step 1: Write charter validation tests**

```csharp
using FluentAssertions;

namespace Tw.Architecture.Tests;

public sealed class PackageCharterTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    [Fact]
    public void EveryRuntimeProject_HasPackageCharterWithCanonicalPackageName()
    {
        var srcRoot = Path.Combine(RepositoryRoot, "backend", "dotnet", "BuildingBlocks", "src");
        var projects = Directory.GetFiles(srcRoot, "*.csproj", SearchOption.AllDirectories);

        foreach (var project in projects)
        {
            var projectName = Path.GetFileNameWithoutExtension(project);
            var charter = Path.Combine(Path.GetDirectoryName(project)!, "package-charter.yaml");
            File.Exists(charter).Should().BeTrue($"{projectName} must declare package-charter.yaml");

            var text = File.ReadAllText(charter);
            text.Should().Contain($"package: {projectName}");
            text.Should().Contain("out_of_scope:");
            text.Should().Contain("public_capabilities:");
            text.Should().Contain("dependency_rules:");
        }
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "AGENTS.md")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("无法定位仓库根目录");
    }
}
```

- [ ] **Step 2: Run the test to reveal invalid charters**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Architecture.Tests/Tw.Architecture.Tests.csproj --filter PackageCharterTests`

Expected: FAIL until moved and renamed packages have updated charters.

- [ ] **Step 3: Fix all current charters**

Use this pattern for each charter:

```yaml
schema_version: "1.0.0"
package: Tw.AspNetCore.Localization
owner: platform-team
stability: experimental
compatibility: "experimental 阶段不承诺兼容"
responsibility: >
  ASP.NET Core 请求文化解析、本地化中间件和 MVC 本地化适配。
in_scope:
  - HTTP 请求文化解析
  - ASP.NET Core 本地化中间件
  - MVC 本地化适配
out_of_scope:
  - JSON 本地化资源解析
  - 实体翻译存储
  - 动态文本存储
public_capabilities:
  - Tw.AspNetCore.Localization
dependency_rules:
  forbid:
    - "SqlSugar*"
    - "DotNetCore.CAP*"
  allow:
    - "Tw.Localization"
    - "Microsoft.AspNetCore.*"
    - "Microsoft.Extensions.*"
```

- [ ] **Step 4: Run the charter test**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Architecture.Tests/Tw.Architecture.Tests.csproj --filter PackageCharterTests`

Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add backend/dotnet/BuildingBlocks/src backend/dotnet/BuildingBlocks/tests/Tw.Architecture.Tests
git commit -m "test: enforce building block package charters"
```

### Task 5: Validate Forbidden References

**Files:**
- Create: `backend/dotnet/BuildingBlocks/tests/Tw.Architecture.Tests/ForbiddenReferenceTests.cs`

- [ ] **Step 1: Write forbidden reference tests**

```csharp
using System.Xml.Linq;
using FluentAssertions;

namespace Tw.Architecture.Tests;

public sealed class ForbiddenReferenceTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    [Fact]
    public void RuntimeProjects_DoNotReferenceTestingPackages()
    {
        var srcRoot = Path.Combine(RepositoryRoot, "backend", "dotnet", "BuildingBlocks", "src");
        var projects = Directory.GetFiles(srcRoot, "*.csproj", SearchOption.AllDirectories);
        var forbidden = new[] { "Tw.TestBase", "Tw.AspNetCore.TestBase", "Tw.Data.SqlSugar.TestBase", "Tw.EventBus.Cap.TestBase" };

        foreach (var project in projects)
        {
            var document = XDocument.Load(project);
            var references = document.Descendants("ProjectReference")
                .Select(element => element.Attribute("Include")?.Value ?? string.Empty)
                .Concat(document.Descendants("PackageReference").Select(element => element.Attribute("Include")?.Value ?? string.Empty));

            references.Should().NotContain(reference => forbidden.Any(reference.Contains), $"{Path.GetFileName(project)} is a runtime project");
        }
    }

    [Fact]
    public void GatewayYarp_DoesNotReferenceApplicationDataOrEventBusPackages()
    {
        var project = Path.Combine(RepositoryRoot, "backend", "dotnet", "BuildingBlocks", "src", "Gateway", "Tw.Gateway.Yarp", "Tw.Gateway.Yarp.csproj");
        if (!File.Exists(project))
        {
            return;
        }

        var forbidden = new[] { "Tw.Data", "Tw.Uow", "Tw.Application", "Tw.EventBus", "Tw.BackgroundJobs", "Tw.MultiTenancy", "Tw.Sharding" };
        var text = File.ReadAllText(project);

        text.Should().NotContainAny(forbidden);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "AGENTS.md")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("无法定位仓库根目录");
    }
}
```

- [ ] **Step 2: Run architecture tests**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Architecture.Tests/Tw.Architecture.Tests.csproj`

Expected: PASS.

- [ ] **Step 3: Commit**

```bash
git add backend/dotnet/BuildingBlocks/tests/Tw.Architecture.Tests
git commit -m "test: enforce building block dependency boundaries"
```

### Task 6: Update Shared Package Documentation Indexes

**Files:**
- Modify: `docs/shared-packages/README.md`
- Modify: `docs/shared-packages/dotnet/README.md`

- [ ] **Step 1: Update root shared package index**

Ensure `docs/shared-packages/README.md` contains this section:

```markdown
## .NET Building Blocks

- [Tw.Core](dotnet/Tw.Core/README.md)
- [Tw.DependencyInjection](dotnet/Tw.DependencyInjection/README.md)
- [Tw.AspNetCore](dotnet/Tw.AspNetCore/README.md)
- [Tw.AspNetCore.Mvc](dotnet/Tw.AspNetCore.Mvc/README.md)
- [Tw.AspNetCore.Grpc](dotnet/Tw.AspNetCore.Grpc/README.md)
- [Tw.AspNetCore.Localization](dotnet/Tw.AspNetCore.Localization/README.md)
- [Tw.Localization](dotnet/Tw.Localization/README.md)
```

- [ ] **Step 2: Update dotnet shared package index**

Ensure `docs/shared-packages/dotnet/README.md` contains this section:

```markdown
## Current Packages

- [Tw.Core](Tw.Core/README.md)
- [Tw.DependencyInjection](Tw.DependencyInjection/README.md)
- [Tw.AspNetCore](Tw.AspNetCore/README.md)
- [Tw.AspNetCore.Mvc](Tw.AspNetCore.Mvc/README.md)
- [Tw.AspNetCore.Grpc](Tw.AspNetCore.Grpc/README.md)
- [Tw.AspNetCore.Localization](Tw.AspNetCore.Localization/README.md)
- [Tw.Localization](Tw.Localization/README.md)
```

- [ ] **Step 3: Rename localization ASP.NET Core docs folder**

Run:

```powershell
Move-Item -LiteralPath docs/shared-packages/dotnet/Tw.Localization.AspNetCore -Destination docs/shared-packages/dotnet/Tw.AspNetCore.Localization
```

- [ ] **Step 4: Run documentation link smoke check**

Run:

```powershell
Test-Path docs/shared-packages/dotnet/Tw.AspNetCore.Localization/README.md
```

Expected: `True`.

- [ ] **Step 5: Commit**

```bash
git add docs/shared-packages
git commit -m "docs: align shared package indexes with package topology"
```

## Plan Self-Review

- Spec coverage: package topology, forbidden package names, central versions, shared package charter, shared docs index, and test-only package boundaries are covered.
- Placeholder scan: no placeholder tokens are present.
- Type consistency: test class names, paths, and package names use the final design package names.
