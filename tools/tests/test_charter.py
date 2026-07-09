from __future__ import annotations

from pathlib import Path

from conftest import write_text
from tw_memory.charter import load_charter, validate_charter

VALID = """\
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


def test_load_valid_charter(tmp_path: Path) -> None:
    path = write_text(tmp_path / "package-charter.yaml", VALID)

    charter = load_charter(path)

    assert charter.path == path
    assert charter.package == "Tw.Core"
    assert charter.public_capabilities == ["Tw.Core.Primitives"]
    assert charter.dependency_rules.forbid == ["Microsoft.AspNetCore.*"]
    assert charter.raw["dependency_rules"]["forbid"] == ["Microsoft.AspNetCore.*"]


def test_validate_accepts_valid_charter(tmp_path: Path) -> None:
    path = write_text(tmp_path / "package-charter.yaml", VALID)

    assert validate_charter(load_charter(path)) == []


def test_validate_rejects_empty_out_of_scope(tmp_path: Path) -> None:
    path = write_text(
        tmp_path / "package-charter.yaml",
        VALID.replace("out_of_scope:\n  - HTTP 中间件", "out_of_scope: []"),
    )

    errors = validate_charter(load_charter(path))

    assert any("out_of_scope" in error for error in errors)


def test_validate_rejects_missing_dependency_rules(tmp_path: Path) -> None:
    path = write_text(
        tmp_path / "package-charter.yaml",
        VALID.replace(
            'dependency_rules:\n  forbid:\n    - "Microsoft.AspNetCore.*"\n  allow: []\n',
            "",
        ),
    )

    errors = validate_charter(load_charter(path))

    assert any("dependency_rules" in error for error in errors)


def test_validate_rejects_future_promise_text(tmp_path: Path) -> None:
    path = write_text(
        tmp_path / "package-charter.yaml",
        VALID.replace("跨服务复用的基础原语与无框架依赖工具。", "职责" + "\u5f85\u5b9a"),
    )

    errors = validate_charter(load_charter(path))

    assert any("placeholder" in error for error in errors)


def test_validate_rejects_non_mapping_dependency_rules(tmp_path: Path) -> None:
    path = write_text(
        tmp_path / "package-charter.yaml",
        VALID.replace(
            'dependency_rules:\n  forbid:\n    - "Microsoft.AspNetCore.*"\n  allow: []',
            "dependency_rules: not-a-map",
        ),
    )

    errors = validate_charter(load_charter(path))

    assert any("dependency_rules must be a mapping" in error for error in errors)


def test_validate_rejects_falsey_non_mapping_dependency_rules(tmp_path: Path) -> None:
    path = write_text(
        tmp_path / "package-charter.yaml",
        VALID.replace(
            'dependency_rules:\n  forbid:\n    - "Microsoft.AspNetCore.*"\n  allow: []',
            "dependency_rules: []",
        ),
    )

    errors = validate_charter(load_charter(path))

    assert any("dependency_rules" in error for error in errors)


def test_validate_rejects_invalid_stability_type(tmp_path: Path) -> None:
    path = write_text(
        tmp_path / "package-charter.yaml",
        VALID + "stability: []\n",
    )

    errors = validate_charter(load_charter(path))

    assert any("invalid stability" in error for error in errors)


def test_validate_rejects_english_responsibility(tmp_path: Path) -> None:
    path = write_text(
        tmp_path / "package-charter.yaml",
        VALID.replace("跨服务复用的基础原语与无框架依赖工具。", "Reusable primitives for services."),
    )

    errors = validate_charter(load_charter(path))

    assert any("responsibility must use Simplified Chinese" in error for error in errors)


def test_validate_rejects_english_scope_items(tmp_path: Path) -> None:
    path = write_text(
        tmp_path / "package-charter.yaml",
        VALID.replace("  - 基础值对象", "  - Value objects").replace("  - HTTP 中间件", "  - HTTP middleware"),
    )

    errors = validate_charter(load_charter(path))

    assert any("in_scope must use Simplified Chinese" in error for error in errors)
    assert any("out_of_scope must use Simplified Chinese" in error for error in errors)


def test_validate_allows_english_public_capability_identifiers(tmp_path: Path) -> None:
    path = write_text(tmp_path / "package-charter.yaml", VALID)

    errors = validate_charter(load_charter(path))

    assert errors == []
