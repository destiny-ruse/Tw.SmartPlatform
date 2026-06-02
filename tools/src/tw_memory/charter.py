from __future__ import annotations

from dataclasses import dataclass
from pathlib import Path
from typing import Any

from tw_memory.yaml_io import load_yaml

PLACEHOLDER_TERMS = (
    "\u540e\u7eed",
    "\u5f85\u5b9a",
    "\u6682\u5b9a",
    "\u89c6\u60c5\u51b5",
    "\u53ef\u80fd",
    "\u5927\u6982",
    "\u5982\u6709\u9700\u8981",
    "\u6309\u9700\u8865\u5145",
    "\u5f85\u8865\u5145",
    "TO" + "DO",
    "T" + "BD",
)
REQUIRED_FIELDS = (
    "schema_version",
    "package",
    "owner",
    "responsibility",
    "in_scope",
    "out_of_scope",
    "public_capabilities",
    "dependency_rules",
)
_STABILITY_VALUES = {"experimental", "stable", "deprecated"}


@dataclass(frozen=True)
class DependencyRules:
    """Dependency allow and forbid rules declared by a package charter."""

    forbid: list[str]
    allow: list[str]


@dataclass(frozen=True)
class Charter:
    """Package governance charter loaded from package-charter.yaml."""

    path: Path
    schema_version: str
    package: str
    owner: str
    responsibility: str
    in_scope: list[str]
    out_of_scope: list[str]
    public_capabilities: list[str]
    dependency_rules: DependencyRules
    stability: str
    compatibility: str | None
    migration_ref: str | None
    raw: dict[str, Any]


def _list(value: Any) -> list[str]:
    return [str(item) for item in value] if isinstance(value, list) else []


def load_charter(path: Path) -> Charter:
    """Load a package charter from YAML while preserving raw validation input."""
    data = load_yaml(path)
    if not isinstance(data, dict):
        data = {}

    rules = data.get("dependency_rules")
    rules_map = rules if isinstance(rules, dict) else {}
    return Charter(
        path=path,
        schema_version=str(data.get("schema_version", "")),
        package=str(data.get("package", "")),
        owner=str(data.get("owner", "")),
        responsibility=str(data.get("responsibility", "")).strip(),
        in_scope=_list(data.get("in_scope")),
        out_of_scope=_list(data.get("out_of_scope")),
        public_capabilities=_list(data.get("public_capabilities")),
        dependency_rules=DependencyRules(
            forbid=_list(rules_map.get("forbid")),
            allow=_list(rules_map.get("allow")),
        ),
        stability=str(data.get("stability", "stable")),
        compatibility=data.get("compatibility") if isinstance(data.get("compatibility"), str) else None,
        migration_ref=data.get("migration_ref") if isinstance(data.get("migration_ref"), str) else None,
        raw=data,
    )


def validate_charter(charter: Charter) -> list[str]:
    """Validate required charter fields and return field-scoped errors."""
    errors: list[str] = []

    for field_name in REQUIRED_FIELDS:
        if not charter.raw.get(field_name):
            errors.append(f"{charter.path}: missing {field_name}")

    for field_name in ("in_scope", "out_of_scope", "public_capabilities"):
        raw_value = charter.raw.get(field_name)
        if not isinstance(raw_value, list) or not getattr(charter, field_name):
            errors.append(f"{charter.path}: {field_name} must be non-empty")

    raw_rules = charter.raw.get("dependency_rules")
    if raw_rules and not isinstance(raw_rules, dict):
        errors.append(f"{charter.path}: dependency_rules must be a mapping")
    elif isinstance(raw_rules, dict):
        if not isinstance(raw_rules.get("forbid", []), list):
            errors.append(f"{charter.path}: dependency_rules.forbid must be a list")
        if not isinstance(raw_rules.get("allow", []), list):
            errors.append(f"{charter.path}: dependency_rules.allow must be a list")

    raw_stability = charter.raw.get("stability", "stable")
    if not isinstance(raw_stability, str) or charter.stability not in _STABILITY_VALUES:
        errors.append(f"{charter.path}: invalid stability {charter.stability!r}")

    text = "\n".join([charter.responsibility, *charter.in_scope, *charter.out_of_scope])
    lowered = text.lower()
    for term in PLACEHOLDER_TERMS:
        if (term.isascii() and term.lower() in lowered) or (not term.isascii() and term in text):
            errors.append(f"{charter.path}: placeholder term {term}")

    return errors
