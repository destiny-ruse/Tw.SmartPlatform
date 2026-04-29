from __future__ import annotations

import re
from collections.abc import Sequence

from .models import SourceRecord


LANGUAGE_TOKENS = {"frontend", "dotnet", "java", "python", "contracts", "deploy", "docs"}
TOKEN_RE = re.compile(r"[/_.\-\s]+")


def summarize_chunk(path: str, heading: str | None, lines: Sequence[str]) -> str:
    if heading:
        return f"{heading} in {path}"
    return f"{len(lines)} line chunk in {path}"


def extract_keywords(path: str, heading: str | None, lines: Sequence[str]) -> list[str]:
    del lines
    keywords = {token for token in _tokens(path) if token}
    if heading:
        keywords.update(token for token in _tokens(heading) if len(token) > 2)
    keywords.update(token for token in LANGUAGE_TOKENS if token in path.lower().split("/"))
    return sorted(keywords)[:20]


def relations_for_source(record: SourceRecord) -> dict[str, object]:
    capabilities = sorted(
        token
        for token in _tokens(record.source_path)
        if len(token) > 2 and token not in {"readme", "generated", "json", "yaml"}
    )[:12]
    return {
        "language": record.language,
        "kind": record.source_type,
        "service": record.service,
        "framework": record.framework,
        "capabilities": capabilities,
    }


def _tokens(value: str) -> list[str]:
    return [token for token in TOKEN_RE.split(value.lower()) if token]
