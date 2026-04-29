# Auto Registration Dependency Admission Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Establish dependency admission, central package versions, and reproducible restore before any DI auto-registration or AOP code lands.

**Architecture:** This plan is a preflight gate for the ordered plan set. It creates the dependency decision record first, then pins NuGet versions through central package management, then updates only the BuildingBlocks projects that need the packages.

**Tech Stack:** .NET 10, Central Package Management, NuGet, Autofac, Castle DynamicProxy, Microsoft.Extensions.Options, gRPC for .NET

---

## Execution Order

Run this file first. Continue with:

1. `2026-04-29-01-auto-registration-core.md`
2. `2026-04-29-02-options-auto-registration.md`
3. `2026-04-29-03-service-aop-castle.md`
4. `2026-04-29-04-aspnetcore-mvc-adapter.md`
5. `2026-04-29-05-cross-cutting-quality-gates.md`
6. `2026-04-29-06-grpc-adapter-p2.md`
7. `2026-04-29-07-cap-worker-adapter-boundary-p3.md`

## Source Inputs

- Design: `docs/superpowers/specs/2026-04-29-auto-registration-aop-design.md`
- Dependency policy: `rules.dependency-policy#rules`, version `1.1.0`, `docs/standards/rules/dependency-policy.md`
- Dependency process: `processes.dependency-onboarding#flow` and `processes.dependency-onboarding#rules`, version `1.0.0`, `docs/standards/processes/dependency-onboarding.md`
- Repository layout: `rules.repo-layout#rules`, version `1.1.0`, `docs/standards/rules/repo-layout.md`

## File Structure

- Create: `docs/superpowers/dependencies/2026-04-29-autofac-castle-grpc-admission.md` records the dependency decision, source links, alternatives, licenses, scan commands, owners, and stage boundary.
- Modify: `backend/dotnet/Build/Packages.Microsoft.props` pins Microsoft Options packages used by automatic options binding.
- Modify: `backend/dotnet/Build/Packages.ThirdParty.props` pins Autofac, Castle, and gRPC packages.
- Modify: `backend/dotnet/BuildingBlocks/src/Tw.Core/Tw.Core.csproj` references the core runtime packages needed by P0/P1 plans.
- Modify: `backend/dotnet/BuildingBlocks/src/Tw.AspNetCore/Tw.AspNetCore.csproj` references ASP.NET Core integration packages needed by P1/P2 plans.
- Verify: `backend/dotnet/BuildingBlocks/src/Tw.Core/packages.lock.json` and `backend/dotnet/BuildingBlocks/src/Tw.AspNetCore/packages.lock.json` update through restore.

## Package Versions

Use stable packages, not prerelease versions:

| Package | Version | Stage | Reason |
| --- | --- | --- | --- |
| `Autofac` | `9.1.0` | P0 | Final DI container and metadata registration |
| `Autofac.Extensions.DependencyInjection` | `11.0.0` | P0 | `AutofacServiceProviderFactory` host integration |
| `Autofac.Extras.DynamicProxy` | `7.1.0` | P1 | Castle interceptor integration through Autofac registration |
| `Castle.Core` | `5.2.1` | P1 | DynamicProxy base package |
| `Castle.Core.AsyncInterceptor` | `2.1.0` | P1 | Async method interception dispatch |
| `Microsoft.Extensions.Configuration` | `10.0.5` | P0 | Configuration abstractions and test configuration builder support |
| `Microsoft.Extensions.Configuration.Binder` | `10.0.7` | P0 | Configuration binding for options auto-registration |
| `Microsoft.Extensions.DependencyInjection` | `10.0.5` | P0 | `IServiceCollection`, registration extensions, and test service provider support |
| `Microsoft.Extensions.Options.ConfigurationExtensions` | `10.0.7` | P0 | Options binding extension methods |
| `Microsoft.Extensions.Options.DataAnnotations` | `10.0.7` | P0 | `ValidateDataAnnotations()` and `ValidateOnStart()` |
| `Grpc.AspNetCore` | `2.76.0` | P2 | Server-side gRPC interceptor integration |
| `Grpc.Core.Api` | `2.76.0` | P2 | `ServerCallContext` and gRPC shared API types |

## Tasks

### Task 1: Write Dependency Admission Record

**Files:**
- Create: `docs/superpowers/dependencies/2026-04-29-autofac-castle-grpc-admission.md`

- [ ] **Step 1: Create the admission document**

