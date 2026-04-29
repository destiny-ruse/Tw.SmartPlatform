import json
import tempfile
import unittest
from pathlib import Path

from tools.tw_memory_test_support import import_engine


engine = import_engine()
MemoryGenerator = engine.generator.MemoryGenerator
MemoryChecker = engine.checker.MemoryChecker
SearchIndex = engine.search.SearchIndex


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

    def test_check_allows_ignored_runtime_cache_directories(self):
        with tempfile.TemporaryDirectory() as work:
            root = Path(work)
            (root / "docs").mkdir()
            (root / "docs" / "README.md").write_text("# Docs\n", encoding="utf-8")
            MemoryGenerator(root).generate()
            SearchIndex(root).build_fts()
            vector_cache = root / ".tw-memory" / "generated" / "vector" / "cache.bin"
            vector_cache.write_bytes(b"runtime cache")

            diagnostics = MemoryChecker(root).check()
            runtime_paths = {
                item.path
                for item in diagnostics
                if item.path
                and item.path.startswith((".tw-memory/generated/fts/", ".tw-memory/generated/vector/"))
            }

            self.assertEqual(runtime_paths, set())

    def test_check_warns_when_route_index_is_too_large(self):
        with tempfile.TemporaryDirectory() as work:
            root = Path(work)
            index = root / ".tw-memory" / "route-index"
            index.mkdir(parents=True)
            (index / "index.generated.json").write_text("x" * 210_000, encoding="utf-8")

            diagnostics = MemoryChecker(root).check()

            self.assertTrue(any(item.code == "large-route-index" and item.level == "warning" for item in diagnostics))

    def test_check_reports_invalid_generated_json(self):
        with tempfile.TemporaryDirectory() as work:
            root = Path(work)
            for path in [
                root / ".tw-memory" / "source-index" / "docs.generated.json",
                root / ".tw-memory" / "generated" / "chunks" / "docs" / "README.md.generated.json",
                root / ".tw-memory" / "route-index" / "index.generated.json",
            ]:
                path.parent.mkdir(parents=True, exist_ok=True)
                path.write_text("{not-json", encoding="utf-8")

            diagnostics = MemoryChecker(root).check()
            invalid_paths = {
                item.path
                for item in diagnostics
                if item.code == "invalid-json" and item.level == "error"
            }

            self.assertIn(".tw-memory/source-index/docs.generated.json", invalid_paths)
            self.assertIn(".tw-memory/generated/chunks/docs/README.md.generated.json", invalid_paths)
            self.assertIn(".tw-memory/route-index/index.generated.json", invalid_paths)

    def test_check_rejects_source_index_paths_that_escape_repo(self):
        with tempfile.TemporaryDirectory() as work:
            root = Path(work)
            index = root / ".tw-memory" / "source-index"
            index.mkdir(parents=True)
            payload = {
                "sources": [
                    {"source_path": str((root.parent / "outside.md").resolve()), "source_hash": "sha256:x"},
                    {"source_path": "../outside.md", "source_hash": "sha256:y"},
                ]
            }
            (index / "docs.generated.json").write_text(json.dumps(payload), encoding="utf-8")

            diagnostics = MemoryChecker(root).check()
            invalid_paths = [
                item.path
                for item in diagnostics
                if item.code == "invalid-source-path" and item.level == "error"
            ]

            self.assertEqual(len(invalid_paths), 2)

    def test_check_rejects_chunk_paths_that_escape_repo(self):
        with tempfile.TemporaryDirectory() as work:
            root = Path(work)
            chunk_file = root / ".tw-memory" / "generated" / "chunks" / "bad.generated.json"
            chunk_file.parent.mkdir(parents=True)
            payload = {
                "source_path": "docs/README.md",
                "chunks": [
                    {
                        "source_path": str((root.parent / "outside.md").resolve()),
                        "start_line": 1,
                        "end_line": 1,
                    },
                    {"source_path": "../outside.md", "start_line": 1, "end_line": 1},
                ],
            }
            chunk_file.write_text(json.dumps(payload), encoding="utf-8")

            diagnostics = MemoryChecker(root).check()
            invalid_paths = [
                item.path
                for item in diagnostics
                if item.code == "invalid-chunk-path" and item.level == "error"
            ]

            self.assertEqual(len(invalid_paths), 2)
