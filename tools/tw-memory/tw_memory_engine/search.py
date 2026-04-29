from __future__ import annotations

import json
import re
import sqlite3
from contextlib import closing
from pathlib import Path
from typing import Any, Iterable

from .models import QueryResult
from .paths import memory_root


class SearchIndex:
    def __init__(self, root: Path):
        self.root = root.resolve()
        self.memory_root = memory_root(self.root)
        self.database_path = self.memory_root / "generated" / "fts" / "tw-memory.sqlite"

    def build_fts(self) -> int:
        chunks = list(self._generated_chunks())
        self.database_path.parent.mkdir(parents=True, exist_ok=True)

        with closing(sqlite3.connect(self.database_path)) as connection:
            connection.execute("DROP TABLE IF EXISTS chunks_fts")
            connection.execute("DROP TABLE IF EXISTS chunks")
            connection.execute(
                """
                CREATE TABLE chunks(
                    chunk_id TEXT PRIMARY KEY,
                    source_path TEXT NOT NULL,
                    start_line INTEGER NOT NULL,
                    end_line INTEGER NOT NULL,
                    summary TEXT NOT NULL,
                    keywords_json TEXT NOT NULL,
                    relations_json TEXT NOT NULL
                )
                """
            )
            connection.execute(
                """
                CREATE VIRTUAL TABLE chunks_fts USING fts5(
                    chunk_id,
                    summary,
                    keywords,
                    source_path,
                    content=''
                )
                """
            )

            for chunk in chunks:
                keywords = self._string_list(chunk.get("keywords"))
                relations = chunk.get("relations") if isinstance(chunk.get("relations"), dict) else {}
                cursor = connection.execute(
                    """
                    INSERT INTO chunks(
                        chunk_id,
                        source_path,
                        start_line,
                        end_line,
                        summary,
                        keywords_json,
                        relations_json
                    )
                    VALUES (?, ?, ?, ?, ?, ?, ?)
                    """,
                    (
                        str(chunk["chunk_id"]),
                        str(chunk["source_path"]),
                        int(chunk["start_line"]),
                        int(chunk["end_line"]),
                        str(chunk.get("summary") or ""),
                        json.dumps(keywords, ensure_ascii=False, sort_keys=True),
                        json.dumps(relations, ensure_ascii=False, sort_keys=True),
                    ),
                )
                connection.execute(
                    """
                    INSERT INTO chunks_fts(rowid, chunk_id, summary, keywords, source_path)
                    VALUES (?, ?, ?, ?, ?)
                    """,
                    (
                        cursor.lastrowid,
                        str(chunk["chunk_id"]),
                        str(chunk.get("summary") or ""),
                        " ".join(keywords),
                        str(chunk["source_path"]),
                    ),
                )
            connection.commit()

        return len(chunks)

    def query(self, text: str, stack: str | None, kind: str | None, limit: int) -> list[QueryResult]:
        if limit <= 0:
            return []
        if self.database_path.exists():
            return self._query_fts(text, stack, kind, limit)
        return self._query_route_index(text, stack, kind, limit)

    def _query_fts(self, text: str, stack: str | None, kind: str | None, limit: int) -> list[QueryResult]:
        expression = self._fts_expression(text)
        if expression is None:
            return []

        rows: list[tuple[QueryResult, dict[str, Any]]] = []
        with closing(sqlite3.connect(self.database_path)) as connection:
            connection.row_factory = sqlite3.Row
            for row in connection.execute(
                """
                SELECT
                    c.chunk_id,
                    c.source_path,
                    c.start_line,
                    c.end_line,
                    c.summary,
                    c.relations_json,
                    bm25(chunks_fts) AS rank
                FROM chunks_fts
                JOIN chunks c ON c.rowid = chunks_fts.rowid
                WHERE chunks_fts MATCH ?
                """,
                (expression,),
            ):
                relations = self._json_dict(row["relations_json"])
                if not self._matches_filters(relations, stack, kind):
                    continue
                rows.append(
                    (
                        QueryResult(
                            chunk_id=row["chunk_id"],
                            source_path=row["source_path"],
                            start_line=int(row["start_line"]),
                            end_line=int(row["end_line"]),
                            summary=row["summary"],
                            score=-float(row["rank"]),
                        ),
                        relations,
                    )
                )

        results = [result for result, _relations in rows]
        return sorted(results, key=lambda result: (-result.score, result.chunk_id))[:limit]

    def _query_route_index(self, text: str, stack: str | None, kind: str | None, limit: int) -> list[QueryResult]:
        terms = [term.lower() for term in self._terms(text)]
        if not terms:
            return []

        by_id: dict[str, QueryResult] = {}
        for chunk in self._route_chunks():
            relations = chunk.get("relations") if isinstance(chunk.get("relations"), dict) else {}
            if not self._matches_filters(relations, stack, kind):
                continue

            haystack = " ".join(
                [
                    str(chunk.get("chunk_id") or ""),
                    str(chunk.get("source_path") or ""),
                    str(chunk.get("summary") or ""),
                    " ".join(self._string_list(chunk.get("keywords"))),
                ]
            ).lower()
            score = float(sum(1 for term in terms if term in haystack))
            if score <= 0:
                continue

            chunk_id = str(chunk["chunk_id"])
            current = by_id.get(chunk_id)
            if current is None or score > current.score:
                by_id[chunk_id] = QueryResult(
                    chunk_id=chunk_id,
                    source_path=str(chunk["source_path"]),
                    start_line=int(chunk["start_line"]),
                    end_line=int(chunk["end_line"]),
                    summary=str(chunk.get("summary") or ""),
                    score=score,
                )

        return sorted(by_id.values(), key=lambda result: (-result.score, result.chunk_id))[:limit]

    def _generated_chunks(self) -> Iterable[dict[str, Any]]:
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
            for chunk in chunks:
                if not isinstance(chunk, dict):
                    continue
                normalized = dict(chunk)
                if "source_path" not in normalized and isinstance(payload_source_path, str):
                    normalized["source_path"] = payload_source_path
                if self._is_valid_chunk(normalized):
                    yield normalized

    def _route_chunks(self) -> Iterable[dict[str, Any]]:
        index_path = self.memory_root / "route-index" / "index.generated.json"
        payload = self._load_json(index_path)
        shards = payload.get("shards", []) if isinstance(payload, dict) else []
        if not isinstance(shards, list):
            return

        for shard in shards:
            if not isinstance(shard, dict) or not isinstance(shard.get("path"), str):
                continue
            shard_path = (self.memory_root / shard["path"]).resolve()
            if not self._is_under(shard_path, self.memory_root):
                continue
            shard_payload = self._load_json(shard_path)
            chunks = shard_payload.get("chunks", []) if isinstance(shard_payload, dict) else []
            if not isinstance(chunks, list):
                continue
            for chunk in chunks:
                if isinstance(chunk, dict) and self._is_valid_chunk(chunk):
                    yield chunk

    def _is_valid_chunk(self, chunk: dict[str, Any]) -> bool:
        return (
            isinstance(chunk.get("chunk_id"), str)
            and isinstance(chunk.get("source_path"), str)
            and isinstance(chunk.get("start_line"), int)
            and isinstance(chunk.get("end_line"), int)
        )

    def _load_json(self, path: Path) -> Any:
        try:
            return json.loads(path.read_text(encoding="utf-8"))
        except (OSError, UnicodeDecodeError, json.JSONDecodeError):
            return {}

    def _fts_expression(self, text: str) -> str | None:
        terms = self._terms(text)
        if not terms:
            return None
        quoted_terms = []
        for term in terms:
            escaped = term.replace('"', '""')
            quoted_terms.append(f'"{escaped}"')
        return " OR ".join(quoted_terms)

    def _terms(self, text: str) -> list[str]:
        return [term for term in re.split(r"\s+", text.strip()) if term]

    def _matches_filters(self, relations: dict[str, Any], stack: str | None, kind: str | None) -> bool:
        if stack is not None and relations.get("language") != stack:
            return False
        if kind is not None and relations.get("kind") != kind:
            return False
        return True

    def _string_list(self, value: Any) -> list[str]:
        if not isinstance(value, list):
            return []
        return [str(item) for item in value]

    def _json_dict(self, value: str) -> dict[str, Any]:
        try:
            payload = json.loads(value)
        except json.JSONDecodeError:
            return {}
        return payload if isinstance(payload, dict) else {}

    def _is_under(self, path: Path, parent: Path) -> bool:
        try:
            path.relative_to(parent.resolve())
        except ValueError:
            return False
        return True
