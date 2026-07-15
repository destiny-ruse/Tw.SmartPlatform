from __future__ import annotations

import json
import subprocess
from pathlib import Path

import pytest
import yaml

from conftest import make_csproj, write_text
from tw_memory.check import run_check
from tw_memory.generate import run_generate
from tw_memory.hashing import sha256_normalized

CHARTER = """\
schema_version: "1.0.0"
package: Tw.Core
owner: platform-team
responsibility: 跨服务复用的基础原语与无框架依赖工具。
in_scope:
  - 基础值对象
out_of_scope:
  - HTTP 中间件
public_capabilities:
  - Tw.Core.Primitives
dependency_rules:
  forbid:
    - "Microsoft.AspNetCore.*"
  allow: []
"""

ALLOW_CHARTER = CHARTER.replace(
    """\
dependency_rules:
  forbid:
    - "Microsoft.AspNetCore.*"
  allow: []
""",
    """\
dependency_rules:
  forbid: []
  allow:
    - "Newtonsoft.Json"
""",
)

MIGRATION_NAMES = [
    "2026-07-building-blocks-adoption-baseline.md",
    "2026-07-building-blocks-consolidation.md",
]


def seed(repo: Path, *, with_docs: bool = True) -> None:
    write_text(
        repo / ".rules/ai-coding-rules/00-always-load.md",
        "# Always\n\n## Required Formal Standards\n\n"
        "- `docs/engineering-standards/03-project-and-code/coding-standards.md`\n\n"
        "## Execution Requirements\n\n"
        "- Read the formal coding standard before changing source.\n",
    )
    write_text(
        repo / "docs/engineering-standards/03-project-and-code/coding-standards.md",
        "# 通用编码规范\n\n## 目标\n\n清晰。\n",
    )
    pkg = make_csproj(repo, "Foundation", "Tw.Core")
    write_text(pkg / "package-charter.yaml", CHARTER)
    if with_docs:
        write_text(
            repo / "docs/shared-packages/dotnet/Tw.Core/context/cancellation-token-provider.md",
            "# 取消令牌 provider 使用指南\n",
        )
        write_text(
            repo / "docs/shared-packages/dotnet/Tw.Core/README.md",
            "# Tw.Core\n\n- [取消令牌 provider](context/cancellation-token-provider.md)\n",
        )
    for migration_name in MIGRATION_NAMES:
        write_text(
            repo / f"docs/shared-packages/dotnet/migrations/{migration_name}",
            f"# {migration_name}\n",
        )
    write_indexes(repo, ["Tw.Core"])


def write_indexes(repo: Path, packages: list[str], migrations: list[str] | None = None) -> None:
    """Write exact shared-package indexes for a test inventory."""
    package_links = "\n".join(
        f"- [{package}](dotnet/{package}/README.md)" for package in sorted(packages)
    )
    write_text(
        repo / "docs/shared-packages/README.md",
        f"# 共享包\n\n- [.NET 共享包](dotnet/README.md)\n{package_links}\n",
    )
    dotnet_links = "\n".join(
        f"- [{package}]({package}/README.md)" for package in sorted(packages)
    )
    migration_links = "\n".join(
        f"- [{migration}](migrations/{migration})"
        for migration in sorted(MIGRATION_NAMES if migrations is None else migrations)
    )
    write_text(
        repo / "docs/shared-packages/dotnet/README.md",
        f"# .NET 共享包\n\n{dotnet_links}\n{migration_links}\n",
    )


def add_package_docs(repo: Path, package: str) -> None:
    """Create the required package README for a test package."""
    write_text(repo / f"docs/shared-packages/dotnet/{package}/README.md", f"# {package}\n")


def test_check_passes_after_generate(repo: Path) -> None:
    seed(repo)

    assert run_generate(str(repo)) == 0
    assert run_check(str(repo)) == 0


def test_check_fails_without_charter(repo: Path) -> None:
    make_csproj(repo, "Foundation", "Tw.Core")

    assert run_check(str(repo)) == 1


def test_check_fails_when_rules_contain_summary(repo: Path) -> None:
    seed(repo)
    write_text(
        repo / ".rules/ai-coding-rules/tasks/security.md",
        "# Security\n\n## Execution Requirements\n\n"
        "- Never rely on front-end checks as the only authorization control.\n",
    )

    assert run_generate(str(repo)) == 0
    assert run_check(str(repo)) == 1


