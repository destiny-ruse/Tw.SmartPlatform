# Dotnet Framework P8 Tooling CI Governance Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement templates, CLI, analyzers, build gates, contract governance, package release checks, and deployment artifact validation for the final `Tw.*` framework design.

**Architecture:** .NET-specific tools live under `backend/dotnet/tools`. Repository-root `tools` remains reserved for language-agnostic repository tooling. Templates generate compliant service, gateway, BuildingBlock, and contract package skeletons. CLI commands call the same validation services used by CI. Roslyn analyzers enforce package names, dependency boundaries, and forbidden API patterns at compile time. NUKE orchestrates build, tests, coverage, mutation, SBOM, image scan, signing, Helm validation, and Argo CD asset checks.

**Tech Stack:** .NET 10, Microsoft.TemplateEngine, System.CommandLine, Roslyn Analyzer SDK, NUKE Build, GitVersion, xUnit v3, coverlet, ReportGenerator, Stryker.NET, CycloneDX, Trivy, Cosign, Helm

---

## File Structure

- Create: `backend/dotnet/tools/Tw.Templates`
- Create: `backend/dotnet/tools/Tw.Cli`
- Create: `backend/dotnet/tools/Tw.Analyzers`
- Create: `backend/dotnet/tools/Tw.Analyzers.Tests`
- Create: `backend/dotnet/Build/QualityGates`
- Modify: `backend/dotnet/Build/Build.cs`
- Modify: `backend/dotnet/Directory.Packages.props`
- Modify: `backend/dotnet/Tw.SmartPlatform.slnx`
- Create: `docs/shared-packages/dotnet/Tw.Templates/README.md`
- Create: `docs/shared-packages/dotnet/Tw.Cli/README.md`
- Create: `docs/shared-packages/dotnet/Tw.Analyzers/README.md`

### Task 1: Create Tool Package Shells

**Files:**
- Create: `backend/dotnet/tools/Tw.Templates/Tw.Templates.csproj`
- Create: `backend/dotnet/tools/Tw.Cli/Tw.Cli.csproj`
- Create: `backend/dotnet/tools/Tw.Analyzers/Tw.Analyzers.csproj`
- Create: `backend/dotnet/tools/Tw.Analyzers.Tests/Tw.Analyzers.Tests.csproj`
- Create: `backend/dotnet/tools/Tw.Templates/package-charter.yaml`
- Create: `backend/dotnet/tools/Tw.Cli/package-charter.yaml`
- Create: `backend/dotnet/tools/Tw.Analyzers/package-charter.yaml`
- Modify: `backend/dotnet/Tw.SmartPlatform.slnx`

- [ ] **Step 1: Add central tool dependencies**

Add or verify these central versions:

```xml
<PackageVersion Include="System.CommandLine" Version="2.0.9" />
<PackageVersion Include="Spectre.Console" Version="0.57.2" />
<PackageVersion Include="Microsoft.CodeAnalysis.CSharp" Version="5.6.0" />
<PackageVersion Include="Microsoft.CodeAnalysis.CSharp.Workspaces" Version="5.6.0" />
<PackageVersion Include="Microsoft.CodeAnalysis.Analyzers" Version="4.14.0" />
<PackageVersion Include="Microsoft.CodeAnalysis.CSharp.Analyzer.Testing.XUnit" Version="1.1.2" />
<PackageVersion Include="GitVersion.MsBuild" Version="6.4.0" />
```

- [ ] **Step 2: Create template package project**

`Tw.Templates.csproj` uses `Microsoft.NET.Sdk`, `net10.0`, `IsPackable=true`, `PackageType=Template`, and includes template content under `content/**`.

- [ ] **Step 3: Create CLI tool project**

`Tw.Cli.csproj` uses `Microsoft.NET.Sdk`, `net10.0`, `OutputType=Exe`, `PackAsTool=true`, `ToolCommandName=tw`, `IsPackable=true`, and references `System.CommandLine` plus `Spectre.Console`.

- [ ] **Step 4: Create analyzer project**

`Tw.Analyzers.csproj` uses `Microsoft.NET.Sdk`, `netstandard2.0`, `IsPackable=true`, `IncludeBuildOutput=false`, and packs analyzer DLLs under `analyzers/dotnet/cs`.