```markdown
# Autofac, Castle, Options, and gRPC Dependency Admission

**Date:** 2026-04-29
**Owner:** platform
**Design:** `docs/superpowers/specs/2026-04-29-auto-registration-aop-design.md`
**Decision:** Approved for staged implementation when package restore, lock files, license check, and vulnerability scan pass

## Scope

P0/P1 may use Autofac, Autofac.Extensions.DependencyInjection, Autofac.Extras.DynamicProxy, Castle.Core, Castle.Core.AsyncInterceptor, Microsoft.Extensions.Configuration.Binder, Microsoft.Extensions.Options.ConfigurationExtensions, and Microsoft.Extensions.Options.DataAnnotations.

P2 may use Grpc.AspNetCore and Grpc.Core.Api when executing `2026-04-29-06-grpc-adapter-p2.md`.

## Alternatives

Native Microsoft DI was considered and rejected for this design because it does not provide the same Autofac keyed service, registration metadata, and DynamicProxy integration model required by the design.

DispatchProxy was considered and rejected for the first implementation because class proxy support, Autofac ecosystem integration, and existing Castle AsyncInterceptor behavior reduce framework glue code.

## Required Validation

Run these commands from `backend/dotnet`:

```powershell
dotnet restore BuildingBlocks/src/Tw.Core/Tw.Core.csproj --use-lock-file
dotnet restore BuildingBlocks/src/Tw.AspNetCore/Tw.AspNetCore.csproj --use-lock-file
dotnet list BuildingBlocks/src/Tw.Core/Tw.Core.csproj package --vulnerable --include-transitive
dotnet list BuildingBlocks/src/Tw.AspNetCore/Tw.AspNetCore.csproj package --vulnerable --include-transitive
dotnet test BuildingBlocks/tests/Tw.Core.Tests/Tw.Core.Tests.csproj
```

Expected result: restore succeeds, vulnerability output contains no high or critical vulnerabilities, and tests pass.
```

- [ ] **Step 2: Commit**

```bash
git add docs/superpowers/dependencies/2026-04-29-autofac-castle-grpc-admission.md
git commit -m "docs: record autofac castle dependency admission"
```

### Task 2: Pin Central Package Versions

**Files:**
- Modify: `backend/dotnet/Build/Packages.Microsoft.props`
- Modify: `backend/dotnet/Build/Packages.ThirdParty.props`

- [ ] **Step 1: Update Microsoft package versions**

Add these entries inside `backend/dotnet/Build/Packages.Microsoft.props`:

```xml
<PackageVersion Include="Microsoft.Extensions.Configuration" Version="10.0.5" />
<PackageVersion Include="Microsoft.Extensions.Configuration.Binder" Version="10.0.7" />
<PackageVersion Include="Microsoft.Extensions.DependencyInjection" Version="10.0.5" />
<PackageVersion Include="Microsoft.Extensions.Options.ConfigurationExtensions" Version="10.0.7" />
<PackageVersion Include="Microsoft.Extensions.Options.DataAnnotations" Version="10.0.7" />
```

- [ ] **Step 2: Update third-party package versions**

Add these entries inside `backend/dotnet/Build/Packages.ThirdParty.props`:

```xml
<PackageVersion Include="Autofac" Version="9.1.0" />
<PackageVersion Include="Autofac.Extensions.DependencyInjection" Version="11.0.0" />
<PackageVersion Include="Autofac.Extras.DynamicProxy" Version="7.1.0" />
<PackageVersion Include="Castle.Core" Version="5.2.1" />
<PackageVersion Include="Castle.Core.AsyncInterceptor" Version="2.1.0" />
<PackageVersion Include="Grpc.AspNetCore" Version="2.76.0" />
<PackageVersion Include="Grpc.Core.Api" Version="2.76.0" />
```

- [ ] **Step 3: Run restore to verify central package metadata**

Run:

```powershell
dotnet restore backend/dotnet/BuildingBlocks/src/Tw.Core/Tw.Core.csproj --use-lock-file
```

Expected: restore succeeds and does not report duplicate central version entries.

- [ ] **Step 4: Commit**

```bash
git add backend/dotnet/Build/Packages.Microsoft.props backend/dotnet/Build/Packages.ThirdParty.props
git commit -m "build: pin autofac castle options packages"
```

### Task 3: Add Project Package References

**Files:**
- Modify: `backend/dotnet/BuildingBlocks/src/Tw.Core/Tw.Core.csproj`
- Modify: `backend/dotnet/BuildingBlocks/src/Tw.AspNetCore/Tw.AspNetCore.csproj`

- [ ] **Step 1: Add `Tw.Core` references**

Add this `ItemGroup` to `Tw.Core.csproj`:

