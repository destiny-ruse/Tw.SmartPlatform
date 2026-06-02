from __future__ import annotations

import re
from dataclasses import dataclass


@dataclass(frozen=True)
class SecretHit:
    """Secret-like text found in scanned content."""

    kind: str
    match: str


_SECRET_PATTERNS: tuple[tuple[str, re.Pattern[str]], ...] = (
    (
        "connection-string",
        re.compile(r"(?i)(password|pwd)\s*=\s*[^;\s]+"),
    ),
    (
        "bearer-token",
        re.compile(r"(?i)bearer\s+[A-Za-z0-9._\-]{20,}"),
    ),
    (
        "private-key",
        re.compile(r"-----BEGIN [A-Z ]*PRIVATE KEY-----"),
    ),
)


def scan_secrets(text: str) -> list[SecretHit]:
    """Return secret-like pattern hits without exposing matched values."""
    hits: list[SecretHit] = []
    for kind, pattern in _SECRET_PATTERNS:
        hits.extend(SecretHit(kind, "<redacted>") for _ in pattern.finditer(text))
    return hits
