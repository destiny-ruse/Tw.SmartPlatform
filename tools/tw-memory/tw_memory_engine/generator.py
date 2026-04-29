from __future__ import annotations

import hashlib
import json
import shutil
from dataclasses import dataclass, replace
from pathlib import Path
from typing import Any

from . import SCHEMA_VERSION
from .chunking import MarkdownChunker
from .hashing import tree_hash
from .models import ChunkRecord, Diagnostic, SourceRecord
from .paths import memory_root
from .scanner import SourceScanner
from .semantic import extract_keywords, relations_for_source, summarize_chunk


README_TEXT = """# TW Memory

`.tw-memory` is the AI memory layer for this repository. It stores generated source indexes, relationship graphs, route indexes, chunk metadata, and search synchronization metadata.

It is not a company documentation directory. Do not place manuals, standards bodies, source copies, third-party documentation, chat logs, secrets, SQLite files, vector caches, or web captures here.

Use the CLI:

```powershell
python tools\\tw-memory\\tw_memory.py generate
python tools\\tw-memory\\tw_memory.py check
```
"""

TAXONOMY_TEXT = """schema_version: "1.0.0"
languages:
  - frontend
  - dotnet
  - java
  - python
  - contracts
  - deploy
  - docs
source_types:
  - spec
  - standard
  - manual
  - readme
  - source
  - package
  - service-directory
  - skill
memory_layers:
  - source-index
  - graph
  - route-index
"""

VECTOR_BACKENDS_TEXT = """schema_version: "1.0.0"
default_backend: fts
vector_backends:
  aliyun:
    enabled: false
  tencent:
    enabled: false
  volcengine:
    enabled: false
  self-hosted:
    enabled: false
"""
MAX_ROUTE_SHARD_CHUNKS = 100


@dataclass(frozen=True)
class GenerationResult:
    generated_paths: list[str]
    errors: list[Diagnostic]

    @property
    def generated_count(self) -> int:
        return len(self.generated_paths)

    def to_json(self) -> dict[str, Any]:
        return {
            "generated_paths": self.generated_paths,
            "generated_count": self.generated_count,
            "diagnostics": [error.to_json() for error in self.errors],
        }


