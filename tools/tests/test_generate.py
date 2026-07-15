from __future__ import annotations

import json
from pathlib import Path

import pytest

import tw_memory.generate as generate_module
from tw_memory import GENERATOR_VERSION
from conftest import make_csproj, write_text
from tw_memory.generate import run_generate
from tw_memory.yaml_io import load_yaml


CHARTER = """schema_version: memory.charter.v1
package: Tw.Core
owner: platform-team
responsibility: Provides cross-service primitives.
in_scope: [Shared primitives]
out_of_scope: [Web host integration]
public_capabilities: [Tw.Core.Primitives]
dependency_rules: {forbid: [], allow: []}
"""


def seed_package(repo: Path) -> Path:
    """Create the smallest valid package needed by generation boundary tests."""
    package_dir = make_csproj(repo, "Foundation", "Tw.Core")
    write_text(package_dir / "package-charter.yaml", CHARTER)
    return package_dir


@pytest.mark.parametrize("cards_directory", ["packages", "public-apis"])
def test_generate_rejects_external_generated_card_root_before_mutation(
    repo: Path,
    cards_directory: str,
) -> None:
    external_root = repo.parent / f"{repo.name}-external-{cards_directory}"
    external_card = write_text(external_root / "user.generated.md", "user content\n")
    cards_root = repo / ".tw-memory/cards"
    cards_root.mkdir(parents=True)
    (cards_root / cards_directory).symlink_to(external_root, target_is_directory=True)

    assert run_generate(str(repo)) == 1
    assert external_card.read_text(encoding="utf-8") == "user content\n"
    assert not (repo / ".tw-memory/routes").exists()


def test_generate_reports_malformed_charter_before_output_mutation(repo: Path) -> None:
    package_dir = make_csproj(repo, "Foundation", "Tw.Core")
    write_text(package_dir / "package-charter.yaml", "package: [\n")

    assert run_generate(str(repo)) == 1
    assert not (repo / ".tw-memory/routes").exists()


def test_generate_rejects_symlink_deletion_candidate_before_mutation(repo: Path) -> None:
    external_card = write_text(
        repo.parent / f"{repo.name}-external/user.generated.md",
        "user content\n",
    )
    package_cards = repo / ".tw-memory/cards/packages"
    package_cards.mkdir(parents=True)
    linked_card = package_cards / "Tw.Old.generated.md"
    linked_card.symlink_to(external_card)

    assert run_generate(str(repo)) == 1
    assert linked_card.is_symlink()
    assert external_card.read_text(encoding="utf-8") == "user content\n"
    assert not (repo / ".tw-memory/routes").exists()


def test_generate_reports_unsafe_compile_before_output_mutation(repo: Path) -> None:
    package_dir = make_csproj(
        repo,
        "Foundation",
        "Tw.Core",
        '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><EnableDefaultCompileItems>false</EnableDefaultCompileItems></PropertyGroup><ItemGroup><Compile Include="../Outside.cs" /></ItemGroup></Project>',
    )
    write_text(package_dir / "../Outside.cs", "namespace Tw.Outside;\npublic sealed class Outside {}\n")
    write_text(
        package_dir / "package-charter.yaml",
        """schema_version: memory.charter.v1
package: Tw.Core
owner: platform-team
responsibility: 跨服务复用的基础原语。
in_scope: [基础原语]
out_of_scope: [宿主集成]
public_capabilities: [Tw.Core]
dependency_rules: {forbid: [], allow: []}
""",
    )
    existing_card = write_text(
        repo / ".tw-memory/cards/packages/Tw.Core.generated.md",
        "existing card\n",
    )

    assert run_generate(str(repo)) == 1
    assert existing_card.read_text(encoding="utf-8") == "existing card\n"
    assert not (repo / ".tw-memory/routes").exists()


