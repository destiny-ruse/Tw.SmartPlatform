from __future__ import annotations

import os
from pathlib import Path

from . import GENERATOR_NAME, SCHEMA_VERSION
from .hashing import file_sha256
from .models import GeneratorInfo, SourceRecord
from .paths import relative_posix


EXCLUDED_DIRS = {
    ".git",
    ".tw-memory",
    ".worktrees",
    "node_modules",
    "bin",
    "obj",
    "__pycache__",
}
EXCLUDED_PREFIXES = {
    ("generated", "fts"),
    ("generated", "vector"),
}
MARKDOWN_ROOTS = {"docs", "docs-old", "backend", "frontend", "contracts", "deploy"}
PACKAGE_FILENAMES = {
    "package.json",
    "pnpm-lock.yaml",
    "yarn.lock",
    "package-lock.json",
    "pom.xml",
    "build.gradle",
    "pyproject.toml",
}
LANGUAGE_ROOTS = {"frontend", "contracts", "deploy", "docs"}


class SourceScanner:
    def __init__(self, root: Path):
        self.root = root.resolve()
        self.generator = GeneratorInfo(name=GENERATOR_NAME, version=SCHEMA_VERSION)

    def scan(self) -> list[SourceRecord]:
        records: list[SourceRecord] = []
        for current_root, dirnames, filenames in os.walk(self.root):
            current_path = Path(current_root)
            dirnames[:] = sorted(
                dirname for dirname in dirnames if not self._is_excluded_dir(current_path / dirname)
            )
            for filename in sorted(filenames):
                path = current_path / filename
                if self._is_excluded(path):
                    continue
                record = self._record_for(path)
                if record is not None:
                    records.append(record)

        return sorted(records, key=lambda record: record.source_path)

    def _record_for(self, path: Path) -> SourceRecord | None:
        source_path = relative_posix(self.root, path)
        parts = tuple(source_path.split("/"))
        if not self._is_included(path, parts):
            return None
        return SourceRecord(
            source_path=source_path,
            source_hash=file_sha256(path),
            source_type=self._source_type(path, parts),
            language=self._language(parts),
            framework=self._framework(parts),
            service=self._service(parts),
            generator=self.generator,
        )

    def _is_excluded_dir(self, path: Path) -> bool:
        parts = path.resolve().relative_to(self.root).parts
        if parts[-1] in EXCLUDED_DIRS:
            return True
        return any(
            parts[index : index + len(prefix)] == prefix
            for prefix in EXCLUDED_PREFIXES
            for index in range(0, len(parts) - len(prefix) + 1)
        )

    def _is_excluded(self, path: Path) -> bool:
        parts = path.resolve().relative_to(self.root).parts
        if any(part in EXCLUDED_DIRS for part in parts[:-1]):
            return True
        return any(
            parts[index : index + len(prefix)] == prefix
            for prefix in EXCLUDED_PREFIXES
            for index in range(0, len(parts) - len(prefix) + 1)
        )

    def _is_included(self, path: Path, parts: tuple[str, ...]) -> bool:
        name = path.name
        if self._is_skill_file(name, parts):
            return True
        if len(parts) == 1 and name == "README.md":
            return True
        if name == "README.md" and parts[0] in MARKDOWN_ROOTS:
            return True
        if path.suffix.lower() == ".md" and parts[0] in MARKDOWN_ROOTS:
            return True
        if self._is_package_file(name):
            return True
        return False

    def _is_skill_file(self, name: str, parts: tuple[str, ...]) -> bool:
        return len(parts) >= 4 and parts[0] == ".agents" and parts[1] == "skills" and name == "SKILL.md"

    def _is_package_file(self, name: str) -> bool:
        return (
            name in PACKAGE_FILENAMES
            or name.endswith(".csproj")
            or name.endswith(".sln")
            or (name.startswith("requirements") and name.endswith(".txt"))
        )

    def _source_type(self, path: Path, parts: tuple[str, ...]) -> str:
        name = path.name
        if name == "README.md":
            return "readme"
        if name == "SERVICE.md" and self._service(parts) is not None:
            return "service-directory"
        if name == "SKILL.md":
            return "skill"
        if self._is_package_file(name):
            return "package"
        if parts[0] == "contracts":
            return "spec"
        if parts[0] == "deploy":
            return "standard"
        if parts[0] in {"docs", "docs-old"}:
            return "manual"
        return "source"

    def _language(self, parts: tuple[str, ...]) -> str | None:
        if parts[0] in {"docs", "docs-old"}:
            return "docs"
        if parts[0] in LANGUAGE_ROOTS:
            return parts[0]
        if len(parts) >= 2 and parts[0] == "backend" and parts[1] in {"dotnet", "java", "python"}:
            return parts[1]
        return None

    def _service(self, parts: tuple[str, ...]) -> str | None:
        if len(parts) >= 5 and parts[:3] == ("backend", "dotnet", "Services"):
            return parts[3].lower()
        if len(parts) >= 4 and parts[:2] in {("backend", "java"), ("backend", "python")}:
            return parts[2].lower()
        if len(parts) >= 4 and parts[:2] == ("frontend", "apps"):
            return parts[2].lower()
        return None

    def _framework(self, parts: tuple[str, ...]) -> str | None:
        if len(parts) >= 6 and parts[:4] == ("backend", "dotnet", "BuildingBlocks", "src"):
            return parts[4].lower()
        if len(parts) >= 4 and parts[:2] == ("frontend", "packages"):
            return parts[2].lower()
        return None
