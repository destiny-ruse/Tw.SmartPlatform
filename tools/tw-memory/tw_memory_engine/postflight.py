from __future__ import annotations

from pathlib import Path

from .scanner import EXCLUDED_DIRS, GENERATED_OUTPUT_DIRS, GENERATED_OUTPUT_PREFIXES, SOURCE_SUFFIXES


PACKAGE_FILENAMES = {
    "package.json",
    "pnpm-lock.yaml",
    "yarn.lock",
    "package-lock.json",
    "pom.xml",
    "build.gradle",
    "pyproject.toml",
}


class PostflightRunner:
    def __init__(self, root: Path):
        self.root = root.resolve()

    def run(self, changed_files: list[str]) -> dict[str, object]:
        files = [_normalize_path(path) for path in changed_files]
        files = [path for path in files if path]

        memory_affecting = [path for path in files if _is_memory_affecting(path)]
        memory_affecting_set = set(memory_affecting)
        review_required = [
            path for path in files if path not in memory_affecting_set and _is_review_required(path)
        ]
        ordinary = [
            path
            for path in files
            if path not in set(memory_affecting) and path not in set(review_required)
        ]

        if memory_affecting:
            actions = ["generate", "check"]
            reason = "memory-affecting files changed"
        elif review_required:
            actions = ["review-manual-sync", "postflight-again"]
            reason = "review-required files changed without direct memory files"
        else:
            actions = []
            reason = "ordinary business code changes"

        return {
            "memory_affecting_files": memory_affecting,
            "review_required_files": review_required,
            "ordinary_files": ordinary,
            "actions": actions,
            "reason": reason,
        }


def _normalize_path(path: str) -> str:
    normalized = path.replace("\\", "/").strip()
    while normalized.startswith("./"):
        normalized = normalized[2:]
    return normalized


def _is_memory_affecting(path: str) -> bool:
    parts = tuple(part for part in path.split("/") if part)
    if not parts:
        return False
    if parts[0] == "docs":
        return True
    if parts[-1] == "README.md":
        return True
    if _is_package_file(parts[-1]):
        return True
    if _is_controlled_source_file(parts):
        return True
    if path == ".tw-memory/taxonomy.yaml":
        return True
    if path.startswith(".tw-memory/graph/"):
        return True
    return False


def _is_package_file(name: str) -> bool:
    return (
        name in PACKAGE_FILENAMES
        or name.endswith(".csproj")
        or name.endswith(".sln")
        or (name.startswith("requirements") and name.endswith(".txt"))
    )


def _is_review_required(path: str) -> bool:
    parts = tuple(part for part in path.split("/") if part)
    if _is_excluded_source_path(parts):
        return False
    if len(parts) >= 5 and parts[:4] == ("backend", "dotnet", "BuildingBlocks", "src"):
        return parts[-1].endswith(".cs")
    if len(parts) >= 5 and parts[:3] == ("backend", "dotnet", "Services"):
        return parts[-1].endswith(".cs")
    if len(parts) >= 3 and parts[:2] == ("frontend", "packages"):
        return True
    if len(parts) >= 4 and parts[:2] == ("frontend", "apps"):
        return True
    if len(parts) >= 4 and parts[:2] == ("backend", "java") and "src" in parts[2:-1]:
        return True
    if len(parts) >= 3 and parts[:2] == ("backend", "python"):
        return True
    return False


def _is_controlled_source_file(parts: tuple[str, ...]) -> bool:
    if not parts or Path(parts[-1]).suffix.lower() not in SOURCE_SUFFIXES:
        return False
    if _is_excluded_source_path(parts):
        return False
    if len(parts) >= 5 and parts[:4] == ("backend", "dotnet", "BuildingBlocks", "src"):
        return True
    if len(parts) >= 5 and parts[:3] == ("backend", "dotnet", "Services"):
        return True
    if len(parts) >= 3 and parts[:2] in {("backend", "java"), ("backend", "python")}:
        return True
    if len(parts) >= 4 and parts[:2] in {("frontend", "packages"), ("frontend", "apps")}:
        return True
    return False


def _is_excluded_source_path(parts: tuple[str, ...]) -> bool:
    if any(part in EXCLUDED_DIRS for part in parts[:-1]):
        return True
    return _is_generated_output_path(parts[:-1])


def _is_generated_output_path(parts: tuple[str, ...]) -> bool:
    has_generated_output_dir = any(part in GENERATED_OUTPUT_DIRS for part in parts)
    has_generated_output_prefix = any(
        parts[index : index + len(prefix)] == prefix
        for prefix in GENERATED_OUTPUT_PREFIXES
        for index in range(0, len(parts) - len(prefix) + 1)
    )
    if not has_generated_output_dir and not has_generated_output_prefix:
        return False
    if not parts or parts[0] == "docs":
        return False
    if len(parts) >= 5 and parts[:4] == ("backend", "dotnet", "BuildingBlocks", "src"):
        return True
    if len(parts) >= 5 and parts[:3] == ("backend", "dotnet", "Services"):
        return True
    if len(parts) >= 3 and parts[:2] in {("backend", "java"), ("backend", "python")}:
        return True
    if len(parts) >= 4 and parts[:2] in {("frontend", "packages"), ("frontend", "apps")}:
        return True
    return False
