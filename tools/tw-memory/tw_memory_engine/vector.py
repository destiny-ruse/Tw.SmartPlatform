from __future__ import annotations

from dataclasses import dataclass
from pathlib import Path
from typing import Any


SUPPORTED_BACKENDS = {"aliyun", "tencent", "volcengine", "self-hosted"}


@dataclass(frozen=True)
class VectorSyncResult:
    backend: str
    status: str
    uploaded: int = 0
    exit_code: int = 2

    def to_json(self) -> dict[str, Any]:
        return {
            "backend": self.backend,
            "status": self.status,
            "uploaded": self.uploaded,
        }


class VectorSyncRunner:
    def __init__(self, root: Path):
        self.root = root

    def run(self, backend: str) -> VectorSyncResult:
        configs = self._read_backend_configs()
        if backend not in SUPPORTED_BACKENDS:
            return VectorSyncResult(backend=backend, status="unknown-backend")

        config = configs.get(backend, {})
        if config.get("enabled") is not True:
            return VectorSyncResult(backend=backend, status="disabled")

        return VectorSyncResult(backend=backend, status="not-configured")

    def _read_backend_configs(self) -> dict[str, dict[str, bool]]:
        path = self.root / ".tw-memory" / "adapters" / "vector-backends.yaml"
        if not path.is_file():
            return {}

        configs: dict[str, dict[str, bool]] = {}
        in_vector_backends = False
        current_backend: str | None = None

        for raw_line in path.read_text(encoding="utf-8").splitlines():
            stripped = raw_line.strip()
            if not stripped or stripped.startswith("#"):
                continue

            indent = len(raw_line) - len(raw_line.lstrip(" "))
            if stripped == "vector_backends:":
                in_vector_backends = True
                current_backend = None
                continue

            if not in_vector_backends:
                continue

            if indent == 0:
                current_backend = None
                in_vector_backends = False
                continue

            if indent == 2 and stripped.endswith(":"):
                current_backend = stripped[:-1]
                configs.setdefault(current_backend, {"enabled": False})
                continue

            if indent >= 4 and current_backend and stripped.startswith("enabled:"):
                value = stripped.split(":", 1)[1].strip().lower()
                configs.setdefault(current_backend, {"enabled": False})["enabled"] = value == "true"

        return configs
