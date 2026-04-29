from __future__ import annotations

from pathlib import Path


def repo_root(start: Path | None = None) -> Path:
    current = (start or Path.cwd()).resolve()
    if current.is_file():
        current = current.parent

    for candidate in (current, *current.parents):
        if (candidate / "README.md").is_file() and (candidate / "tools").is_dir():
            return candidate

    raise FileNotFoundError(f"Could not find repository root from {current}")


def memory_root(root: Path) -> Path:
    return root / ".tw-memory"


def relative_posix(root: Path, path: Path) -> str:
    return path.resolve().relative_to(root.resolve()).as_posix()