def test_check_fails_when_source_hash_is_stale(repo: Path) -> None:
    seed(repo)
    assert run_generate(str(repo)) == 0

    write_text(
        repo / "docs/engineering-standards/03-project-and-code/coding-standards.md",
        "# 通用编码规范\n\n## 目标\n\n更新。\n",
    )

    assert run_check(str(repo)) == 1


def test_check_fails_when_dependency_violates_forbid_rule(repo: Path) -> None:
    seed(repo)
    make_csproj(
        repo,
        "Web",
        "Tw.AspNetCore",
        '<Project Sdk="Microsoft.NET.Sdk"><ItemGroup><PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="1.0.0" /></ItemGroup></Project>',
    )
    write_text(
        repo / "backend/dotnet/BuildingBlocks/src/Web/Tw.AspNetCore/package-charter.yaml",
        CHARTER.replace("package: Tw.Core", "package: Tw.AspNetCore").replace(
            "Tw.Core.Primitives", "Tw.AspNetCore.Primitives"
        ),
    )
    add_package_docs(repo, "Tw.AspNetCore")
    write_indexes(repo, ["Tw.AspNetCore", "Tw.Core"])

    assert run_generate(str(repo)) == 0
    assert run_check(str(repo)) == 1


def test_check_fails_when_dependency_violates_allow_rule(repo: Path) -> None:
    seed(repo)
    write_text(
        repo / "backend/dotnet/BuildingBlocks/src/Foundation/Tw.Core/Tw.Core.csproj",
        '<Project Sdk="Microsoft.NET.Sdk"><ItemGroup><PackageReference Include="Serilog" Version="1.0.0" /></ItemGroup></Project>',
    )
    write_text(repo / "backend/dotnet/BuildingBlocks/src/Foundation/Tw.Core/package-charter.yaml", ALLOW_CHARTER)

    assert run_generate(str(repo)) == 0
    assert run_check(str(repo)) == 1


def test_check_passes_when_dependency_matches_allow_rule(repo: Path) -> None:
    seed(repo)
    write_text(
        repo / "backend/dotnet/BuildingBlocks/src/Foundation/Tw.Core/Tw.Core.csproj",
        '<Project Sdk="Microsoft.NET.Sdk"><ItemGroup><PackageReference Include="Newtonsoft.Json" Version="1.0.0" /></ItemGroup></Project>',
    )
    write_text(repo / "backend/dotnet/BuildingBlocks/src/Foundation/Tw.Core/package-charter.yaml", ALLOW_CHARTER)

    assert run_generate(str(repo)) == 0
    assert run_check(str(repo)) == 0


def test_check_fails_when_charter_contains_secret(repo: Path) -> None:
    seed(repo)
    charter_path = repo / "backend/dotnet/BuildingBlocks/src/Foundation/Tw.Core/package-charter.yaml"
    write_text(charter_path, CHARTER.replace("owner: platform-team", "owner: Password=hunter2;"))

    assert run_generate(str(repo)) == 0
    assert run_check(str(repo)) == 1


def test_check_fails_when_public_capabilities_overlap(repo: Path) -> None:
    seed(repo)
    pkg = make_csproj(repo, "Foundation", "Tw.Shared")
    write_text(pkg / "package-charter.yaml", CHARTER.replace("package: Tw.Core", "package: Tw.Shared"))
    add_package_docs(repo, "Tw.Shared")
    write_indexes(repo, ["Tw.Core", "Tw.Shared"])

    assert run_generate(str(repo)) == 0
    assert run_check(str(repo)) == 1


def test_check_allows_distinct_public_capability_prefixes(repo: Path) -> None:
    seed(repo)
    pkg = make_csproj(repo, "Data", "Tw.Data.SqlSugar")
    write_text(
        pkg / "package-charter.yaml",
        CHARTER.replace("package: Tw.Core", "package: Tw.Data.SqlSugar").replace("Tw.Core.Primitives", "Tw.Data.SqlSugar"),
    )
    core_charter = repo / "backend/dotnet/BuildingBlocks/src/Foundation/Tw.Core/package-charter.yaml"
    write_text(core_charter, CHARTER.replace("Tw.Core.Primitives", "Tw.Data"))
    add_package_docs(repo, "Tw.Data.SqlSugar")
    write_indexes(repo, ["Tw.Core", "Tw.Data.SqlSugar"])

    assert run_generate(str(repo)) == 0
    assert run_check(str(repo)) == 0


