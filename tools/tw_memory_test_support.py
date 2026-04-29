import importlib
import sys
from pathlib import Path


def import_engine():
    repo_root = Path(__file__).resolve().parents[1]
    tool_root = repo_root / "tools" / "tw-memory"
    if str(tool_root) not in sys.path:
        sys.path.insert(0, str(tool_root))

    class Engine:
        def __getattr__(self, name):
            return importlib.import_module(f"tw_memory_engine.{name}")

    return Engine()
