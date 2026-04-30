import tempfile
import unittest
from pathlib import Path

from tools.tw_memory_test_support import import_engine


engine = import_engine()
MemoryGenerator = engine.generator.MemoryGenerator
PreflightRunner = engine.preflight.PreflightRunner
PostflightRunner = engine.postflight.PostflightRunner


class PreflightPostflightTests(unittest.TestCase):
    def test_preflight_is_read_only_and_reports_missing_memory(self):
        with tempfile.TemporaryDirectory() as work:
            root = Path(work)
            (root / "docs").mkdir()
            (root / "docs" / "README.md").write_text("# Docs\n", encoding="utf-8")

            result = PreflightRunner(root).run(task="cache wrapper", stack="dotnet", path="backend/dotnet")

            self.assertIn("generate", result["actions"])
            self.assertFalse((root / ".tw-memory").exists())

    def test_preflight_returns_candidates_when_memory_exists(self):
        with tempfile.TemporaryDirectory() as work:
            root = Path(work)
            docs = root / "docs"
            docs.mkdir()
            (docs / "cache.md").write_text("# Cache\n\nRedis cache wrapper.\n", encoding="utf-8")
            MemoryGenerator(root).generate()

            result = PreflightRunner(root).run(task="redis cache wrapper", stack=None, path="docs/cache.md")

            self.assertEqual(result["status"], "ok")
            self.assertGreaterEqual(len(result["candidates"]), 1)

    def test_preflight_reports_build_search_when_fts_is_missing(self):
        with tempfile.TemporaryDirectory() as work:
            root = Path(work)
            docs = root / "docs"
            docs.mkdir()
            (docs / "cache.md").write_text("# Cache\n\nRedis cache wrapper.\n", encoding="utf-8")
            MemoryGenerator(root).generate()

            result = PreflightRunner(root).run(task="redis cache wrapper", stack=None, path="docs/cache.md")

            self.assertEqual(result["status"], "ok")
            self.assertIn("build-search", result["actions"])
            self.assertGreaterEqual(len(result["candidates"]), 1)

    def test_preflight_filters_candidates_by_requested_stack(self):
        with tempfile.TemporaryDirectory() as work:
            root = Path(work)
            dotnet = root / "backend" / "dotnet" / "BuildingBlocks" / "src" / "Tw.Caching"
            java = root / "backend" / "java" / "orders"
            dotnet.mkdir(parents=True)
            java.mkdir(parents=True)
            (dotnet / "README.md").write_text("# Dotnet Cache\n\nCaching building blocks.\n", encoding="utf-8")
            (java / "README.md").write_text("# Java Orders\n\nJava order service.\n", encoding="utf-8")
            MemoryGenerator(root).generate()

            result = PreflightRunner(root).run(
                task="dotnet caching building blocks",
                stack="java",
                path="backend/java",
            )

            candidate_paths = [candidate["source_path"] for candidate in result["candidates"]]
            self.assertGreaterEqual(len(candidate_paths), 1)
            self.assertFalse(
                any(path.startswith("backend/dotnet/") for path in candidate_paths),
                candidate_paths,
            )

    def test_preflight_includes_global_standards_with_stack_filter(self):
        with tempfile.TemporaryDirectory() as work:
            root = Path(work)
            dotnet = root / "backend" / "dotnet" / "BuildingBlocks" / "src" / "Tw.Caching"
            standard = root / "docs" / "standards" / "rules"
            dotnet.mkdir(parents=True)
            standard.mkdir(parents=True)
            (dotnet / "README.md").write_text("# Dotnet Cache\n\nCaching building blocks.\n", encoding="utf-8")
            (standard / "api-response-shape.md").write_text(
                "# API Response Shape\n\nResponse envelope standard.\n",
                encoding="utf-8",
            )
            MemoryGenerator(root).generate()

            result = PreflightRunner(root).run(
                task="api response shape for dotnet cache",
                stack="dotnet",
                path="backend/dotnet/BuildingBlocks/src/Tw.Caching",
            )

            candidate_paths = [candidate["source_path"] for candidate in result["candidates"]]
            self.assertIn("backend/dotnet/BuildingBlocks/src/Tw.Caching/README.md", candidate_paths)
            self.assertIn("docs/standards/rules/api-response-shape.md", candidate_paths)

    def test_preflight_query_normalizes_cli_path_forms_for_language_and_service_terms(self):
        runner = PreflightRunner(Path("."))

        for path in (
            "./backend/dotnet/Services/Orders/Foo.cs",
            r".\backend\dotnet\Services\Orders\Foo.cs",
            "  backend//dotnet//Services//Orders//Foo.cs  ",
        ):
            with self.subTest(path=path):
                query = runner._semantic_query(task="update command handler", stack=None, path=path)

                self.assertIn("Foo.cs", query)
                self.assertIn("dotnet", query)
                self.assertIn("orders", query)

    def test_postflight_classifies_memory_impact(self):
        result = PostflightRunner(Path(".")).run(
            [
                "docs/standards/rules/cache.md",
                "backend/dotnet/BuildingBlocks/src/Tw.Caching/README.md",
                "backend/dotnet/BuildingBlocks/src/Tw.Caching/CacheClient.cs",
                "frontend/apps/tw.web.ops/src/view.vue",
            ]
        )

        self.assertIn("docs/standards/rules/cache.md", result["memory_affecting_files"])
        self.assertIn(
            "backend/dotnet/BuildingBlocks/src/Tw.Caching/README.md",
            result["memory_affecting_files"],
        )
        self.assertIn(
            "backend/dotnet/BuildingBlocks/src/Tw.Caching/CacheClient.cs",
            result["review_required_files"],
        )
        self.assertIn("generate", result["actions"])
        self.assertIn("check", result["actions"])

    def test_postflight_classifies_memory_metadata_and_review_only_actions(self):
        metadata_result = PostflightRunner(Path(".")).run([".tw-memory/taxonomy.yaml"])

        self.assertEqual(metadata_result["memory_affecting_files"], [".tw-memory/taxonomy.yaml"])
        self.assertEqual(metadata_result["actions"], ["generate", "check"])

        review_result = PostflightRunner(Path(".")).run(
            [
                "backend/dotnet/BuildingBlocks/src/CacheClient.cs",
                "backend/java/src/Main.java",
                "backend/python/orders/app.py",
                "frontend/packages/ui/src/Button.tsx",
            ]
        )

        self.assertEqual(
            review_result["review_required_files"],
            [
                "backend/dotnet/BuildingBlocks/src/CacheClient.cs",
                "backend/java/src/Main.java",
                "backend/python/orders/app.py",
                "frontend/packages/ui/src/Button.tsx",
            ],
        )
        self.assertEqual(review_result["actions"], ["review-manual-sync", "postflight-again"])


if __name__ == "__main__":
    unittest.main()
