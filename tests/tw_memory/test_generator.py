import json
import tempfile
import unittest
from pathlib import Path

from tools.tw_memory_test_support import import_engine


engine = import_engine()
MemoryGenerator = engine.generator.MemoryGenerator


class GeneratorTests(unittest.TestCase):
    def test_generate_writes_three_layer_memory_without_source_bodies(self):
        with tempfile.TemporaryDirectory() as work:
            root = Path(work)
            (root / "docs").mkdir()
            (root / "docs" / "README.md").write_text("# Docs\n\nHuman readable body.\n", encoding="utf-8")

            result = MemoryGenerator(root).generate()

            self.assertEqual(result.errors, [])
            source_index = json.loads((root / ".tw-memory" / "source-index" / "docs.generated.json").read_text(encoding="utf-8"))
            route_index = json.loads((root / ".tw-memory" / "route-index" / "index.generated.json").read_text(encoding="utf-8"))
            chunk_files = list((root / ".tw-memory" / "generated" / "chunks").rglob("*.generated.json"))

            self.assertGreaterEqual(len(source_index["sources"]), 1)
            self.assertIn("shards", route_index)
            self.assertGreaterEqual(len(chunk_files), 1)
            self.assertNotIn("Human readable body.", json.dumps(source_index, ensure_ascii=False))
            self.assertNotIn("Human readable body.", json.dumps(route_index, ensure_ascii=False))
            for chunk_file in chunk_files:
                self.assertNotIn("Human readable body.", chunk_file.read_text(encoding="utf-8"))
            for route_file in (root / ".tw-memory" / "route-index").rglob("*.generated.json"):
                if route_file.name != "index.generated.json":
                    self.assertNotIn("Human readable body.", route_file.read_text(encoding="utf-8"))

    def test_generate_extracts_bounded_keywords_from_body_without_storing_body(self):
        with tempfile.TemporaryDirectory() as work:
            root = Path(work)
            docs = root / "docs"
            docs.mkdir()
            source = docs / "general.md"
            source.write_text(
                "# General\n\nRedis distributed cache wrapper handles cache invalidation.\n",
                encoding="utf-8",
            )

            MemoryGenerator(root).generate()

            chunk_file = root / ".tw-memory" / "generated" / "chunks" / "docs" / "general.md.generated.json"
            payload = json.loads(chunk_file.read_text(encoding="utf-8"))
            chunk = payload["chunks"][0]
            serialized = json.dumps(payload, ensure_ascii=False)
            self.assertIn("redis", chunk["keywords"])
            self.assertIn("cache", chunk["keywords"])
            self.assertLessEqual(len(chunk["keywords"]), 20)
            self.assertNotIn("Redis distributed cache wrapper handles cache invalidation.", serialized)

    def test_generate_creates_language_graph_for_dotnet_frontend_java_python(self):
        with tempfile.TemporaryDirectory() as work:
            root = Path(work)
            for path in [
                "backend/dotnet/README.md",
                "backend/java/README.md",
                "backend/python/README.md",
                "frontend/README.md",
            ]:
                target = root / path
                target.parent.mkdir(parents=True, exist_ok=True)
                target.write_text(f"# {path}\n", encoding="utf-8")

            MemoryGenerator(root).generate()

            for language in ["dotnet", "java", "python", "frontend"]:
                graph = root / ".tw-memory" / "graph" / "languages" / f"{language}.yaml"
                self.assertTrue(graph.exists(), language)

    def test_generate_prunes_stale_generated_artifacts(self):
        with tempfile.TemporaryDirectory() as work:
            root = Path(work)
            (root / "docs").mkdir()
            (root / "docs" / "README.md").write_text("# Docs\n", encoding="utf-8")
            stale_files = [
                root / ".tw-memory" / "source-index" / "stale.generated.json",
                root / ".tw-memory" / "graph" / "languages" / "stale.yaml",
                root / ".tw-memory" / "route-index" / "by-language" / "stale.generated.json",
                root / ".tw-memory" / "generated" / "chunks" / "stale.generated.json",
            ]
            preserved_files = [
                root / ".tw-memory" / "generated" / "fts" / "cache.db",
                root / ".tw-memory" / "generated" / "vector" / "cache.bin",
            ]
            for path in stale_files + preserved_files:
                path.parent.mkdir(parents=True, exist_ok=True)
                path.write_text("stale\n", encoding="utf-8")

            MemoryGenerator(root).generate()

            for path in stale_files:
                self.assertFalse(path.exists(), path)
            for path in preserved_files:
                self.assertTrue(path.exists(), path)

    def test_generate_uses_collision_safe_chunk_ids(self):
        with tempfile.TemporaryDirectory() as work:
            root = Path(work)
            source_paths = [
                "docs/cache.md",
                "frontend/apps/tw.web.client/README.md",
                "frontend/apps/tw/web/client/README.md",
            ]
            for source_path in source_paths:
                target = root / source_path
                target.parent.mkdir(parents=True, exist_ok=True)
                target.write_text(f"# {source_path}\n", encoding="utf-8")

            MemoryGenerator(root).generate()

            chunk_files = (root / ".tw-memory" / "generated" / "chunks").rglob("*.generated.json")
            chunk_ids = [
                chunk["chunk_id"]
                for chunk_file in chunk_files
                for chunk in json.loads(chunk_file.read_text(encoding="utf-8"))["chunks"]
            ]
            self.assertIn("docs.cache#chunk-001", chunk_ids)
            self.assertEqual(len(chunk_ids), len(set(chunk_ids)))

    def test_generate_reports_utf8_decode_errors(self):
        with tempfile.TemporaryDirectory() as work:
            root = Path(work)
            path = root / "docs" / "bad.md"
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_bytes(b"# bad\n\xff\xfe\n")

            result = MemoryGenerator(root).generate()

            self.assertEqual(len(result.errors), 1)
            self.assertEqual(result.errors[0].code, "source-decode-failed")
            self.assertEqual(result.errors[0].path, "docs/bad.md")

    def test_generate_summarizes_synthetic_chunks_by_line_range(self):
        with tempfile.TemporaryDirectory() as work:
            root = Path(work)
            path = root / "docs" / "no-heading.md"
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_text("first\nsecond\nthird\n", encoding="utf-8")

            MemoryGenerator(root).generate()

            chunk_file = root / ".tw-memory" / "generated" / "chunks" / "docs" / "no-heading.md.generated.json"
            payload = json.loads(chunk_file.read_text(encoding="utf-8"))
            self.assertEqual(payload["chunks"][0]["summary"], "Lines 1-3 in docs/no-heading.md")

    def test_generate_splits_large_route_shards_before_checker_warning_size(self):
        with tempfile.TemporaryDirectory() as work:
            root = Path(work)
            docs = root / "docs" / "superpowers" / "specs"
            docs.mkdir(parents=True)
            for index in range(500):
                (docs / f"very-long-memory-route-shard-file-name-{index:03d}.md").write_text(
                    f"# Route shard warning fixture {index}\n",
                    encoding="utf-8",
                )

            MemoryGenerator(root).generate()

            route_files = [
                path
                for path in (root / ".tw-memory" / "route-index").rglob("*.generated.json")
                if path.name != "index.generated.json"
            ]
            self.assertTrue(route_files)
            self.assertTrue(all(path.stat().st_size <= 200 * 1024 for path in route_files))