- [ ] **Step 5: Add charters**

`Tw.Templates` charter covers official `dotnet new` templates. `Tw.Cli` charter covers project creation, capability add, contract validation, dependency audit, and diagnostics. `Tw.Analyzers` charter covers compile-time governance rules and has no runtime API.

- [ ] **Step 6: Register projects**

Run:

```powershell
dotnet sln backend/dotnet/Tw.SmartPlatform.slnx add backend/dotnet/tools/Tw.Templates/Tw.Templates.csproj
dotnet sln backend/dotnet/Tw.SmartPlatform.slnx add backend/dotnet/tools/Tw.Cli/Tw.Cli.csproj
dotnet sln backend/dotnet/Tw.SmartPlatform.slnx add backend/dotnet/tools/Tw.Analyzers/Tw.Analyzers.csproj
dotnet sln backend/dotnet/Tw.SmartPlatform.slnx add backend/dotnet/tools/Tw.Analyzers.Tests/Tw.Analyzers.Tests.csproj
```

- [ ] **Step 7: Commit**

```bash
git add backend/dotnet/tools backend/dotnet/Build backend/dotnet/Directory.Packages.props backend/dotnet/Tw.SmartPlatform.slnx
git commit -m "feat: add dotnet tooling package shells"
```

### Task 2: Implement Dotnet New Templates

**Files:**
- Create: `backend/dotnet/tools/Tw.Templates/content/service/.template.config/template.json`
- Create: `backend/dotnet/tools/Tw.Templates/content/service/src/Company.Service.Domain/Company.Service.Domain.csproj`
- Create: `backend/dotnet/tools/Tw.Templates/content/service/src/Company.Service.Application/Company.Service.Application.csproj`
- Create: `backend/dotnet/tools/Tw.Templates/content/service/src/Company.Service.HttpApi/Company.Service.HttpApi.csproj`
- Create: `backend/dotnet/tools/Tw.Templates/content/service/src/Company.Service.Host/Company.Service.Host.csproj`
- Create: `backend/dotnet/tools/Tw.Templates/content/gateway/.template.config/template.json`
- Create: `backend/dotnet/tools/Tw.Templates/content/building-block/.template.config/template.json`
- Create: `backend/dotnet/tools/Tw.Templates/content/contract-package/.template.config/template.json`
- Test: `backend/dotnet/tools/Tw.Templates.Tests/TemplateSmokeTests.cs`

- [ ] **Step 1: Write template smoke test**

```csharp
using AwesomeAssertions;
using Xunit;

namespace Tw.Templates.Tests;

public sealed class TemplateSmokeTests
{
    [Fact]
    public void ServiceTemplate_DoesNotReferenceForbiddenPackages()
    {
        var root = Path.Combine("content", "service");
        var files = Directory.GetFiles(root, "*.csproj", SearchOption.AllDirectories);
        var text = string.Join(Environment.NewLine, files.Select(File.ReadAllText));

        text.Should().NotContain("Tw.Infrastructure");
        text.Should().NotContain("Tw.UnitOfWork");
        text.Should().NotContain("Tw.Data.Abstractions");
        text.Should().NotContain("MassTransit");
    }
}
```

- [ ] **Step 2: Implement service template**

The service template creates `Domain.Shared`, `Domain`, `Application.Contracts`, `Application`, `HttpApi`, `Infrastructure`, `Host`, `UnitTests`, `IntegrationTests`, and `ContractTests` projects. Generated project references follow the service structure rules: `Domain` does not reference contracts, ASP.NET Core, application, data, cache, event bus, HTTP client, or infrastructure packages.

- [ ] **Step 3: Implement gateway template**

The gateway template references `Tw.Gateway`, `Tw.Gateway.Yarp`, `Tw.AspNetCore`, `Tw.Observability`, and `Tw.Configuration`. It does not reference `Tw.Data.*`, `Tw.Uow`, `Tw.Application`, `Tw.EventBus.*`, `Tw.BackgroundJobs.*`, `Tw.MultiTenancy`, or `Tw.Sharding`.

- [ ] **Step 4: Implement BuildingBlock template**

The BuildingBlock template creates `src/<PackageName>`, `tests/<PackageName>.Tests`, `package-charter.yaml`, README stub, and XML documentation settings. It rejects names listed in the final design forbidden package list.

