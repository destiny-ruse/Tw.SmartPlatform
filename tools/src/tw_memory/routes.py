from __future__ import annotations

from pathlib import Path
from typing import Any

import yaml

from tw_memory.generated_io import write_generated_text


def write_route(root: Path, path: Path, schema_version: str, entries: dict[str, Any]) -> None:
    """Write a deterministic generated route YAML document."""
    text = yaml.safe_dump(
        {"schema_version": schema_version, **entries},
        allow_unicode=True,
        sort_keys=True,
        default_flow_style=False,
    )
    write_generated_text(root, path, text)
