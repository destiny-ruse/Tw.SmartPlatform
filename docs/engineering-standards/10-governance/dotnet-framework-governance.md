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
dotnet test backend/dotnet/BuildingBlocks/tests/Architecture/Tw.Architecture.Tests/Tw.Architecture.Tests.csproj
python -m pytest tools/tests/test_charter.py
dotnet test backend/dotnet/Tw.SmartPlatform.slnx
```

治理检查由架构测试、Python charter 校验和解决方案测试承载。`backend/dotnet/Build` 只保存中央包版本 `.props` 与必要锁定文件。