def test_check_fails_when_package_docs_are_missing(
    repo: Path,
    capsys: pytest.CaptureFixture[str],
) -> None:
    seed(repo, with_docs=False)

    assert run_generate(str(repo)) == 0
    assert run_check(str(repo)) == 1
    assert "missing shared package reference docs" in capsys.readouterr().err


def test_check_reports_malformed_charter_without_crashing(
    repo: Path,
    capsys: pytest.CaptureFixture[str],
) -> None:
    seed(repo)
    charter_path = repo / "backend/dotnet/BuildingBlocks/src/Foundation/Tw.Core/package-charter.yaml"
    write_text(charter_path, "package: [\n")

    assert run_check(str(repo)) == 1
    assert "invalid package charter YAML" in capsys.readouterr().err


def test_check_reports_unsafe_compile_without_crashing(
    repo: Path,
    capsys: pytest.CaptureFixture[str],
) -> None:
    seed(repo)
    assert run_generate(str(repo)) == 0
    project = repo / "backend/dotnet/BuildingBlocks/src/Foundation/Tw.Core/Tw.Core.csproj"
    write_text(
        project,
        '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><EnableDefaultCompileItems>false</EnableDefaultCompileItems></PropertyGroup><ItemGroup><Compile Include="../Outside.cs" /></ItemGroup></Project>',
    )
    write_text(project.parent / "../Outside.cs", "namespace Tw.Outside; public sealed class Outside {}\n")

    assert run_check(str(repo)) == 1
    assert "cannot collect package API" in capsys.readouterr().err


def test_check_reports_external_package_readme_without_crashing(
    repo: Path,
    capsys: pytest.CaptureFixture[str],
) -> None:
    seed(repo)
    assert run_generate(str(repo)) == 0
    readme = repo / "docs/shared-packages/dotnet/Tw.Core/README.md"
    readme.unlink()
    external_readme = write_text(
        repo.parent / f"{repo.name}-external-docs/README.md",
        "# External documentation\n",
    )
    readme.symlink_to(external_readme)

    assert run_check(str(repo)) == 1
    assert "cannot collect package API" in capsys.readouterr().err


def test_check_requires_package_readme_even_when_other_package_docs_exist(repo: Path) -> None:
    seed(repo)
    (repo / "docs/shared-packages/dotnet/Tw.Core/README.md").unlink()

    assert run_generate(str(repo)) == 0
    assert run_check(str(repo)) == 1


def test_check_reports_invalid_project_shape(
    repo: Path,
    capsys: pytest.CaptureFixture[str],
) -> None:
    write_text(
        repo / "backend/dotnet/BuildingBlocks/src/Tw.Flat/Tw.Flat.csproj",
        '<Project Sdk="Microsoft.NET.Sdk" />',
    )

    assert run_check(str(repo)) == 1
    assert "Capability/Package/Package.csproj" in capsys.readouterr().err


def test_check_rejects_discovered_package_missing_from_approved_inventory(repo: Path) -> None:
    seed(repo)
    topology_path = repo / "backend/dotnet/BuildingBlocks/building-blocks-topology.json"
    topology = json.loads(topology_path.read_text(encoding="utf-8"))
    topology["runtimeProjects"] = []
    write_text(topology_path, json.dumps(topology))

    assert run_generate(str(repo)) == 0
    assert run_check(str(repo)) == 1


def test_check_rejects_duplicate_runtime_project_path(
    repo: Path,
    capsys: pytest.CaptureFixture[str],
) -> None:
    seed(repo)
    topology_path = repo / "backend/dotnet/BuildingBlocks/building-blocks-topology.json"
    topology = json.loads(topology_path.read_text(encoding="utf-8"))
    topology["runtimeProjects"].append(dict(topology["runtimeProjects"][0]))
    write_text(topology_path, json.dumps(topology))

    assert run_generate(str(repo)) == 0
    assert run_check(str(repo)) == 1
    assert "duplicate runtime project path Foundation/Tw.Core/Tw.Core.csproj" in capsys.readouterr().err


