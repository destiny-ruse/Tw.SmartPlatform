from __future__ import annotations

import subprocess
from pathlib import Path

from conftest import make_csproj, write_text
from tw_memory.check import run_check
from tw_memory.generate import run_generate

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
    pkg = make_csproj(repo, "Tw.Core")
    write_text(pkg / "package-charter.yaml", CHARTER)
    if with_docs:
        write_text(
            repo / "docs/shared-packages/dotnet/Tw.Core/context/cancellation-token-provider.md",
            "# 取消令牌 provider 使用指南\n",
        )


def test_check_passes_after_generate(repo: Path) -> None:
    seed(repo)

    assert run_generate(str(repo)) == 0
    assert run_check(str(repo)) == 0


def test_check_fails_without_charter(repo: Path) -> None:
    make_csproj(repo, "Tw.Core")

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
        "Tw.AspNetCore",
        '<Project Sdk="Microsoft.NET.Sdk"><ItemGroup><PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="1.0.0" /></ItemGroup></Project>',
    )
    write_text(
        repo / "backend/dotnet/BuildingBlocks/src/Tw.AspNetCore/package-charter.yaml",
        CHARTER.replace("package: Tw.Core", "package: Tw.AspNetCore").replace(
            "Tw.Core.Primitives", "Tw.AspNetCore.Primitives"
        ),
    )

    assert run_generate(str(repo)) == 0
    assert run_check(str(repo)) == 1


def test_check_fails_when_dependency_violates_allow_rule(repo: Path) -> None:
    seed(repo)
    make_csproj(
        repo,
        "Tw.Core",
        '<Project Sdk="Microsoft.NET.Sdk"><ItemGroup><PackageReference Include="Serilog" Version="1.0.0" /></ItemGroup></Project>',
    )
    write_text(repo / "backend/dotnet/BuildingBlocks/src/Tw.Core/package-charter.yaml", ALLOW_CHARTER)

    assert run_generate(str(repo)) == 0
    assert run_check(str(repo)) == 1


def test_check_passes_when_dependency_matches_allow_rule(repo: Path) -> None:
    seed(repo)
    make_csproj(
        repo,
        "Tw.Core",
        '<Project Sdk="Microsoft.NET.Sdk"><ItemGroup><PackageReference Include="Newtonsoft.Json" Version="1.0.0" /></ItemGroup></Project>',
    )
    write_text(repo / "backend/dotnet/BuildingBlocks/src/Tw.Core/package-charter.yaml", ALLOW_CHARTER)

    assert run_generate(str(repo)) == 0
    assert run_check(str(repo)) == 0


def test_check_fails_when_charter_contains_secret(repo: Path) -> None:
    seed(repo)
    charter_path = repo / "backend/dotnet/BuildingBlocks/src/Tw.Core/package-charter.yaml"
    write_text(charter_path, CHARTER.replace("owner: platform-team", "owner: Password=hunter2;"))

    assert run_generate(str(repo)) == 0
    assert run_check(str(repo)) == 1


def test_check_fails_when_public_capabilities_overlap(repo: Path) -> None:
    seed(repo)
    pkg = make_csproj(repo, "Tw.Shared")
    write_text(pkg / "package-charter.yaml", CHARTER.replace("package: Tw.Core", "package: Tw.Shared"))

    assert run_generate(str(repo)) == 0
    assert run_check(str(repo)) == 1


def test_check_fails_when_public_capability_prefix_overlaps(repo: Path) -> None:
    seed(repo)
    pkg = make_csproj(repo, "Tw.Shared")
    write_text(
        pkg / "package-charter.yaml",
        CHARTER.replace("package: Tw.Core", "package: Tw.Shared").replace("Tw.Core.Primitives", "Tw.Core"),
    )

    assert run_generate(str(repo)) == 0
    assert run_check(str(repo)) == 1


def test_check_fails_when_usage_docs_are_missing(repo: Path) -> None:
    seed(repo, with_docs=False)

    assert run_generate(str(repo)) == 0
    assert run_check(str(repo)) == 1


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
