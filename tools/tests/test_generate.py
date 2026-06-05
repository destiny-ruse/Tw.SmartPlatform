from __future__ import annotations

import json
from pathlib import Path

from tw_memory import GENERATOR_VERSION
from conftest import make_csproj, write_text
from tw_memory.generate import run_generate
from tw_memory.yaml_io import load_yaml


def test_generate_writes_commit_memory_without_runtime_state(repo: Path) -> None:
    write_text(
        repo / ".rules/ai-coding-rules/00-always-load.md",
        "# Always\n\n## Required Formal Standards\n\n"
        "- `docs/engineering-standards/03-project-and-code/coding-standards.md`\n",
    )
    write_text(
        repo / "docs/engineering-standards/03-project-and-code/coding-standards.md",
        "# 通用编码规范\n\n## 目标\n\n清晰。\n",
    )
    package_dir = make_csproj(
        repo,
        "Tw.Core",
        '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework></PropertyGroup></Project>',
    )
    write_text(
        package_dir / "Context/CancellationTokenServiceCollectionExtensions.cs",
        """using Microsoft.Extensions.DependencyInjection;

namespace Tw.Context;

public static class CancellationTokenServiceCollectionExtensions
{
    public static IServiceCollection AddCancellationTokenProvider(this IServiceCollection services)
    {
        return services;
    }
}
""",
    )
    write_text(
        package_dir / "package-charter.yaml",
        """schema_version: memory.charter.v1
package: Tw.Core
owner: platform-team
responsibility: Provides cross-service primitives.
in_scope:
  - Shared primitives
out_of_scope:
  - Web host integration
public_capabilities:
  - Tw.Core.Primitives
dependency_rules:
  forbid:
    - Microsoft.AspNetCore.*
  allow:
    - System.*
stability: stable
""",
    )
    write_text(
        repo / "docs/shared-packages/dotnet/Tw.Core/context/cancellation-token-provider.md",
        "# 取消令牌 provider 使用指南\n",
    )

    assert run_generate(str(repo)) == 0

    assert (repo / ".tw-memory/manifest/source-index.generated.json").exists()
    assert (repo / ".tw-memory/routes/standards.generated.yaml").exists()
    assert (repo / ".tw-memory/cards/packages/Tw.Core.generated.md").exists()
    assert not (repo / ".tw-memory/runtime").exists()

    packages_route = load_yaml(repo / ".tw-memory/routes/packages.generated.yaml")
    assert packages_route["packages"]["Tw.Core"]["card"] == ".tw-memory/cards/packages/Tw.Core.generated.md"
    assert packages_route["packages"]["Tw.Core"]["public_api_card"] == ".tw-memory/cards/public-apis/Tw.Core.generated.md"
    assert "apis" in load_yaml(repo / ".tw-memory/routes/apis.generated.yaml")
    taxonomy = load_yaml(repo / ".tw-memory/manifest/taxonomy.generated.yaml")
    assert taxonomy["generator"] == GENERATOR_VERSION
    assert "package-source" in taxonomy["source_types"]
    assert "package-doc" in taxonomy["source_types"]

    source_index = json.loads((repo / ".tw-memory/manifest/source-index.generated.json").read_text(encoding="utf-8"))
    assert "charter:package-charter:Tw.Core" in source_index["sources"]
    assert "package-source:Tw.Core:backend/dotnet/BuildingBlocks/src/Tw.Core/Context/CancellationTokenServiceCollectionExtensions.cs" in source_index["sources"]
    assert "package-doc:Tw.Core:docs/shared-packages/dotnet/Tw.Core/context/cancellation-token-provider.md" in source_index["sources"]

    public_api_card = (repo / ".tw-memory/cards/public-apis/Tw.Core.generated.md").read_text(encoding="utf-8")
    assert "公开能力边界：" in public_api_card
    assert "实现公开命名空间：" in public_api_card
    assert "- Tw.Context" in public_api_card
    assert "- static class CancellationTokenServiceCollectionExtensions" in public_api_card
    assert "- Tw.Context.CancellationTokenServiceCollectionExtensions.AddCancellationTokenProvider" in public_api_card
    assert "docs/shared-packages/dotnet/Tw.Core/context/cancellation-token-provider.md" in public_api_card
