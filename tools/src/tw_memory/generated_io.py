from __future__ import annotations

from pathlib import Path


def repo_relative(root: Path, path: Path) -> str:
    """Return a repository-relative path with slash separators."""
    return path.resolve().relative_to(root.resolve()).as_posix()


def assert_generated_memory_path(root: Path, path: Path) -> None:
    """Ensure a .tw-memory path is a generated commit-layer file."""
    rel = repo_relative(root, path)
    if not rel.startswith(".tw-memory/"):
        raise ValueError(f"{rel} is outside .tw-memory")
    if "/runtime/" in f"/{rel}/":
        raise ValueError(f"{rel} is runtime state, not generated commit memory")
    if not (
        rel.endswith(".generated.yaml")
        or rel.endswith(".generated.json")
        or rel.endswith(".generated.md")
    ):
        raise ValueError(f"{rel} is not a generated memory file")


def write_generated_text(root: Path, path: Path, content: str) -> None:
    """Write generated text with LF newlines after checking path boundaries."""
    assert_generated_memory_path(root, path)
    path.parent.mkdir(parents=True, exist_ok=True)
    normalized = content.replace("\r\n", "\n").replace("\r", "\n")
    path.write_text(normalized, encoding="utf-8", newline="\n")