- [ ] **Step 5: Implement contract package template**

The contract package template emits HTTP DTO, gRPC `.proto`, CAP event contract, and error code catalog placeholders with ID values represented as `long` in C# and decimal string in external HTTP JSON, OpenAPI, and generated client contracts.

- [ ] **Step 6: Run tests and local install**

Run:

```powershell
dotnet test backend/dotnet/tools/Tw.Templates.Tests/Tw.Templates.Tests.csproj --nologo
dotnet pack backend/dotnet/tools/Tw.Templates/Tw.Templates.csproj -o artifacts/templates
dotnet new install (Get-ChildItem artifacts/templates/Tw.Templates*.nupkg | Select-Object -First 1).FullName --force
dotnet new tw-service -n Demo.Billing -o artifacts/template-smoke/Demo.Billing
dotnet build artifacts/template-smoke/Demo.Billing/Demo.Billing.slnx --nologo
```

Expected: PASS.

- [ ] **Step 7: Commit**

```bash
git add backend/dotnet/tools/Tw.Templates backend/dotnet/tools/Tw.Templates.Tests
git commit -m "feat: add dotnet new templates"
```

### Task 3: Implement CLI Commands

**Files:**
- Create: `backend/dotnet/tools/Tw.Cli/Program.cs`
- Create: `backend/dotnet/tools/Tw.Cli/Commands/NewCommand.cs`
- Create: `backend/dotnet/tools/Tw.Cli/Commands/AddCapabilityCommand.cs`
- Create: `backend/dotnet/tools/Tw.Cli/Commands/ValidateContractsCommand.cs`
- Create: `backend/dotnet/tools/Tw.Cli/Commands/AuditDependenciesCommand.cs`
- Create: `backend/dotnet/tools/Tw.Cli/Commands/DiagnoseCommand.cs`
- Create: `backend/dotnet/tools/Tw.Cli/Governance/ForbiddenPackageCatalog.cs`
- Create: `backend/dotnet/tools/Tw.Cli/Governance/ProjectDependencyScanner.cs`
- Test: `backend/dotnet/tools/Tw.Cli.Tests/ValidateContractsCommandTests.cs`
- Test: `backend/dotnet/tools/Tw.Cli.Tests/AuditDependenciesCommandTests.cs`

- [ ] **Step 1: Write dependency audit test**

```csharp
using AwesomeAssertions;
using Tw.Cli.Governance;
using Xunit;

namespace Tw.Cli.Tests;

public sealed class AuditDependenciesCommandTests
{
    [Fact]
    public void Scan_FailsWhenProductionProjectReferencesTestBase()
    {
        var scanner = new ProjectDependencyScanner();
        var result = scanner.ScanProjectText(
            projectPath: "src/Billing.Host/Billing.Host.csproj",
            projectXml: "<Project><ItemGroup><ProjectReference Include=\"..\\Tw.TestBase\\Tw.TestBase.csproj\" /></ItemGroup></Project>");

        result.Errors.Should().Contain(e => e.Code == "TWGOV003");
    }
}
```

- [ ] **Step 2: Implement command surface**

`tw new` wraps the templates. `tw add capability` adds framework package references according to allowed dependency rules. `tw validate contracts` validates HTTP, OpenAPI, gRPC proto, CAP event, and error code contracts. `tw audit dependencies` enforces package dependency rules. `tw diagnose` reports versions, package topology, central package drift, and lock file status.

- [ ] **Step 3: Implement shared governance services**

`ForbiddenPackageCatalog` includes all forbidden package names from the spec. `ProjectDependencyScanner` parses `.csproj` XML with `XDocument`, not string matching, and reports stable error codes.

- [ ] **Step 4: Run CLI tests and pack**

Run:

```powershell
dotnet test backend/dotnet/tools/Tw.Cli.Tests/Tw.Cli.Tests.csproj --nologo
dotnet pack backend/dotnet/tools/Tw.Cli/Tw.Cli.csproj -o artifacts/tools
dotnet tool install --tool-path artifacts/tool-home Tw.Cli --add-source artifacts/tools
artifacts/tool-home/tw diagnose --repository D:/DestinyWorkSpaces/Tw.SmartPlatform
```

