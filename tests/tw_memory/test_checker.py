import tempfile
import unittest
from pathlib import Path

from tools.tw_memory_test_support import import_engine


engine = import_engine()
MemoryGenerator = engine.generator.MemoryGenerator
MemoryChecker = engine.checker.MemoryChecker


class CheckerTests(unittest.TestCase):
    def test_check_reports_stale_source_hash(self):
        with tempfile.TemporaryDirectory() as work:
            root = Path(work)
            (root / "docs").mkdir()
            source = root / "docs" / "README.md"
            source.write_text("# Docs\n\nFirst\n", encoding="utf-8")
            MemoryGenerator(root).generate()

            source.write_text("# Docs\n\nChanged\n", encoding="utf-8")
            diagnostics = MemoryChecker(root).check()

            self.assertTrue(any(item.code == "source-stale" for item in diagnostics))

    def test_check_rejects_sqlite_files_inside_committable_memory(self):
        with tempfile.TemporaryDirectory() as work:
            root = Path(work)
            (root / "docs").mkdir()
            (root / "docs" / "README.md").write_text("# Docs\n", encoding="utf-8")
            MemoryGenerator(root).generate()
            forbidden = root / ".tw-memory" / "source-index" / "cache.sqlite"
            forbidden.write_bytes(b"sqlite")

            diagnostics = MemoryChecker(root).check()

            self.assertTrue(any(item.code == "forbidden-memory-file" for item in diagnostics))

    def test_check_rejects_runtime_cache_directories_inside_committable_memory(self):
        with tempfile.TemporaryDirectory() as work:
            root = Path(work)
            (root / "docs").mkdir()
            (root / "docs" / "README.md").write_text("# Docs\n", encoding="utf-8")
            MemoryGenerator(root).generate()
            fts_cache = root / ".tw-memory" / "generated" / "fts" / "cache.json"
            vector_cache = root / ".tw-memory" / "generated" / "vector" / "cache.json"
            fts_cache.write_text("cache\n", encoding="utf-8")
            vector_cache.write_text("cache\n", encoding="utf-8")

            diagnostics = MemoryChecker(root).check()
            forbidden_paths = {
                item.path
                for item in diagnostics
                if item.code == "forbidden-memory-file"
            }

            self.assertIn(".tw-memory/generated/fts/cache.json", forbidden_paths)
            self.assertIn(".tw-memory/generated/vector/cache.json", forbidden_paths)

    def test_check_warns_when_route_index_is_too_large(self):
        with tempfile.TemporaryDirectory() as work:
            root = Path(work)
            index = root / ".tw-memory" / "route-index"
            index.mkdir(parents=True)
            (index / "index.generated.json").write_text("x" * 210_000, encoding="utf-8")

            diagnostics = MemoryChecker(root).check()

            self.assertTrue(any(item.code == "large-route-index" and item.level == "warning" for item in diagnostics))
