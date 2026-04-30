from __future__ import annotations

import json
import os
from pathlib import Path, PurePosixPath, PureWindowsPath
from typing import Any

from .hashing import file_sha256
from .models import Diagnostic
from .paths import memory_root, relative_posix
from .scanner import SourceScanner


WARNING_SIZE_BYTES = 200 * 1024
ERROR_SIZE_BYTES = 1024 * 1024
FORBIDDEN_SUFFIXES = {".sqlite", ".db", ".parquet", ".bin", ".npy"}
FORBIDDEN_DIRS = {"third-party-docs", "third-party-source"}
RUNTIME_PREFIXES = {
    ("generated", "fts"),
    ("generated", "vector"),
}
RELATED_SOURCE_SUFFIXES = {
    ".cs",
    ".fs",
    ".vb",
    ".ts",
    ".tsx",
    ".js",
    ".jsx",
    ".vue",
    ".java",
    ".py",
    ".go",
    ".rs",
    ".sql",
}
RELATED_EXCLUDED_DIRS = {
    ".git",
    ".tw-memory",
    ".worktrees",
    "bin",
    "obj",
    "node_modules",
    "__pycache__",
}


class MemoryChecker:
    def __init__(self, root: Path):
        self.root = root.resolve()
        self.memory_root = memory_root(self.root)

    def check(self) -> list[Diagnostic]:
        diagnostics: list[Diagnostic] = []
        diagnostics.extend(self._check_required_artifacts())
        source_records = self._source_records(diagnostics)
        diagnostics.extend(self._check_source_freshness(source_records))
        diagnostics.extend(self._check_source_index_matches_scan(source_records))
        diagnostics.extend(self._check_chunk_ranges())
        diagnostics.extend(self._check_manual_freshness(source_records))
        diagnostics.extend(self._check_route_index())
        diagnostics.extend(self._check_memory_safety())
        return diagnostics

    def _check_required_artifacts(self) -> list[Diagnostic]:
        if not self.memory_root.exists():
            return [
                Diagnostic(
                    level="error",
                    code="memory-missing",
                    path=".tw-memory",
                    message="required .tw-memory directory is missing",
                )
            ]

        required_paths = [
            (
                self.memory_root / "source-index",
                "source-index-missing",
                ".tw-memory/source-index",
                "required source index directory is missing",
            ),
            (
                self.memory_root / "generated" / "chunks",
                "chunks-missing",
                ".tw-memory/generated/chunks",
                "required generated chunks directory is missing",
            ),
            (
                self.memory_root / "route-index" / "index.generated.json",
                "route-index-missing",
                ".tw-memory/route-index/index.generated.json",
                "required route index is missing",
            ),
        ]

        diagnostics: list[Diagnostic] = []
        for path, code, diagnostic_path, message in required_paths:
            if not path.exists():
                diagnostics.append(
                    Diagnostic(
                        level="error",
                        code=code,
                        path=diagnostic_path,
                        message=message,
                    )
                )
        return diagnostics

    def _source_records(self, diagnostics: list[Diagnostic]) -> list[tuple[Path, dict[str, Any]]]:
        source_index = self.memory_root / "source-index"
        if not source_index.exists():
            return []

        records: list[tuple[Path, dict[str, Any]]] = []
        for path in sorted(source_index.glob("*.generated.json")):
            payload = self._load_json(path, diagnostics)
            for record in payload.get("sources", []) if isinstance(payload, dict) else []:
                if isinstance(record, dict):
                    records.append((path, record))
        return records

    def _check_source_freshness(self, source_records: list[tuple[Path, dict[str, Any]]]) -> list[Diagnostic]:
        diagnostics: list[Diagnostic] = []
        for _index_path, record in source_records:
            source_path = record.get("source_path")
            expected_hash = record.get("source_hash")
            if not isinstance(source_path, str):
                continue

            path = self._resolve_repo_path(
                source_path,
                code="invalid-source-path",
                diagnostics=diagnostics,
                message="source-index path must stay inside the repository",
            )
            if path is None:
                continue

            if not path.is_file():
                diagnostics.append(
                    Diagnostic(
                        level="error",
                        code="source-missing",
                        path=source_path,
                        message="source-index points to a missing source file",
                    )
                )
                continue

            if isinstance(expected_hash, str) and file_sha256(path) != expected_hash:
                diagnostics.append(
                    Diagnostic(
                        level="error",
                        code="source-stale",
                        path=source_path,
                        message="source-index hash differs from the current source file",
                    )
                )
        return diagnostics

    def _check_source_index_matches_scan(self, source_records: list[tuple[Path, dict[str, Any]]]) -> list[Diagnostic]:
        diagnostics: list[Diagnostic] = []
        indexed_paths: dict[str, int] = {}
        for _index_path, record in source_records:
            source_path = record.get("source_path")
            if isinstance(source_path, str):
                indexed_paths[source_path] = indexed_paths.get(source_path, 0) + 1

        for source_path, count in sorted(indexed_paths.items()):
            if count > 1:
                diagnostics.append(
                    Diagnostic(
                        level="error",
                        code="source-index-duplicate",
                        path=source_path,
                        message="source-index contains duplicate entries for the same source file",
                    )
                )

        try:
            scanned_paths = {record.source_path for record in SourceScanner(self.root).scan()}
        except OSError as exc:
            diagnostics.append(
                Diagnostic(
                    level="error",
                    code="source-scan-failed",
                    path=None,
                    message=f"current source scan failed: {exc}",
                )
            )
            return diagnostics

        for source_path in sorted(scanned_paths - set(indexed_paths)):
            diagnostics.append(
                Diagnostic(
                    level="error",
                    code="source-index-stale",
                    path=source_path,
                    message="source file is not present in generated source-index",
                )
            )

        return diagnostics

    def _check_chunk_ranges(self) -> list[Diagnostic]:
        chunks_root = self.memory_root / "generated" / "chunks"
        if not chunks_root.exists():
            return []

        diagnostics: list[Diagnostic] = []
        line_counts: dict[str, int] = {}
        for chunk_file in sorted(chunks_root.rglob("*.generated.json")):
            payload = self._load_json(chunk_file, diagnostics)
            if not isinstance(payload, dict):
                continue

            payload_source_path = payload.get("source_path")
            chunks = payload.get("chunks", [])
            if not isinstance(chunks, list):
                continue

            for chunk in chunks:
                if not isinstance(chunk, dict):
                    continue
                source_path = chunk.get("source_path", payload_source_path)
                if not isinstance(source_path, str):
                    continue
                source = self._resolve_repo_path(
                    source_path,
                    code="invalid-chunk-path",
                    diagnostics=diagnostics,
                    message="chunk source path must stay inside the repository",
                )
                if source is None:
                    continue
                if not source.is_file():
                    continue
                if source_path not in line_counts:
                    try:
                        line_counts[source_path] = len(source.read_text(encoding="utf-8").splitlines())
                    except (OSError, UnicodeDecodeError):
                        continue

                start_line = chunk.get("start_line")
                end_line = chunk.get("end_line")
                if not self._valid_line_range(start_line, end_line, line_counts[source_path]):
                    diagnostics.append(
                        Diagnostic(
                            level="error",
                            code="chunk-range-invalid",
                            path=relative_posix(self.root, chunk_file),
                            message=f"chunk range is outside {source_path}",
                        )
                    )
        return diagnostics

    def _check_manual_freshness(self, source_records: list[tuple[Path, dict[str, Any]]]) -> list[Diagnostic]:
        diagnostics: list[Diagnostic] = []
        for index_path, record in source_records:
            if record.get("source_type") not in {"readme", "manual"} or record.get("framework") is None:
                continue
            source_path = record.get("source_path")
            if not isinstance(source_path, str):
                continue

            manual = self._resolve_repo_path(source_path)
            if manual is None:
                continue
            if not manual.is_file():
                continue
            index_mtime = index_path.stat().st_mtime
            newer_source = self._newer_related_package_source(manual.parent, manual, index_mtime)
            if newer_source is not None:
                diagnostics.append(
                    Diagnostic(
                        level="warning",
                        code="manual-maybe-stale",
                        path=source_path,
                        message=f"related package source is newer than {relative_posix(self.root, index_path)}",
                    )
                )
        return diagnostics

    def _check_route_index(self) -> list[Diagnostic]:
        index_path = self.memory_root / "route-index" / "index.generated.json"
        if not index_path.exists():
            return []

        diagnostics: list[Diagnostic] = []
        payload = self._load_json(index_path, diagnostics)
        if not isinstance(payload, dict):
            return diagnostics

        shards = payload.get("shards", [])
        if not isinstance(shards, list):
            return diagnostics

        for shard in shards:
            if not isinstance(shard, dict):
                continue
            shard_path = shard.get("path")
            if not isinstance(shard_path, str):
                continue
            path = self._resolve_memory_path(
                shard_path,
                code="broken-route-path",
                diagnostics=diagnostics,
                message="route-index shard path points outside .tw-memory",
            )
            if path is None:
                continue
            if not self._is_under(path, self.memory_root) or not path.is_file():
                diagnostics.append(
                    Diagnostic(
                        level="error",
                        code="broken-route-path",
                        path=f".tw-memory/{shard_path}",
                        message="route-index shard path points to a missing generated file",
                    )
                )
        return diagnostics

    def _check_memory_safety(self) -> list[Diagnostic]:
        if not self.memory_root.exists():
            return []

        diagnostics: list[Diagnostic] = []
        for path in sorted(self.memory_root.rglob("*")):
            if not path.is_file():
                continue
            relative_parts = path.relative_to(self.memory_root).parts
            relative_path = relative_posix(self.root, path)
            size = path.stat().st_size
            if self._is_runtime_cache_file(relative_parts):
                continue

            if self._is_forbidden_memory_file(path, relative_parts, size):
                diagnostics.append(
                    Diagnostic(
                        level="error",
                        code="forbidden-memory-file",
                        path=relative_path,
                        message="file is not allowed in committable .tw-memory content",
                    )
                )

            route_root_index = relative_parts == ("route-index", "index.generated.json")
            if route_root_index:
                diagnostics.extend(self._large_file_diagnostics(path, relative_path, "large-route-index"))
            else:
                diagnostics.extend(self._large_file_diagnostics(path, relative_path, "large-memory-file"))
        return diagnostics

    def _large_file_diagnostics(self, path: Path, relative_path: str, code: str) -> list[Diagnostic]:
        size = path.stat().st_size
        if size <= WARNING_SIZE_BYTES:
            return []

        level = "error" if size > ERROR_SIZE_BYTES else "warning"
        limit = "1 MB" if level == "error" else "200 KB"
        return [
            Diagnostic(
                level=level,
                code=code,
                path=relative_path,
                message=f"file is larger than {limit}",
            )
        ]

    def _is_forbidden_memory_file(self, path: Path, relative_parts: tuple[str, ...], size: int) -> bool:
        if path.suffix.lower() in FORBIDDEN_SUFFIXES:
            return True
        if any(part in FORBIDDEN_DIRS for part in relative_parts):
            return True
        if size > ERROR_SIZE_BYTES:
            return True
        return False

    def _is_runtime_cache_file(self, relative_parts: tuple[str, ...]) -> bool:
        return any(relative_parts[: len(prefix)] == prefix for prefix in RUNTIME_PREFIXES)

    def _newer_related_package_source(self, directory: Path, manual: Path, index_mtime: float) -> Path | None:
        if not directory.exists():
            return None

        for current_root, dirnames, filenames in os.walk(directory):
            current_path = Path(current_root)
            dirnames[:] = sorted(dirname for dirname in dirnames if dirname not in RELATED_EXCLUDED_DIRS)
            for filename in sorted(filenames):
                path = current_path / filename
                if path == manual or path.suffix.lower() not in RELATED_SOURCE_SUFFIXES:
                    continue
                if path.stat().st_mtime > index_mtime:
                    return path
        return None

    def _valid_line_range(self, start_line: Any, end_line: Any, line_count: int) -> bool:
        if not isinstance(start_line, int) or not isinstance(end_line, int):
            return False
        return 1 <= start_line <= end_line <= line_count

    def _load_json(self, path: Path, diagnostics: list[Diagnostic]) -> Any:
        try:
            return json.loads(path.read_text(encoding="utf-8"))
        except (OSError, UnicodeDecodeError, json.JSONDecodeError) as exc:
            diagnostics.append(
                Diagnostic(
                    level="error",
                    code="invalid-json",
                    path=relative_posix(self.root, path),
                    message=f"generated JSON could not be read: {exc}",
                )
            )
            return {}

    def _resolve_repo_path(
        self,
        source_path: str,
        *,
        code: str | None = None,
        diagnostics: list[Diagnostic] | None = None,
        message: str = "path must stay inside the repository",
    ) -> Path | None:
        return self._resolve_safe_path(source_path, self.root, code=code, diagnostics=diagnostics, message=message)

    def _resolve_memory_path(
        self,
        source_path: str,
        *,
        code: str | None = None,
        diagnostics: list[Diagnostic] | None = None,
        message: str = "path must stay inside .tw-memory",
    ) -> Path | None:
        return self._resolve_safe_path(
            source_path,
            self.memory_root,
            code=code,
            diagnostics=diagnostics,
            message=message,
            display_prefix=".tw-memory/",
        )

    def _resolve_safe_path(
        self,
        source_path: str,
        base: Path,
        *,
        code: str | None,
        diagnostics: list[Diagnostic] | None,
        message: str,
        display_prefix: str = "",
    ) -> Path | None:
        if self._is_unsafe_relative_path(source_path):
            if diagnostics is not None and code is not None:
                diagnostics.append(
                    Diagnostic(
                        level="error",
                        code=code,
                        path=f"{display_prefix}{source_path}",
                        message=message,
                    )
                )
            return None

        resolved = (base / source_path).resolve()
        if not self._is_under(resolved, base):
            if diagnostics is not None and code is not None:
                diagnostics.append(
                    Diagnostic(
                        level="error",
                        code=code,
                        path=f"{display_prefix}{source_path}",
                        message=message,
                    )
                )
            return None
        return resolved

    def _is_unsafe_relative_path(self, source_path: str) -> bool:
        if not source_path:
            return True

        posix = PurePosixPath(source_path)
        windows = PureWindowsPath(source_path)
        if posix.is_absolute() or windows.is_absolute() or windows.drive:
            return True
        return ".." in posix.parts or ".." in windows.parts

    def _is_under(self, path: Path, parent: Path) -> bool:
        try:
            path.relative_to(parent.resolve())
        except ValueError:
            return False
        return True
