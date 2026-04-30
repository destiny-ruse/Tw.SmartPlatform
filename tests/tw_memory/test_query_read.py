import json
import sqlite3
import tempfile
import unittest
from contextlib import closing
from pathlib import Path

from tools.tw_memory_test_support import import_engine


engine = import_engine()
MemoryGenerator = engine.generator.MemoryGenerator
SearchIndex = engine.search.SearchIndex
ChunkReader = engine.reader.ChunkReader


class QueryReadTests(unittest.TestCase):
    def test_query_returns_limited_brief_results(self):
        with tempfile.TemporaryDirectory() as work:
            root = Path(work)
            docs = root / "docs"
            docs.mkdir()
            (docs / "cache.md").write_text("# Cache\n\nRedis distributed cache wrapper.\n", encoding="utf-8")
            MemoryGenerator(root).generate()
            SearchIndex(root).build_fts()

            results = SearchIndex(root).query("redis cache", stack=None, kind=None, limit=1)

            self.assertEqual(len(results), 1)
            self.assertIn("cache", results[0].summary.lower())

    def test_query_matches_body_derived_keywords_without_fts(self):
        with tempfile.TemporaryDirectory() as work:
            root = Path(work)
            docs = root / "docs"
            docs.mkdir()
            (docs / "general.md").write_text(
                "# General\n\nRedis distributed cache wrapper.\n",
                encoding="utf-8",
            )
            MemoryGenerator(root).generate()

            results = SearchIndex(root).query("redis", stack=None, kind=None, limit=5)

            self.assertEqual([result.chunk_id for result in results], ["docs.general#chunk-001"])

    def test_query_filters_by_stack_and_kind(self):
        with tempfile.TemporaryDirectory() as work:
            root = Path(work)
            docs = root / "docs"
            docs.mkdir()
            (docs / "cache.md").write_text("# Cache\n\nRedis distributed cache wrapper.\n", encoding="utf-8")
            package = root / "backend" / "dotnet" / "Cache"
            package.mkdir(parents=True)
            (package / "Cache.csproj").write_text("<Project Sdk=\"Microsoft.NET.Sdk\"></Project>\n", encoding="utf-8")
            MemoryGenerator(root).generate()

            docs_results = SearchIndex(root).query("cache", stack="docs", kind="manual", limit=10)
            package_results = SearchIndex(root).query("cache", stack="dotnet", kind="package", limit=10)

            self.assertEqual([result.chunk_id for result in docs_results], ["docs.cache#chunk-001"])
            self.assertEqual([result.source_path for result in package_results], ["backend/dotnet/Cache/Cache.csproj"])

    def test_query_falls_back_when_fts_database_is_corrupt(self):
        with tempfile.TemporaryDirectory() as work:
            root = Path(work)
            docs = root / "docs"
            docs.mkdir()
            (docs / "cache.md").write_text("# Cache\n\nRedis distributed cache wrapper.\n", encoding="utf-8")
            MemoryGenerator(root).generate()
            index = SearchIndex(root)
            index.database_path.parent.mkdir(parents=True, exist_ok=True)
            index.database_path.write_bytes(b"not sqlite")

            results = index.query("redis cache", stack=None, kind=None, limit=3)

            self.assertEqual(len(results), 1)
            self.assertEqual(results[0].chunk_id, "docs.cache#chunk-001")

    def test_query_falls_back_when_fts_tables_are_missing(self):
        with tempfile.TemporaryDirectory() as work:
            root = Path(work)
            docs = root / "docs"
            docs.mkdir()
            (docs / "cache.md").write_text("# Cache\n\nRedis distributed cache wrapper.\n", encoding="utf-8")
            MemoryGenerator(root).generate()
            index = SearchIndex(root)
            index.database_path.parent.mkdir(parents=True, exist_ok=True)
            with closing(sqlite3.connect(index.database_path)) as connection:
                connection.execute("CREATE TABLE unrelated(id INTEGER PRIMARY KEY)")
                connection.commit()

            results = index.query("redis cache", stack=None, kind=None, limit=3)

            self.assertEqual(len(results), 1)
            self.assertEqual(results[0].chunk_id, "docs.cache#chunk-001")

    def test_query_falls_back_when_fts_metadata_is_stale(self):
        with tempfile.TemporaryDirectory() as work:
            root = Path(work)
            docs = root / "docs"
            docs.mkdir()
            source = docs / "cache.md"
            source.write_text("# Redis\n\nDistributed cache wrapper.\n", encoding="utf-8")
            MemoryGenerator(root).generate()
            index = SearchIndex(root)
            index.build_fts()

            source.write_text("# Postgres\n\nRoute index update.\n", encoding="utf-8")
            MemoryGenerator(root).generate()
            results = index.query("postgres", stack=None, kind=None, limit=3)

            self.assertEqual(len(results), 1)
            self.assertEqual(results[0].chunk_id, "docs.cache#chunk-001")
            self.assertIn("Postgres", results[0].summary)

    def test_fts_remains_fresh_after_regenerate_without_source_changes(self):
        with tempfile.TemporaryDirectory() as work:
            root = Path(work)
            docs = root / "docs"
            docs.mkdir()
            (docs / "cache.md").write_text("# Redis\n\nDistributed cache wrapper.\n", encoding="utf-8")
            MemoryGenerator(root).generate()
            index = SearchIndex(root)
            index.build_fts()

            MemoryGenerator(root).generate()

            self.assertTrue(index._can_use_fts())

    def test_query_handles_odd_fts_input_without_traceback(self):
        with tempfile.TemporaryDirectory() as work:
            root = Path(work)
            docs = root / "docs"
            docs.mkdir()
            (docs / "cache.md").write_text("# Cache\n\nRedis distributed cache wrapper.\n", encoding="utf-8")
            MemoryGenerator(root).generate()
            SearchIndex(root).build_fts()

            results = SearchIndex(root).query('redis \x00 cache " OR *', stack=None, kind=None, limit=3)

            self.assertEqual(len(results), 1)
            self.assertEqual(results[0].chunk_id, "docs.cache#chunk-001")

    def test_read_returns_evidence_and_verifies_hash(self):
        with tempfile.TemporaryDirectory() as work:
            root = Path(work)
            docs = root / "docs"
            docs.mkdir()
            source = docs / "cache.md"
            source.write_text("# Cache\n\nRedis distributed cache wrapper.\n", encoding="utf-8")
            MemoryGenerator(root).generate()
            chunk_id = "docs.cache#chunk-001"

            evidence = ChunkReader(root).read(chunk_id, with_neighbors=0)

            self.assertEqual(evidence["chunk_id"], chunk_id)
            self.assertIn("Redis distributed cache wrapper.", evidence["text"])
            self.assertEqual(evidence["stale"], False)

            source.write_text("# Cache\n\nChanged body.\n", encoding="utf-8")
            stale = ChunkReader(root).read(chunk_id, with_neighbors=0)
            self.assertEqual(stale["stale"], True)
            self.assertNotIn("Changed body.", stale.get("text", ""))

    def test_read_returns_chunk_not_found_for_unknown_chunk(self):
        with tempfile.TemporaryDirectory() as work:
            root = Path(work)

            missing = ChunkReader(root).read("missing#chunk-001", with_neighbors=0)

            self.assertEqual(missing, {"chunk_id": "missing#chunk-001", "stale": True, "error": "chunk-not-found"})

    def test_read_expands_to_neighbors_from_same_source(self):
        with tempfile.TemporaryDirectory() as work:
            root = Path(work)
            docs = root / "docs"
            docs.mkdir()
            source = docs / "cache.md"
            source.write_text("# One\n\nFirst body.\n\n# Two\n\nSecond body.\n", encoding="utf-8")
            MemoryGenerator(root).generate()

            evidence = ChunkReader(root).read("docs.cache#chunk-002", with_neighbors=1)

            self.assertEqual(evidence["stale"], False)
            self.assertIn("First body.", evidence["text"])
            self.assertIn("Second body.", evidence["text"])

    def test_read_rejects_invalid_chunk_range_without_text(self):
        with tempfile.TemporaryDirectory() as work:
            root = Path(work)
            docs = root / "docs"
            docs.mkdir()
            source = docs / "cache.md"
            source.write_text("# Cache\n\nRedis distributed cache wrapper.\n", encoding="utf-8")
            MemoryGenerator(root).generate()
            chunk_file = root / ".tw-memory" / "generated" / "chunks" / "docs" / "cache.md.generated.json"
            payload = json.loads(chunk_file.read_text(encoding="utf-8"))
            payload["chunks"][0]["end_line"] = 99
            chunk_file.write_text(json.dumps(payload), encoding="utf-8")

            evidence = ChunkReader(root).read("docs.cache#chunk-001", with_neighbors=0)

            self.assertEqual(evidence["stale"], True)
            self.assertEqual(evidence["error"], "invalid-chunk-range")
            self.assertNotIn("Redis distributed cache wrapper.", evidence.get("text", ""))


if __name__ == "__main__":
    unittest.main()
