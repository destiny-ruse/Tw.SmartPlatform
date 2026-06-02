from __future__ import annotations

from pathlib import Path


def existing_files(paths: list[Path]) -> list[Path]:
    """Return existing file paths in deterministic order."""
    return [path for path in paths if path.exists()]


def discover_contract_files(root: Path) -> list[Path]:
    """Discover committed API contract files."""
    contracts_root = root / "contracts"
    if not contracts_root.exists():
        return []
    return sorted(path for path in contracts_root.rglob("*") if path.is_file())


def discover_skill_files(root: Path) -> list[Path]:
    """Discover repository skill instruction files."""
    skills_root = root / ".agents/skills"
    if not skills_root.exists():
        return []
    return sorted(skills_root.rglob("SKILL.md"))
