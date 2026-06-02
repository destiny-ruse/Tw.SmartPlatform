from __future__ import annotations

from pathlib import Path

from conftest import write_text
from tw_memory.cards import render_package_card, render_public_api_card
from tw_memory.charter import load_charter

CHARTER = """schema_version: memory.charter.v1
package: Tw.Core
owner: platform-team
responsibility: 跨服务复用的基础原语与无框架依赖工具。
in_scope:
  - 基础类型
out_of_scope:
  - Web 框架集成
public_capabilities:
  - Tw.Core.Primitives
dependency_rules:
  forbid:
    - Microsoft.AspNetCore.*
  allow:
    - System.*
stability: stable
compatibility: 保持跨服务二进制兼容
migration_ref: docs/migrations/tw-core.md
"""


def test_render_package_card_uses_fixed_slots(tmp_path: Path) -> None:
    charter_path = write_text(
        tmp_path / "package-charter.yaml",
        CHARTER,
    )
    charter = load_charter(charter_path)

    card = render_package_card(
        "Tw.Core",
        "backend/dotnet/BuildingBlocks/src/Tw.Core",
        charter,
        ["manual:package-charter:Tw.Core"],
    )

    assert "标识：Tw.Core / backend/dotnet/BuildingBlocks/src/Tw.Core / platform-team" in card
    assert "职责：跨服务复用的基础原语与无框架依赖工具。" in card
    assert "不适用范围：" in card
    assert "source_refs:" in card


def test_render_public_api_card_includes_public_capability(tmp_path: Path) -> None:
    charter_path = write_text(
        tmp_path / "package-charter.yaml",
        CHARTER,
    )
    charter = load_charter(charter_path)

    card = render_public_api_card(
        "Tw.Core",
        "backend/dotnet/BuildingBlocks/src/Tw.Core",
        charter,
        ["manual:package-charter:Tw.Core"],
    )

    assert "- Tw.Core.Primitives" in card
