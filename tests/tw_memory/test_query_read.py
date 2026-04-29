import tempfile
import unittest
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


if __name__ == "__main__":
    unittest.main()