class MemoryGenerator:
    def __init__(self, root: Path):
        self.root = root.resolve()
        self.memory_root = memory_root(self.root)

    def generate(self) -> GenerationResult:
        generated_paths: list[str] = []
        errors: list[Diagnostic] = []

        sources = SourceScanner(self.root).scan()
        repo_hash = tree_hash(self.root, [record.source_path for record in sources])

        self._prune_generated_artifacts()
        generated_paths.extend(self._write_static_files())
        self._ensure_runtime_dirs()
        chunks = self._chunks_for_sources(sources, errors)

        generated_paths.extend(self._write_source_indexes(sources))
        generated_paths.extend(self._write_chunk_files(chunks))
        generated_paths.extend(self._write_graphs(sources, chunks))
        generated_paths.extend(self._write_route_indexes(repo_hash, chunks))

        return GenerationResult(generated_paths=sorted(generated_paths), errors=errors)

    def _write_static_files(self) -> list[str]:
        return [
            self._write_text("README.md", README_TEXT),
            self._write_text("taxonomy.yaml", TAXONOMY_TEXT),
            self._write_text("adapters/vector-backends.yaml", VECTOR_BACKENDS_TEXT),
        ]

    def _ensure_runtime_dirs(self) -> None:
        for source_path in ("generated/fts", "generated/vector"):
            (self.memory_root / source_path).mkdir(parents=True, exist_ok=True)

    def _prune_generated_artifacts(self) -> None:
        for source_path in ("source-index", "graph", "route-index", "generated/chunks"):
            path = self.memory_root / source_path
            if path.exists():
                shutil.rmtree(path)

    def _chunks_for_sources(self, sources: list[SourceRecord], errors: list[Diagnostic]) -> list[ChunkRecord]:
        chunks: list[ChunkRecord] = []
        base_ids = self._base_ids_for_sources(sources)
        for record in sources:
            path = self.root / record.source_path
            try:
                source_lines = path.read_text(encoding="utf-8").splitlines()
                relations = relations_for_source(record)
                for chunk in MarkdownChunker(path, base_ids[record.source_path], source_lines).chunk():
                    chunk_lines = source_lines[chunk.start_line - 1 : chunk.end_line]
                    chunks.append(
                        replace(
                            chunk,
                            source_path=record.source_path,
                            summary=summarize_chunk(
                                record.source_path,
                                chunk.heading,
                                chunk_lines,
                                chunk.start_line,
                                chunk.end_line,
                            ),
                            keywords=extract_keywords(record.source_path, chunk.heading, chunk_lines),
                            relations=relations,
                        )
                    )
            except UnicodeDecodeError as exc:
                errors.append(
                    Diagnostic(
                        level="error",
                        code="source-decode-failed",
                        message=str(exc),
                        path=record.source_path,
                    )
                )
            except OSError as exc:
                errors.append(
                    Diagnostic(
                        level="error",
                        code="source-read-failed",
                        message=str(exc),
                        path=record.source_path,
                    )
                )
        return sorted(chunks, key=lambda chunk: chunk.chunk_id)

    def _write_source_indexes(self, sources: list[SourceRecord]) -> list[str]:
        buckets = {
            "docs": [record for record in sources if record.source_type != "package" and record.source_type != "source"],
            "code": [record for record in sources if record.source_type == "source"],
            "packages": [record for record in sources if record.source_type == "package"],
        }
        generated_paths: list[str] = []
        for name, records in buckets.items():
            payload = {
                "schema_version": SCHEMA_VERSION,
                "sources": [record.to_json() for record in sorted(records, key=lambda item: item.source_path)],
            }
            generated_paths.append(self._write_json(f"source-index/{name}.generated.json", payload))
        return generated_paths

    def _write_chunk_files(self, chunks: list[ChunkRecord]) -> list[str]:
        generated_paths: list[str] = []
        for source_path, source_chunks in self._group_chunks(chunks, lambda chunk: chunk.source_path).items():
            payload = {
                "schema_version": SCHEMA_VERSION,
                "source_path": source_path,
                "chunks": [chunk.to_json() for chunk in source_chunks],
            }
            generated_paths.append(self._write_json(f"generated/chunks/{source_path}.generated.json", payload))
        return generated_paths

    def _write_graphs(self, sources: list[SourceRecord], chunks: list[ChunkRecord]) -> list[str]:
        generated_paths: list[str] = []
        for field, directory in (("language", "languages"), ("framework", "frameworks"), ("service", "services")):
            values = sorted({getattr(record, field) for record in sources if getattr(record, field) is not None})
            for value in values:
                matching_sources = [record.source_path for record in sources if getattr(record, field) == value]
                matching_chunks = [
                    chunk.chunk_id for chunk in chunks if chunk.relations.get(field) == value
                ]
                generated_paths.append(
                    self._write_text(
                        f"graph/{directory}/{value}.yaml",
                        self._render_yaml(
                            {
                                "schema_version": SCHEMA_VERSION,
                                field: value,
                                "sources": sorted(matching_sources),
                                "chunks": sorted(matching_chunks),
                            }
                        ),
                    )
                )
        return generated_paths

    def _write_route_indexes(self, repo_hash: str, chunks: list[ChunkRecord]) -> list[str]:
        generated_paths: list[str] = []
        shard_specs = [
            ("language", "by-language"),
            ("kind", "by-kind"),
            ("service", "by-service"),
            ("framework", "by-framework"),
        ]
        shards: list[dict[str, object]] = []
        for relation_key, directory in shard_specs:
            grouped = self._group_chunks(
                [chunk for chunk in chunks if chunk.relations.get(relation_key) is not None],
                lambda chunk, key=relation_key: str(chunk.relations[key]),
            )
            for value, shard_chunks in grouped.items():
                split_chunks = self._split_route_chunks(shard_chunks)
                for index, route_chunks in enumerate(split_chunks, start=1):
                    suffix = "" if len(split_chunks) == 1 else f"-part-{index:03d}"
                    path = f"route-index/{directory}/{value}{suffix}.generated.json"
                    generated_paths.append(self._write_json(path, self._route_payload(route_chunks)))
                    shards.append(
                        {
                            "kind": relation_key,
                            "name": value,
                            "path": path,
                            "chunk_count": len(route_chunks),
                        }
                    )

        root_payload = {
            "schema_version": SCHEMA_VERSION,
            "repo_hash": repo_hash,
            "shards": sorted(shards, key=lambda shard: (str(shard["kind"]), str(shard["name"]))),
        }
        generated_paths.append(self._write_json("route-index/index.generated.json", root_payload))
        return generated_paths

    def _route_payload(self, chunks: list[ChunkRecord]) -> dict[str, object]:
        return {
            "schema_version": SCHEMA_VERSION,
            "chunks": [
                {
                    "chunk_id": chunk.chunk_id,
                    "source_path": chunk.source_path,
                    "start_line": chunk.start_line,
                    "end_line": chunk.end_line,
                    "heading": chunk.heading,
                    "summary": chunk.summary,
                    "keywords": chunk.keywords,
                    "relations": chunk.relations,
                }
                for chunk in sorted(chunks, key=lambda item: item.chunk_id)
            ],
        }

    def _split_route_chunks(self, chunks: list[ChunkRecord]) -> list[list[ChunkRecord]]:
        sorted_chunks = sorted(chunks, key=lambda item: item.chunk_id)
        return [
            sorted_chunks[index : index + MAX_ROUTE_SHARD_CHUNKS]
            for index in range(0, len(sorted_chunks), MAX_ROUTE_SHARD_CHUNKS)
        ]

    def _write_json(self, relative_path: str, payload: dict[str, object]) -> str:
        path = self.memory_root / relative_path
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(json.dumps(payload, ensure_ascii=False, indent=2, sort_keys=True) + "\n", encoding="utf-8")
        return path.relative_to(self.root).as_posix()

    def _write_text(self, relative_path: str, text: str) -> str:
        path = self.memory_root / relative_path
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(text, encoding="utf-8")
        return path.relative_to(self.root).as_posix()

    def _render_yaml(self, payload: dict[str, object]) -> str:
        lines: list[str] = []
        for key in sorted(payload):
            value = payload[key]
            if isinstance(value, list):
                lines.append(f"{key}:")
                for item in value:
                    lines.append(f"  - {item}")
            else:
                lines.append(f'{key}: "{value}"')
        return "\n".join(lines) + "\n"

    def _base_id(self, source_path: str) -> str:
        path = Path(source_path)
        without_suffix = (path.parent / path.stem).as_posix() if path.parent.as_posix() != "." else path.stem
        return without_suffix.replace("/", ".").replace("\\", ".")

    def _base_ids_for_sources(self, sources: list[SourceRecord]) -> dict[str, str]:
        base_counts: dict[str, int] = {}
        for record in sources:
            base_id = self._base_id(record.source_path)
            base_counts[base_id] = base_counts.get(base_id, 0) + 1

        base_ids: dict[str, str] = {}
        for record in sources:
            base_id = self._base_id(record.source_path)
            if base_counts[base_id] > 1:
                suffix = hashlib.sha256(record.source_path.encode("utf-8")).hexdigest()[:8]
                base_id = f"{base_id}-{suffix}"
            base_ids[record.source_path] = base_id
        return base_ids

    def _group_chunks(
        self,
        chunks: list[ChunkRecord],
        key_func,
    ) -> dict[str, list[ChunkRecord]]:
        grouped: dict[str, list[ChunkRecord]] = {}
        for chunk in sorted(chunks, key=lambda item: item.chunk_id):
            grouped.setdefault(key_func(chunk), []).append(chunk)
        return grouped
