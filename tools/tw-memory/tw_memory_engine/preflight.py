from __future__ import annotations

from pathlib import Path, PurePosixPath

from .checker import MemoryChecker
from .paths import memory_root
from .search import SearchIndex


class PreflightRunner:
    def __init__(self, root: Path):
        self.root = root.resolve()
        self.memory_root = memory_root(self.root)

    def run(self, task: str, stack: str | None, path: str | None) -> dict[str, object]:
        if not (self.memory_root / "route-index" / "index.generated.json").is_file():
            return {
                "status": "missing-memory",
                "candidates": [],
                "diagnostics": [],
                "actions": ["generate", "check"],
            }

        diagnostics = MemoryChecker(self.root).check()
        diagnostic_payload = [diagnostic.to_json() for diagnostic in diagnostics]
        if any(diagnostic.level == "error" for diagnostic in diagnostics):
            return {
                "status": "stale-or-invalid",
                "candidates": [],
                "diagnostics": diagnostic_payload,
                "actions": ["generate", "check"],
            }

        index = SearchIndex(self.root)
        fts_available = index._can_use_fts()
        query = self._semantic_query(task, stack, path)
        candidates = [result.to_json() for result in index.query(query, stack=None, kind=None, limit=5)]
        actions = [] if fts_available else ["build-search"]

        return {
            "status": "ok",
            "candidates": candidates,
            "diagnostics": diagnostic_payload,
            "actions": actions,
        }

    def _semantic_query(self, task: str, stack: str | None, path: str | None) -> str:
        terms = [task]
        if stack:
            terms.append(stack)
        if path:
            normalized = _normalize_path(path)
            basename = PurePosixPath(normalized).name
            if basename:
                terms.append(basename)
            language = _path_language(normalized)
            if language:
                terms.append(language)
            service = _path_service(normalized)
            if service:
                terms.append(service)
        return " ".join(term for term in terms if term)


def _normalize_path(path: str) -> str:
    parts = [part for part in path.strip().replace("\\", "/").split("/") if part and part != "."]
    return "/".join(parts)


def _path_language(path: str) -> str | None:
    parts = tuple(part for part in path.split("/") if part)
    if not parts:
        return None
    if parts[0] in {"docs", "docs-old"}:
        return "docs"
    if parts[0] in {"frontend", "contracts", "deploy"}:
        return parts[0]
    if len(parts) >= 2 and parts[0] == "backend" and parts[1] in {"dotnet", "java", "python"}:
        return parts[1]
    return None


def _path_service(path: str) -> str | None:
    parts = tuple(part for part in path.split("/") if part)
    if len(parts) >= 4 and parts[:3] == ("backend", "dotnet", "Services"):
        return parts[3].lower()
    if len(parts) >= 3 and parts[:2] in {("backend", "java"), ("backend", "python")}:
        return parts[2].lower()
    if len(parts) >= 3 and parts[:2] == ("frontend", "apps"):
        return parts[2].lower()
    return None
