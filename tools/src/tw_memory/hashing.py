from __future__ import annotations

import hashlib
from pathlib import Path


def sha256_normalized(path: Path) -> str:
    """Return a sha256 digest after normalizing CRLF and CR line endings."""
    raw = path.read_bytes()
    normalized = raw.replace(b"\r\n", b"\n").replace(b"\r", b"\n")
    return hashlib.sha256(normalized).hexdigest()