def test_check_rejects_duplicate_tool_project_path(
    repo: Path,
    capsys: pytest.CaptureFixture[str],
) -> None:
    seed(repo)
    topology_path = repo / "backend/dotnet/BuildingBlocks/building-blocks-topology.json"
    topology = json.loads(topology_path.read_text(encoding="utf-8"))
    tool_path = "backend/dotnet/tools/src/Tw.Cli/Tw.Cli.csproj"
    topology["toolProjects"] = [tool_path, tool_path]
    write_text(topology_path, json.dumps(topology))

    assert run_generate(str(repo)) == 0
    assert run_check(str(repo)) == 1
    assert f"duplicate tool project path {tool_path}" in capsys.readouterr().err


def test_check_rejects_retired_package_id(repo: Path) -> None:
    seed(repo)
    topology_path = repo / "backend/dotnet/BuildingBlocks/building-blocks-topology.json"
    topology = json.loads(topology_path.read_text(encoding="utf-8"))
    topology["retiredPackages"] = [
        {"packageId": "Tw.Core", "runtimeProjectPath": "Foundation/Tw.Core/Tw.Core.csproj"}
    ]
    write_text(topology_path, json.dumps(topology))

    assert run_generate(str(repo)) == 0
    assert run_check(str(repo)) == 1


@pytest.mark.parametrize(
    ("entry", "expected"),
    [
        ("Tw.Old", "must be a mapping"),
        ({}, "missing packageId"),
        ({"packageId": 42}, "packageId must be a non-empty string"),
    ],
)
def test_check_reports_invalid_retired_package_entries(
    repo: Path,
    capsys: pytest.CaptureFixture[str],
    entry: object,
    expected: str,
) -> None:
    seed(repo)
    assert run_generate(str(repo)) == 0
    topology_path = repo / "backend/dotnet/BuildingBlocks/building-blocks-topology.json"
    topology = json.loads(topology_path.read_text(encoding="utf-8"))
    topology["retiredPackages"] = [entry]
    write_text(topology_path, json.dumps(topology))

    assert run_check(str(repo)) == 1
    assert f"retiredPackages[0] {expected}" in capsys.readouterr().err


def test_check_reports_malformed_topology_json(
    repo: Path,
    capsys: pytest.CaptureFixture[str],
) -> None:
    seed(repo)
    assert run_generate(str(repo)) == 0
    topology_path = repo / "backend/dotnet/BuildingBlocks/building-blocks-topology.json"
    write_text(topology_path, "{")

    assert run_check(str(repo)) == 1
    assert "invalid topology JSON" in capsys.readouterr().err


def test_check_rejects_orphan_package_docs_directory(repo: Path) -> None:
    seed(repo)
    write_text(repo / "docs/shared-packages/dotnet/Tw.Old/README.md", "# Tw.Old\n")

    assert run_generate(str(repo)) == 0
    assert run_check(str(repo)) == 1


def test_check_rejects_orphan_file_in_dotnet_docs_root(
    repo: Path,
    capsys: pytest.CaptureFixture[str],
) -> None:
    seed(repo)
    write_text(repo / "docs/shared-packages/dotnet/rogue.md", "# Rogue\n")

    assert run_generate(str(repo)) == 0
    assert run_check(str(repo)) == 1
    assert "rogue.md: orphan .NET shared-package documentation file" in capsys.readouterr().err


def test_check_requires_package_links_in_both_shared_package_indexes(repo: Path) -> None:
    seed(repo)
    write_text(
        repo / "docs/shared-packages/README.md",
        "# 共享包\n\n- [.NET 共享包](dotnet/README.md)\n",
    )

    assert run_generate(str(repo)) == 0
    assert run_check(str(repo)) == 1


def test_check_requires_package_readme_to_link_local_package_docs(repo: Path) -> None:
    seed(repo)
    write_text(repo / "docs/shared-packages/dotnet/Tw.Core/README.md", "# Tw.Core\n")

    assert run_generate(str(repo)) == 0
    assert run_check(str(repo)) == 1


def test_check_requires_all_migration_docs_in_dotnet_index(repo: Path) -> None:
    seed(repo)
    migration_names = ["2026-07-a.md", "2026-07-b.md"]
    for migration_name in migration_names:
        write_text(repo / f"docs/shared-packages/dotnet/migrations/{migration_name}", f"# {migration_name}\n")
    write_indexes(repo, ["Tw.Core"], migrations=[migration_names[0]])

    assert run_generate(str(repo)) == 0
    assert run_check(str(repo)) == 1


