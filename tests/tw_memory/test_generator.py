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