Expected: PASS and diagnostics output contains package topology, central package drift result, and lock file status.

- [ ] **Step 5: Commit**

```bash
git add backend/dotnet/tools/Tw.Cli backend/dotnet/tools/Tw.Cli.Tests
git commit -m "feat: add tw dotnet cli"
```

### Task 4: Implement Roslyn Governance Analyzers

**Files:**
- Create: `backend/dotnet/tools/Tw.Analyzers/Rules/ForbiddenPackageNameAnalyzer.cs`
- Create: `backend/dotnet/tools/Tw.Analyzers/Rules/ForbiddenIdentifierPrefixAnalyzer.cs`
- Create: `backend/dotnet/tools/Tw.Analyzers/Rules/ForbiddenProjectReferenceAnalyzer.cs`
- Create: `backend/dotnet/tools/Tw.Analyzers/Rules/DirectThirdPartyUsageAnalyzer.cs`
- Create: `backend/dotnet/tools/Tw.Analyzers/Rules/UserSecretsEnvironmentAnalyzer.cs`
- Create: `backend/dotnet/tools/Tw.Analyzers/Rules/LongIdExternalContractAnalyzer.cs`
- Test: `backend/dotnet/tools/Tw.Analyzers.Tests/ForbiddenIdentifierPrefixAnalyzerTests.cs`
- Test: `backend/dotnet/tools/Tw.Analyzers.Tests/ForbiddenProjectReferenceAnalyzerTests.cs`
- Test: `backend/dotnet/tools/Tw.Analyzers.Tests/LongIdExternalContractAnalyzerTests.cs`

- [ ] **Step 1: Write identifier analyzer test**

```csharp
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace Tw.Analyzers.Tests;

public sealed class ForbiddenIdentifierPrefixAnalyzerTests
{
    [Fact]
    public async Task ReportsTwPrefixExceptTwException()
    {
        var source = """
        namespace Demo;
        public sealed class TwOrderService { }
        public sealed class TwException : System.Exception { }
        """;

        await AnalyzerVerifier<ForbiddenIdentifierPrefixAnalyzer>.VerifyAnalyzerAsync(
            source,
            DiagnosticResult.CompilerWarning("TWGOV001").WithSpan(2, 21, 2, 35));
    }
}
```

- [ ] **Step 2: Implement analyzer rules**

Rules:

- `TWGOV001`: self-owned interfaces, classes, enums, attributes, fields, methods, extension methods, package internal file names, and package internal feature folders must not use `Tw`, `Abp`, or `Furion` framework prefixes. `TwException` is allowed.
- `TWGOV002`: forbidden package names and old package names are compile-time errors.
- `TWGOV003`: production projects must not reference `*TestBase` packages.
- `TWGOV004`: business projects must not directly reference SqlSugar, CAP implementation packages, ASP.NET Core, Quartz, Gateway packages, or infrastructure packages outside allowed layers.
- `TWGOV005`: User Secrets are allowed only in Local and Development entry points.
- `TWGOV006`: HTTP JSON, OpenAPI, and generated client contracts must expose long IDs as decimal strings.

- [ ] **Step 3: Integrate analyzers into templates**

Add `Tw.Analyzers` as `PrivateAssets=all` in generated template projects and in framework sample projects.

- [ ] **Step 4: Run analyzer tests and pack**

Run:

