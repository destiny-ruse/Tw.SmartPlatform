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

    assert run_generate(str(repo)) == 0

    assert (repo / ".tw-memory/manifest/source-index.generated.json").exists()
    assert (repo / ".tw-memory/routes/standards.generated.yaml").exists()
    assert (repo / ".tw-memory/cards/packages/Tw.Core.generated.md").exists()
    assert not (repo / ".tw-memory/runtime").exists()

    packages_route = load_yaml(repo / ".tw-memory/routes/packages.generated.yaml")
    assert packages_route["packages"]["Tw.Core"]["card"] == ".tw-memory/cards/packages/Tw.Core.generated.md"
    assert packages_route["packages"]["Tw.Core"]["public_api_card"] == ".tw-memory/cards/public-apis/Tw.Core.generated.md"
    assert "apis" in load_yaml(repo / ".tw-memory/routes/apis.generated.yaml")
    assert load_yaml(repo / ".tw-memory/manifest/taxonomy.generated.yaml")["generator"] == GENERATOR_VERSION

    source_index = json.loads((repo / ".tw-memory/manifest/source-index.generated.json").read_text(encoding="utf-8"))
    assert "charter:package-charter:Tw.Core" in source_index["sources"]
