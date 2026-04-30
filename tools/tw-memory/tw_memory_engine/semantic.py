from __future__ import annotations

import re
from collections.abc import Sequence

from .models import SourceRecord


LANGUAGE_TOKENS = {"frontend", "dotnet", "java", "python", "contracts", "deploy", "docs"}
STOP_WORDS = {
    "and",
    "are",
    "but",
    "for",
    "from",
    "has",
    "have",
    "into",
    "not",
    "the",
    "this",
    "that",
    "with",
    "without",
}
MAX_KEYWORDS = 20
TOKEN_RE = re.compile(r"[/_.\-\s]+")
BODY_KEYWORD_RE = re.compile(r"[A-Za-z][A-Za-z0-9_]{2,}")
CAMEL_RE = re.compile(r"(?<=[a-z0-9])(?=[A-Z])")


def summarize_chunk(
    path: str,
    heading: str | None,
    lines: Sequence[str],
    start_line: int | None = None,
    end_line: int | None = None,
) -> str:
    del lines
    if heading:
        return f"{heading} in {path}"
    if start_line is None or end_line is None:
        raise ValueError("start_line and end_line are required for synthetic chunk summaries")
    return f"Lines {start_line}-{end_line} in {path}"


def extract_keywords(path: str, heading: str | None, lines: Sequence[str]) -> list[str]:
    keywords = {token for token in _tokens(path) if token}
    if heading:
        keywords.update(token for token in _tokens(heading) if len(token) > 2)
    keywords.update(token for token in LANGUAGE_TOKENS if token in path.lower().split("/"))
    keywords.update(_body_tokens(lines))
    return sorted(keywords)[:MAX_KEYWORDS]


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


def _body_tokens(lines: Sequence[str]) -> set[str]:
    tokens: set[str] = set()
    for line in lines:
        for raw_token in BODY_KEYWORD_RE.findall(line):
            for token in CAMEL_RE.sub(" ", raw_token).replace("_", " ").split():
                normalized = token.lower()
                if len(normalized) < 3 or normalized in STOP_WORDS:
                    continue
                tokens.add(normalized)
                if len(tokens) >= MAX_KEYWORDS:
                    return tokens
    return tokens