```powershell
dotnet test backend/dotnet/tools/Tw.Analyzers.Tests/Tw.Analyzers.Tests.csproj --nologo
dotnet pack backend/dotnet/tools/Tw.Analyzers/Tw.Analyzers.csproj -o artifacts/analyzers
```

Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add backend/dotnet/tools/Tw.Analyzers backend/dotnet/tools/Tw.Analyzers.Tests backend/dotnet/tools/Tw.Templates
git commit -m "feat: add framework governance analyzers"
```

### Task 5: Implement Contract Validation Gates

**Files:**
- Create: `backend/dotnet/Build/QualityGates/ContractCompatibilityGuard.ps1`
- Create: `backend/dotnet/Build/QualityGates/ErrorCodeCatalogGuard.ps1`
- Create: `backend/dotnet/Build/QualityGates/LongIdContractGuard.ps1`
- Create: `backend/dotnet/Build/QualityGates/CapEventContractGuard.ps1`
- Modify: `backend/dotnet/Build/Build.cs`

- [ ] **Step 1: Add HTTP and OpenAPI guard**

`ContractCompatibilityGuard.ps1` compares generated OpenAPI with the approved baseline and fails on removed operations, removed fields, changed status codes, changed error response model, or changed ID schema from string to number.

- [ ] **Step 2: Add gRPC proto guard**

The same guard validates `.proto` compatibility: field numbers are not reused, fields are not removed without reservation, package names are stable, and services are not renamed.

- [ ] **Step 3: Add CAP event guard**

`CapEventContractGuard.ps1` validates event name, version, required fields, tenant and correlation metadata, idempotency key, and schema compatibility.

- [ ] **Step 4: Add error code guard**

`ErrorCodeCatalogGuard.ps1` validates stable error code uniqueness, category prefix, owner, HTTP status mapping, gRPC status mapping, retry classification, and localization key presence.

- [ ] **Step 5: Add long ID guard**

`LongIdContractGuard.ps1` fails when OpenAPI schemas, generated TypeScript clients, generated C# HTTP clients, or JSON fixtures expose `long` IDs as JSON numbers instead of decimal strings.

- [ ] **Step 6: Wire gates into NUKE**

`Build.cs` adds targets `ValidateContracts`, `ValidateErrorCodes`, `ValidateLongIdContracts`, and `ValidateCapEventContracts`, all required before pack and publish.

- [ ] **Step 7: Run gates**

Run:

```powershell
pwsh backend/dotnet/Build/QualityGates/ContractCompatibilityGuard.ps1
pwsh backend/dotnet/Build/QualityGates/ErrorCodeCatalogGuard.ps1
pwsh backend/dotnet/Build/QualityGates/LongIdContractGuard.ps1
pwsh backend/dotnet/Build/QualityGates/CapEventContractGuard.ps1
```

Expected: PASS.

- [ ] **Step 8: Commit**

```bash
git add backend/dotnet/Build/QualityGates backend/dotnet/Build/Build.cs
git commit -m "build: add contract governance gates"
```

### Task 6: Implement CI/CD And Release Governance

**Files:**
- Modify: `backend/dotnet/Build/Build.cs`
- Create: `backend/dotnet/Build/QualityGates/PackageCharterGuard.ps1`
- Create: `backend/dotnet/Build/QualityGates/ForbiddenPackageGuard.ps1`
- Create: `backend/dotnet/Build/QualityGates/PackageBoundaryGuard.ps1`
- Create: `backend/dotnet/Build/QualityGates/CoverageThresholdGuard.ps1`
- Create: `.github/workflows/dotnet-ci.yml`
- Create: `.github/workflows/dotnet-release.yml`
- Create: `deploy/helm/tw-service/Chart.yaml`
- Create: `deploy/helm/tw-service/values.yaml`
- Create: `deploy/argocd/tw-service-application.yaml`

- [ ] **Step 1: Add NUKE build targets**

Targets:

- `Restore`
- `Compile`
- `Test`
- `Coverage`
- `Mutation`
- `ValidatePackageCharters`
- `ValidateForbiddenPackages`
- `ValidatePackageBoundaries`
- `ValidateContracts`
- `ValidateSensitiveOutput`
- `Pack`
- `Sbom`
- `ImageScan`
- `Sign`
- `HelmLint`
- `ArgoCdValidate`
- `Publish`

- [ ] **Step 2: Add package governance gates**

`PackageCharterGuard.ps1` fails when a runtime package lacks `package-charter.yaml`. `ForbiddenPackageGuard.ps1` fails on forbidden package names, compatibility aliases, type forwarders, obsolete shells, and empty compatibility implementations. `PackageBoundaryGuard.ps1` checks dependency rules from package charters and from the final design.

- [ ] **Step 3: Add coverage and mutation gates**

`CoverageThresholdGuard.ps1` enforces 98 percent line coverage for framework-owned packages. Stryker.NET mutation runs on high-risk packages: UoW, data, CAP, multi-tenancy, sharding, authorization, idempotency, gateway, configuration, and application pipeline.

- [ ] **Step 4: Add SBOM, image scan, and signing**

NUKE invokes CycloneDX for SBOM, Trivy for image scanning, and Cosign for image signing. Release fails when SBOM generation, image scan, or signing fails.

- [ ] **Step 5: Add Helm and Argo CD validation**

Helm chart assets live under `deploy/helm`. Argo CD application manifests live under `deploy/argocd`. NUKE validates `helm lint`, template rendering, required image tags, probes, resource requests, secret references, and namespace ownership.

- [ ] **Step 6: Add GitHub workflows**

`dotnet-ci.yml` runs restore, compile, tests, coverage, analyzers, contract gates, and package boundary gates on pull requests. `dotnet-release.yml` runs all CI gates plus pack, SBOM, image scan, signing, Helm validation, Argo CD validation, and publish on version tags.

- [ ] **Step 7: Run release pipeline locally**

Run:

```powershell
dotnet run --project backend/dotnet/Build/Build.csproj -- --target Compile
dotnet run --project backend/dotnet/Build/Build.csproj -- --target Test
dotnet run --project backend/dotnet/Build/Build.csproj -- --target ValidatePackageBoundaries
dotnet run --project backend/dotnet/Build/Build.csproj -- --target ValidateContracts
dotnet run --project backend/dotnet/Build/Build.csproj -- --target HelmLint
dotnet run --project backend/dotnet/Build/Build.csproj -- --target ArgoCdValidate
```

Expected: PASS.

- [ ] **Step 8: Commit**

```bash
git add backend/dotnet/Build .github/workflows deploy
git commit -m "build: add ci release and deployment governance"
```

### Task 7: Documentation And Final Verification

**Files:**
- Create: `docs/shared-packages/dotnet/Tw.Templates/README.md`
- Create: `docs/shared-packages/dotnet/Tw.Cli/README.md`
- Create: `docs/shared-packages/dotnet/Tw.Analyzers/README.md`
- Create: `docs/engineering-standards/10-governance/dotnet-framework-governance.md`
- Modify: `docs/shared-packages/dotnet/README.md`
- Modify: `docs/engineering-standards/README.md`

- [ ] **Step 1: Document tools**

Tool docs include install commands, public commands or template names, governance checks, examples, and failure code catalog.

- [ ] **Step 2: Document CI/CD governance**

`dotnet-framework-governance.md` describes mandatory gates: package charters, forbidden package scan, dependency boundaries, contract compatibility, long ID external contract string check, CAP event contract check, test-only packages, coverage, mutation, SBOM, image scan, signing, Helm, and Argo CD validation.

- [ ] **Step 3: Update indexes**

Add `Tw.Templates`, `Tw.Cli`, and `Tw.Analyzers` to tool documentation indexes. Add the governance standard to `docs/engineering-standards/README.md`.

- [ ] **Step 4: Run final verification**

Run:

```powershell
dotnet test backend/dotnet/tools/Tw.Analyzers.Tests/Tw.Analyzers.Tests.csproj --nologo
dotnet test backend/dotnet/tools/Tw.Cli.Tests/Tw.Cli.Tests.csproj --nologo
dotnet test backend/dotnet/tools/Tw.Templates.Tests/Tw.Templates.Tests.csproj --nologo
pwsh backend/dotnet/Build/QualityGates/ForbiddenPackageGuard.ps1
pwsh backend/dotnet/Build/QualityGates/PackageBoundaryGuard.ps1
pwsh backend/dotnet/Build/QualityGates/LongIdContractGuard.ps1
```

Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add docs/shared-packages docs/engineering-standards backend/dotnet/tools backend/dotnet/Build
git commit -m "docs: document tooling and governance gates"
```

## Plan Self-Review

- Spec coverage: .NET-specific tools under `backend/dotnet/tools`, service and gateway templates, CLI governance commands, Roslyn analyzers, contract validation, long ID external string contract, CAP event contracts, package boundaries, CI/CD, SBOM, image scan, signing, Helm, and Argo CD are covered.
- Forbidden compatibility layers: gates explicitly reject forbidden packages, aliases, type forwarders, obsolete shells, and empty compatibility implementations.
- Test-only packages: analyzers and build guards enforce production-project exclusion.
- Placeholder scan: no placeholder tokens are present.
- Verification: analyzer tests, CLI tests, template smoke tests, build gates, and local release pipeline targets are included.
