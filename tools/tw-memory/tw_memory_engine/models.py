from __future__ import annotations

from dataclasses import asdict, dataclass, field
from typing import Any


@dataclass(frozen=True)
class GeneratorInfo:
    name: str
    version: str

    def to_json(self) -> dict[str, Any]:
        return asdict(self)


@dataclass(frozen=True)
class SourceRecord:
    source_path: str
    source_hash: str
    source_type: str
    language: str | None
    framework: str | None
    service: str | None
    generator: GeneratorInfo

    def to_json(self) -> dict[str, Any]:
        return asdict(self)


@dataclass(frozen=True)
class ChunkRecord:
    chunk_id: str
    source_path: str
    source_hash: str
    start_line: int
    end_line: int
    heading: str | None
    summary: str
    keywords: list[str]
    relations: dict[str, Any] = field(default_factory=dict)

    def to_json(self) -> dict[str, Any]:
        return asdict(self)


@dataclass(frozen=True)
class Diagnostic:
    level: str
    code: str
    message: str
    path: str | None = None

    def to_json(self) -> dict[str, Any]:
        return asdict(self)


@dataclass(frozen=True)
class QueryResult:
    chunk_id: str
    source_path: str
    start_line: int
    end_line: int
    summary: str
    score: float

    def to_json(self) -> dict[str, Any]:
        return asdict(self)
