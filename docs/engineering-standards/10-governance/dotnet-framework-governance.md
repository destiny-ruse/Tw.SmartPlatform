# .NET Framework Governance

This standard defines mandatory governance gates for the Tw .NET framework.

## Required Gates

- Package charter validation for every runtime shared package.
- Forbidden package scan for retired package names, compatibility aliases, type forwarders, and empty compatibility shells.
- Package boundary validation from charters and final framework design.
- Contract compatibility validation for HTTP, OpenAPI, gRPC, CAP events, and error-code catalogs.
- Long ID external contract validation so HTTP JSON, OpenAPI, and generated clients use decimal strings.
- Test-only package validation so production projects do not reference `*TestBase` packages.
- Sensitive output validation for logs, audit records, and diagnostics.
- Coverage and mutation gates for high-risk framework packages.
- SBOM generation, image scanning, signing, Helm validation, and Argo CD validation before release.

## Local Commands

```powershell
dotnet run --project backend/dotnet/Build/Build.csproj -- --target Compile
dotnet run --project backend/dotnet/Build/Build.csproj -- --target Test
dotnet run --project backend/dotnet/Build/Build.csproj -- --target ValidatePackageBoundaries
dotnet run --project backend/dotnet/Build/Build.csproj -- --target ValidateContracts
dotnet run --project backend/dotnet/Build/Build.csproj -- --target HelmLint
dotnet run --project backend/dotnet/Build/Build.csproj -- --target ArgoCdValidate
```

PowerShell gates can be run directly with Windows PowerShell:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File backend/dotnet/Build/QualityGates/ForbiddenPackageGuard.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File backend/dotnet/Build/QualityGates/PackageBoundaryGuard.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File backend/dotnet/Build/QualityGates/LongIdContractGuard.ps1
```