def test_generate_preserves_existing_cards_when_discovery_is_invalid(repo: Path) -> None:
    write_text(
        repo / "backend/dotnet/BuildingBlocks/src/Tw.Flat/Tw.Flat.csproj",
        '<Project Sdk="Microsoft.NET.Sdk" />',
    )
    existing_card = write_text(
        repo / ".tw-memory/cards/packages/Tw.Existing.generated.md",
        "existing card\n",
    )

    assert run_generate(str(repo)) == 1
    assert existing_card.read_text(encoding="utf-8") == "existing card\n"
    assert not (repo / ".tw-memory/routes").exists()


def test_generate_revalidates_card_root_at_each_write_boundary(
    repo: Path,
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    seed_package(repo)
    package_cards = repo / ".tw-memory/cards/packages"
    package_cards.mkdir(parents=True)
    external_cards = repo.parent / f"{repo.name}-external-cards"
    victim = write_text(external_cards / "Tw.Core.generated.md", "external victim\n")
    original_write_route = generate_module.write_route
    swapped = False

    def swap_before_first_writer(*args: object, **kwargs: object) -> None:
        nonlocal swapped
        if not swapped:
            package_cards.rmdir()
            package_cards.symlink_to(external_cards, target_is_directory=True)
            swapped = True
        original_write_route(*args, **kwargs)

    monkeypatch.setattr(generate_module, "write_route", swap_before_first_writer)

    assert run_generate(str(repo)) == 1
    assert victim.read_text(encoding="utf-8") == "external victim\n"
    assert sorted(path.name for path in external_cards.iterdir()) == ["Tw.Core.generated.md"]


def test_generate_rejects_external_package_doc_before_output_mutation(repo: Path) -> None:
    seed_package(repo)
    external_readme = write_text(
        repo.parent / f"{repo.name}-external-docs/README.md",
        "# External documentation\n",
    )
    docs_root = repo / "docs/shared-packages/dotnet/Tw.Core"
    docs_root.mkdir(parents=True)
    (docs_root / "README.md").symlink_to(external_readme)
    existing_card = write_text(
        repo / ".tw-memory/cards/packages/Tw.Core.generated.md",
        "existing card\n",
    )

    assert run_generate(str(repo)) == 1
    assert existing_card.read_text(encoding="utf-8") == "existing card\n"
    assert not (repo / ".tw-memory/routes").exists()


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
        "Foundation",
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
    orphan_package_card = write_text(
        repo / ".tw-memory/cards/packages/Tw.Old.generated.md",
        "# Package: Tw.Old\n",
    )
    orphan_api_card = write_text(
        repo / ".tw-memory/cards/public-apis/Tw.Old.generated.md",
        "# Public API: Tw.Old\n",
    )

    assert run_generate(str(repo)) == 0

    assert (repo / ".tw-memory/manifest/source-index.generated.json").exists()
    assert (repo / ".tw-memory/routes/standards.generated.yaml").exists()
    assert (repo / ".tw-memory/cards/packages/Tw.Core.generated.md").exists()
    assert not (repo / ".tw-memory/runtime").exists()
    assert not orphan_package_card.exists()
    assert not orphan_api_card.exists()

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
    assert "package-source:Tw.Core:backend/dotnet/BuildingBlocks/src/Foundation/Tw.Core/Context/CancellationTokenServiceCollectionExtensions.cs" in source_index["sources"]
    assert "package-doc:Tw.Core:docs/shared-packages/dotnet/Tw.Core/context/cancellation-token-provider.md" in source_index["sources"]

    public_api_card = (repo / ".tw-memory/cards/public-apis/Tw.Core.generated.md").read_text(encoding="utf-8")
    assert "公开能力边界：" in public_api_card
    assert "实现公开命名空间：" in public_api_card
    assert "- Tw.Context" in public_api_card
    assert "- static class CancellationTokenServiceCollectionExtensions" in public_api_card
    assert "- Tw.Context.CancellationTokenServiceCollectionExtensions.AddCancellationTokenProvider" in public_api_card
    assert "docs/shared-packages/dotnet/Tw.Core/context/cancellation-token-provider.md" in public_api_card
    assert "包参考文档：" in public_api_card
    assert "使用文档：" not in public_api_card
