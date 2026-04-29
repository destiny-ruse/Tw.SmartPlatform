from __future__ import annotations

import hashlib
from collections.abc import Iterable
from pathlib import Path


def normalize_text_for_hash(text: str) -> bytes:
    return text.replace("\r\n", "\n").replace("\r", "\n").encode("utf-8")


def file_sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as source:
        for chunk in iter(lambda: source.read(1024 * 1024), b""):
            digest.update(chunk)
    return f"sha256:{digest.hexdigest()}"


def tree_hash(root: Path, paths: Iterable[str]) -> str:
    digest = hashlib.sha256()
    for source_path in sorted(paths):
        digest.update(normalize_text_for_hash(source_path))
        digest.update(b"\0")
        digest.update(normalize_text_for_hash(file_sha256(root / source_path)))
        digest.update(b"\n")
    return f"sha256:{digest.hexdigest()}"
