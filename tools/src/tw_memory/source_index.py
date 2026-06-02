from __future__ import annotations

import json
from pathlib import Path
from typing import Any

from tw_memory.generated_io import repo_relative
from tw_memory.hashing import sha256_normalized


def source_id(source_type: str, key: str) -> str:
    """Return a stable source identifier for a source type and key."""
    return f"{source_type}:{key}"


def make_source_entry(
    root: Path,
    path: Path,
    source_type: str,
    source_key: str,
    extractor: str,
) -> dict[str, str]:
    """Create a source-index entry for a repository source file."""
    return {
        "source_id": source_id(source_type, source_key),
        "source_type": source_type,
        "path": repo_relative(root, path),
        "hash_algorithm": "sha256",
        "sha256": sha256_normalized(path),
        "extractor": extractor,
    }


def write_source_index(path: Path, entries: list[dict[str, str]]) -> None:
    """Write a deterministic source-index JSON document."""
    path.parent.mkdir(parents=True, exist_ok=True)
    data = {
        "schema_version": "1.0.0",
        "sources": {
            entry["source_id"]: entry
            for entry in sorted(entries, key=lambda entry: entry["source_id"])
        },
    }
    text = json.dumps(data, ensure_ascii=False, indent=2, sort_keys=True)
    path.write_text(f"{text}\n", encoding="utf-8", newline="\n")


def load_source_index(path: Path) -> dict[str, Any]:
    """Load a generated source-index JSON document."""
    data = json.loads(path.read_text(encoding="utf-8"))
    return data if isinstance(data, dict) else {}
