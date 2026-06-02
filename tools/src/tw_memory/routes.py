from __future__ import annotations

from pathlib import Path
from typing import Any

from tw_memory.yaml_io import dump_yaml


def write_route(path: Path, schema_version: str, entries: dict[str, Any]) -> None:
    """Write a deterministic generated route YAML document."""
    dump_yaml(path, {"schema_version": schema_version, **entries})
