from __future__ import annotations

from pathlib import Path
from typing import Any

import yaml


def load_yaml(path: Path) -> Any:
    """Load YAML safely and return an empty mapping for empty files."""
    with path.open("r", encoding="utf-8") as stream:
        return yaml.safe_load(stream) or {}


def dump_yaml(path: Path, data: Any) -> None:
    """Write deterministic UTF-8 YAML with LF line endings."""
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open("w", encoding="utf-8", newline="\n") as stream:
        yaml.safe_dump(
            data,
            stream,
            allow_unicode=True,
            sort_keys=True,
            default_flow_style=False,
        )
