from __future__ import annotations

import json
from pathlib import Path, PurePosixPath, PureWindowsPath
from typing import Any, Iterable

from .hashing import file_sha256
from .paths import memory_root


class ChunkReader:
    def __init__(self, root: Path):
        self.root = root.resolve()
        self.memory_root = memory_root(self.root)

    def read(self, chunk_id: str, with_neighbors: int = 0) -> dict[str, Any]:
        located = self._locate_chunk(chunk_id)
        if located is None:
            return {"chunk_id": chunk_id, "stale": True, "error": "chunk-not-found"}

        chunk, source_chunks = located
        source_path = str(chunk["source_path"])
        source = self._resolve_source(source_path)
        if source is None or not source.is_file():
            return {
                "chunk_id": chunk_id,
                "stale": True,
                "error": "source-stale",
                "action": "run generate/check",
            }

        expected_hash = chunk.get("source_hash")
        try:
            current_hash = file_sha256(source)
        except OSError:
            return {
                "chunk_id": chunk_id,
                "stale": True,
                "error": "source-stale",
                "action": "run generate/check",
            }

        if current_hash != expected_hash:
            return {
                "chunk_id": chunk_id,
                "stale": True,
                "error": "source-stale",
                "action": "run generate/check",
            }

        selected_chunks = self._expand_neighbors(chunk, source_chunks, max(0, with_neighbors))
        start_line = min(int(item["start_line"]) for item in selected_chunks)
        end_line = max(int(item["end_line"]) for item in selected_chunks)

        try:
            lines = source.read_text(encoding="utf-8").splitlines()
        except (OSError, UnicodeDecodeError):
            return {
                "chunk_id": chunk_id,
                "stale": True,
                "error": "source-stale",
                "action": "run generate/check",
            }

        text = "\n".join(lines[start_line - 1 : end_line])
        return {
            "chunk_id": chunk_id,
            "source_path": source_path,
            "source_hash": current_hash,
            "start_line": start_line,
            "end_line": end_line,
            "heading": chunk.get("heading"),
            "text": text,
            "stale": False,
        }

    def format(self, evidence: dict[str, Any], output_format: str = "evidence") -> str:
        if evidence.get("stale"):
            error = evidence.get("error", "stale")
            action = evidence.get("action")
            return f"{evidence.get('chunk_id', '-')}: {error}" + (f" ({action})" if action else "")

        if output_format not in {"evidence", "full-section"}:
            raise ValueError(f"unsupported read format: {output_format}")

        location = f"{evidence['source_path']}:{evidence['start_line']}-{evidence['end_line']}"
        return f"{evidence['chunk_id']} {location}\n{evidence['text']}"

    def _locate_chunk(self, chunk_id: str) -> tuple[dict[str, Any], list[dict[str, Any]]] | None:
        for source_chunks in self._chunk_groups():
            for chunk in source_chunks:
                if chunk.get("chunk_id") == chunk_id:
                    return chunk, source_chunks
        return None

    def _chunk_groups(self) -> Iterable[list[dict[str, Any]]]:
        chunks_root = self.memory_root / "generated" / "chunks"
        if not chunks_root.exists():
            return

        for path in sorted(chunks_root.rglob("*.generated.json")):
            payload = self._load_json(path)
            if not isinstance(payload, dict):
                continue
            payload_source_path = payload.get("source_path")
            chunks = payload.get("chunks", [])
            if not isinstance(chunks, list):
                continue

            normalized_chunks: list[dict[str, Any]] = []
            for chunk in chunks:
                if not isinstance(chunk, dict):
                    continue
                normalized = dict(chunk)
                if "source_path" not in normalized and isinstance(payload_source_path, str):
                    normalized["source_path"] = payload_source_path
                if self._is_valid_chunk(normalized):
                    normalized_chunks.append(normalized)

            if normalized_chunks:
                yield sorted(normalized_chunks, key=lambda item: (int(item["start_line"]), str(item["chunk_id"])))

    def _expand_neighbors(
        self,
        selected: dict[str, Any],
        source_chunks: list[dict[str, Any]],
        with_neighbors: int,
    ) -> list[dict[str, Any]]:
        index = source_chunks.index(selected)
        start = max(0, index - with_neighbors)
        end = min(len(source_chunks), index + with_neighbors + 1)
        return source_chunks[start:end]

    def _is_valid_chunk(self, chunk: dict[str, Any]) -> bool:
        return (
            isinstance(chunk.get("chunk_id"), str)
            and isinstance(chunk.get("source_path"), str)
            and isinstance(chunk.get("source_hash"), str)
            and isinstance(chunk.get("start_line"), int)
            and isinstance(chunk.get("end_line"), int)
        )

    def _resolve_source(self, source_path: str) -> Path | None:
        if self._is_unsafe_relative_path(source_path):
            return None
        resolved = (self.root / source_path).resolve()
        if not self._is_under(resolved, self.root):
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

    def _load_json(self, path: Path) -> Any:
        try:
            return json.loads(path.read_text(encoding="utf-8"))
        except (OSError, UnicodeDecodeError, json.JSONDecodeError):
            return {}