def test_check_rejects_extra_migration_doc_even_when_indexed(
    repo: Path,
    capsys: pytest.CaptureFixture[str],
) -> None:
    seed(repo)
    migration_names = [
        "2026-07-building-blocks-adoption-baseline.md",
        "2026-07-building-blocks-consolidation.md",
        "rogue.md",
    ]
    for migration_name in migration_names:
        write_text(repo / f"docs/shared-packages/dotnet/migrations/{migration_name}", f"# {migration_name}\n")
    write_indexes(repo, ["Tw.Core"], migrations=migration_names)

    assert run_generate(str(repo)) == 0
    assert run_check(str(repo)) == 1
    assert "rogue.md: orphan .NET shared-package migration document" in capsys.readouterr().err


def test_check_requires_migration_directory_and_documents(
    repo: Path,
    capsys: pytest.CaptureFixture[str],
) -> None:
    seed(repo)
    migration_names = sorted(MIGRATION_NAMES)
    migrations_root = repo / "docs/shared-packages/dotnet/migrations"
    for migration_name in migration_names:
        write_text(migrations_root / migration_name, f"# {migration_name}\n")
    write_indexes(repo, ["Tw.Core"], migrations=migration_names)
    assert run_generate(str(repo)) == 0
    assert run_check(str(repo)) == 0

    for migration_name in migration_names:
        (migrations_root / migration_name).unlink()
    migrations_root.rmdir()
    write_indexes(repo, ["Tw.Core"], migrations=[])

    assert run_check(str(repo)) == 1
    stderr = capsys.readouterr().err
    assert "missing required .NET shared-package migration document" in stderr
    assert all(migration_name in stderr for migration_name in migration_names)


def test_check_rejects_orphan_package_route(repo: Path) -> None:
    seed(repo)
    assert run_generate(str(repo)) == 0
    route_path = repo / ".tw-memory/routes/packages.generated.yaml"
    route = yaml.safe_load(route_path.read_text(encoding="utf-8"))
    route["packages"]["Tw.Old"] = {
        "path": "backend/dotnet/BuildingBlocks/src/Foundation/Tw.Old",
        "card": ".tw-memory/cards/packages/Tw.Old.generated.md",
        "public_api_card": ".tw-memory/cards/public-apis/Tw.Old.generated.md",
        "source_refs": ["charter:package-charter:Tw.Old"],
    }
    write_text(route_path, yaml.safe_dump(route, sort_keys=True))

    assert run_check(str(repo)) == 1


def test_check_reports_malformed_package_route_yaml(
    repo: Path,
    capsys: pytest.CaptureFixture[str],
) -> None:
    seed(repo)
    assert run_generate(str(repo)) == 0
    route_path = repo / ".tw-memory/routes/packages.generated.yaml"
    write_text(route_path, "packages: [\n")

    assert run_check(str(repo)) == 1
    assert "invalid package route YAML" in capsys.readouterr().err


def test_check_rejects_missing_expected_source_index_entry(repo: Path) -> None:
    seed(repo)
    assert run_generate(str(repo)) == 0
    source_index_path = repo / ".tw-memory/manifest/source-index.generated.json"
    source_index = json.loads(source_index_path.read_text(encoding="utf-8"))
    del source_index["sources"]["charter:package-charter:Tw.Core"]
    write_text(source_index_path, json.dumps(source_index))

    assert run_check(str(repo)) == 1


def test_check_rejects_source_index_entry_bound_to_wrong_path(
    repo: Path,
    capsys: pytest.CaptureFixture[str],
) -> None:
    seed(repo)
    assert run_generate(str(repo)) == 0
    wrong_path = write_text(repo / "wrong.txt", "wrong source\n")
    source_index_path = repo / ".tw-memory/manifest/source-index.generated.json"
    source_index = json.loads(source_index_path.read_text(encoding="utf-8"))
    source = source_index["sources"]["charter:package-charter:Tw.Core"]
    source["path"] = "wrong.txt"
    source["sha256"] = sha256_normalized(wrong_path)
    write_text(source_index_path, json.dumps(source_index))

    assert run_check(str(repo)) == 1
    assert "does not match expected path" in capsys.readouterr().err