```xml
<ItemGroup>
  <PackageReference Include="Autofac" />
  <PackageReference Include="Autofac.Extras.DynamicProxy" />
  <PackageReference Include="Castle.Core" />
  <PackageReference Include="Castle.Core.AsyncInterceptor" />
  <PackageReference Include="Microsoft.Extensions.Configuration" />
  <PackageReference Include="Microsoft.Extensions.Configuration.Binder" />
  <PackageReference Include="Microsoft.Extensions.DependencyInjection" />
  <PackageReference Include="Microsoft.Extensions.Options.ConfigurationExtensions" />
  <PackageReference Include="Microsoft.Extensions.Options.DataAnnotations" />
</ItemGroup>
```

- [ ] **Step 2: Add `Tw.AspNetCore` references**

Add this `ItemGroup` to `Tw.AspNetCore.csproj`:

```xml
<ItemGroup>
  <ProjectReference Include="..\Tw.Core\Tw.Core.csproj" />
  <PackageReference Include="Autofac.Extensions.DependencyInjection" />
  <PackageReference Include="Grpc.AspNetCore" />
  <PackageReference Include="Grpc.Core.Api" />
</ItemGroup>
```

- [ ] **Step 3: Restore lock files**

Run:

```powershell
dotnet restore backend/dotnet/BuildingBlocks/src/Tw.Core/Tw.Core.csproj --use-lock-file
dotnet restore backend/dotnet/BuildingBlocks/src/Tw.AspNetCore/Tw.AspNetCore.csproj --use-lock-file
```

Expected: `packages.lock.json` files update under both source projects.

- [ ] **Step 4: Run package vulnerability scan**

Run:

```powershell
dotnet list backend/dotnet/BuildingBlocks/src/Tw.Core/Tw.Core.csproj package --vulnerable --include-transitive
dotnet list backend/dotnet/BuildingBlocks/src/Tw.AspNetCore/Tw.AspNetCore.csproj package --vulnerable --include-transitive
```

Expected: no high or critical vulnerability is reported. If a lower-severity vulnerability appears, record the CVE, severity, package path, mitigation, owner, and expiry date in the admission document before continuing.

- [ ] **Step 5: Commit**

```bash
git add backend/dotnet/BuildingBlocks/src/Tw.Core/Tw.Core.csproj backend/dotnet/BuildingBlocks/src/Tw.Core/packages.lock.json backend/dotnet/BuildingBlocks/src/Tw.AspNetCore/Tw.AspNetCore.csproj backend/dotnet/BuildingBlocks/src/Tw.AspNetCore/packages.lock.json docs/superpowers/dependencies/2026-04-29-autofac-castle-grpc-admission.md
git commit -m "build: add auto registration runtime dependencies"
```

### Task 4: Baseline Verification

**Files:**
- Verify: `backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Tw.Core.Tests.csproj`

- [ ] **Step 1: Run existing tests**

Run:

```powershell
dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Tw.Core.Tests.csproj
```

Expected: PASS.

- [ ] **Step 2: Record validation evidence**

Append this section to `docs/superpowers/dependencies/2026-04-29-autofac-castle-grpc-admission.md` with the actual command date and result:

```markdown
## Validation Evidence

| Command | Result | Notes |
| --- | --- | --- |
| `dotnet restore backend/dotnet/BuildingBlocks/src/Tw.Core/Tw.Core.csproj --use-lock-file` | PASS | Lock file updated |
| `dotnet restore backend/dotnet/BuildingBlocks/src/Tw.AspNetCore/Tw.AspNetCore.csproj --use-lock-file` | PASS | Lock file updated |
| `dotnet list backend/dotnet/BuildingBlocks/src/Tw.Core/Tw.Core.csproj package --vulnerable --include-transitive` | PASS | No high or critical vulnerability |
| `dotnet list backend/dotnet/BuildingBlocks/src/Tw.AspNetCore/Tw.AspNetCore.csproj package --vulnerable --include-transitive` | PASS | No high or critical vulnerability |
| `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Tw.Core.Tests.csproj` | PASS | Existing baseline preserved |
```

- [ ] **Step 3: Commit**

```bash
git add docs/superpowers/dependencies/2026-04-29-autofac-castle-grpc-admission.md
git commit -m "docs: add dependency validation evidence"
```

## Self-Review Checklist

- [ ] Dependency record exists before package files change.
- [ ] Package versions are pinned through central package management.
- [ ] Lock files are updated by restore, not by hand.
- [ ] Validation commands and vulnerability scan results are recorded.
- [ ] No production package uses `latest`, prerelease, or an unbounded floating version.
