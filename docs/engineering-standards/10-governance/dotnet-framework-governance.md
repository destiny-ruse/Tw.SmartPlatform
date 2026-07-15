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
$env:PYTHONPATH = (Resolve-Path tools/src).Path
python -m pytest tools/tests
python -m tw_memory check --root .
dotnet test backend/dotnet/Tw.SmartPlatform.slnx
```

治理检查由架构测试、完整 Python 工具测试、`tw_memory` 仓库事实校验和解决方案测试承载。`backend/dotnet/Build` 只保存中央包版本 `.props` 与必要锁定文件。

## Isolated Pre-commit Gate

仓库必须使用以下 local hook 执行提交边界检查：

```yaml
- repo: local
  hooks:
    - id: tw-memory-check
      name: tw-memory check
      entry: python -I tools/scripts/run_tw_memory.py check --staged
      language: python
      additional_dependencies:
        - PyYAML>=6.0
      pass_filenames: false
      always_run: true
```

`language: python` 为 hook 创建隔离环境并只安装外部 YAML 依赖。`-I` 屏蔽调用方的 `PYTHONPATH` 和用户 site-packages；`tools/scripts/run_tw_memory.py` 依据自身绝对路径加载 `tools/src`，因此不依赖系统安装的 `tw-memory`。