@pytest.mark.parametrize(
    ("field", "value"),
    [
        ("source_id", "charter:package-charter:Tw.Wrong"),
        ("source_type", "standard"),
        ("hash_algorithm", "sha512"),
        ("extractor", "wrong:v1"),
    ],
)
def test_check_rejects_source_index_identity_metadata_tampering(
    repo: Path,
    capsys: pytest.CaptureFixture[str],
    field: str,
    value: str,
) -> None:
    seed(repo)
    assert run_generate(str(repo)) == 0
    source_index_path = repo / ".tw-memory/manifest/source-index.generated.json"
    source_index = json.loads(source_index_path.read_text(encoding="utf-8"))
    source = source_index["sources"]["charter:package-charter:Tw.Core"]
    source[field] = value
    write_text(source_index_path, json.dumps(source_index))

    assert run_check(str(repo)) == 1
    assert f"metadata {field}" in capsys.readouterr().err


def test_check_reports_malformed_source_index_json(
    repo: Path,
    capsys: pytest.CaptureFixture[str],
) -> None:
    seed(repo)
    assert run_generate(str(repo)) == 0
    source_index_path = repo / ".tw-memory/manifest/source-index.generated.json"
    write_text(source_index_path, "{")

    assert run_check(str(repo)) == 1
    assert "invalid source index JSON" in capsys.readouterr().err


def test_check_fails_when_generated_public_api_card_is_stale(repo: Path) -> None:
    seed(repo)
    assert run_generate(str(repo)) == 0

    write_text(repo / ".tw-memory/cards/public-apis/Tw.Core.generated.md", "# stale\n")

    assert run_check(str(repo)) == 1


def test_check_fails_when_generated_package_card_is_orphaned(repo: Path) -> None:
    seed(repo)
    assert run_generate(str(repo)) == 0
    write_text(repo / ".tw-memory/cards/packages/Tw.Old.generated.md", "# Package: Tw.Old\n")

    assert run_check(str(repo)) == 1


@pytest.mark.parametrize("cards_directory", ["packages", "public-apis"])
def test_check_rejects_external_generated_card_root_without_reading_it(
    repo: Path,
    cards_directory: str,
    capsys: pytest.CaptureFixture[str],
) -> None:
    seed(repo)
    assert run_generate(str(repo)) == 0
    cards_root = repo / ".tw-memory/cards" / cards_directory
    for card in cards_root.iterdir():
        card.unlink()
    cards_root.rmdir()
    external_cards = repo.parent / f"{repo.name}-external-{cards_directory}"
    external_card = write_text(external_cards / "Tw.Core.generated.md", "external card\n")
    cards_root.symlink_to(external_cards, target_is_directory=True)

    assert run_check(str(repo)) == 1
    assert external_card.read_text(encoding="utf-8") == "external card\n"
    assert "generated output directory must not be a symlink or reparse point" in capsys.readouterr().err


def test_check_fails_for_staged_runtime_path(git_repo: Path) -> None:
    runtime_file = write_text(git_repo / ".tw-memory/runtime/probe.txt", "probe")
    subprocess.run(["git", "add", "-f", str(runtime_file)], cwd=git_repo, check=True)

    assert run_check(str(git_repo), staged=True) == 1


def test_check_fails_for_tracked_codegraph_path(git_repo: Path) -> None:
    codegraph_file = write_text(git_repo / ".codegraph/index.sqlite", "probe")
    subprocess.run(["git", "add", "-f", str(codegraph_file)], cwd=git_repo, check=True)
    subprocess.run(["git", "commit", "-m", "track forbidden path"], cwd=git_repo, check=True)

    assert run_check(str(git_repo)) == 1


def test_check_staged_mode_still_fails_for_tracked_runtime_path(git_repo: Path) -> None:
    runtime_file = write_text(git_repo / ".tw-memory/runtime/probe.txt", "probe")
    subprocess.run(["git", "add", "-f", str(runtime_file)], cwd=git_repo, check=True)
    subprocess.run(["git", "commit", "-m", "track forbidden runtime"], cwd=git_repo, check=True)

    assert run_check(str(git_repo), staged=True) == 1


def test_check_staged_mode_allows_cleanup_deletion_of_tracked_runtime_path(git_repo: Path) -> None:
    runtime_file = write_text(git_repo / ".tw-memory/runtime/probe.txt", "probe")
    subprocess.run(["git", "add", "-f", str(runtime_file)], cwd=git_repo, check=True)
    subprocess.run(["git", "commit", "-m", "track forbidden runtime"], cwd=git_repo, check=True)
    subprocess.run(["git", "rm", "-q", str(runtime_file)], cwd=git_repo, check=True)

    assert run_check(str(git_repo), staged=True) == 0
