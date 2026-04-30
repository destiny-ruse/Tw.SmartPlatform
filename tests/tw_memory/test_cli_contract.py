import json
import os
import shutil
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path

from tools.tw_memory_test_support import import_engine


REPO_ROOT = Path(__file__).resolve().parents[2]
CLI = REPO_ROOT / "tools" / "tw-memory" / "tw_memory.py"
engine = import_engine()
MemoryGenerator = engine.generator.MemoryGenerator


class CliContractTests(unittest.TestCase):
    def _temp_repo_root(self, work: str) -> Path:
        root = Path(work)
        (root / "README.md").write_text("# Temp\n", encoding="utf-8")
        (root / "tools").mkdir()
        return root

    def test_help_lists_all_public_commands(self):
        result = subprocess.run(
            [sys.executable, str(CLI), "--help"],
            cwd=REPO_ROOT,
            text=True,
            stdout=subprocess.PIPE,
            stderr=subprocess.PIPE,
            timeout=10,
        )

        self.assertEqual(result.returncode, 0, result.stderr)
        for command in [
            "scan",
            "generate",
            "check",
            "query",
            "read",
            "preflight",
            "postflight",
            "build-search",
            "sync-vector",
        ]:
            self.assertIn(command, result.stdout)

    def test_scan_can_emit_json_without_writing_memory(self):
        memory_root = REPO_ROOT / ".tw-memory"
        sentinel = memory_root / "task1-scan-sentinel.txt"
        before_exists = memory_root.exists()
        before_sentinel_exists = sentinel.exists()
        before_sentinel_contents = sentinel.read_text(encoding="utf-8") if before_sentinel_exists else None

        memory_root.mkdir(exist_ok=True)
        sentinel.write_text("scan must not rewrite this file\n", encoding="utf-8")
        before_files = sorted(
            path.relative_to(memory_root).as_posix()
            for path in memory_root.rglob("*")
            if path.is_file()
        )
        before_contents = sentinel.read_text(encoding="utf-8")

        try:
            result = subprocess.run(
                [sys.executable, str(CLI), "scan", "--format", "json"],
                cwd=REPO_ROOT,
                text=True,
                stdout=subprocess.PIPE,
                stderr=subprocess.PIPE,
                timeout=10,
            )

            self.assertEqual(result.returncode, 0, result.stderr)
            payload = json.loads(result.stdout)
            self.assertEqual(payload["writes"], [])
            self.assertTrue(payload["sources"])
            self.assertIn("source_path", payload["sources"][0])
            self.assertIn("source_hash", payload["sources"][0])
            self.assertTrue(sentinel.exists())
            self.assertEqual(before_contents, sentinel.read_text(encoding="utf-8"))
            self.assertEqual(
                before_files,
                sorted(
                    path.relative_to(memory_root).as_posix()
                    for path in memory_root.rglob("*")
                    if path.is_file()
                ),
            )
        finally:
            if before_sentinel_exists:
                sentinel.write_text(before_sentinel_contents, encoding="utf-8")
            elif sentinel.exists():
                sentinel.unlink()

            if not before_exists and memory_root.exists():
                shutil.rmtree(memory_root)

    def test_sync_vector_rejects_unknown_backend(self):
        result = subprocess.run(
            [sys.executable, str(CLI), "sync-vector", "--backend", "test", "--format", "json"],
            cwd=REPO_ROOT,
            text=True,
            stdout=subprocess.PIPE,
            stderr=subprocess.PIPE,
            timeout=10,
        )

        self.assertEqual(result.returncode, 2)
        self.assertIn('"status": "unknown-backend"', result.stdout)

    def test_sync_vector_is_safe_when_backend_is_disabled(self):
        result = subprocess.run(
            [sys.executable, str(CLI), "sync-vector", "--backend", "aliyun", "--format", "json"],
            cwd=REPO_ROOT,
            text=True,
            stdout=subprocess.PIPE,
            stderr=subprocess.PIPE,
        )

        self.assertEqual(result.returncode, 2)
        self.assertIn('"status": "disabled"', result.stdout)

    def test_check_can_emit_json_diagnostics(self):
        result = subprocess.run(
            [sys.executable, str(CLI), "check", "--format", "json"],
            cwd=REPO_ROOT,
            text=True,
            stdout=subprocess.PIPE,
            stderr=subprocess.PIPE,
            timeout=10,
        )

        self.assertEqual(result.returncode, 0, result.stderr)
        payload = json.loads(result.stdout)
        self.assertIn("diagnostics", payload)
        self.assertIsInstance(payload["diagnostics"], list)
        for diagnostic in payload["diagnostics"]:
            self.assertIn("level", diagnostic)
            self.assertIn("code", diagnostic)
            self.assertIn("message", diagnostic)

    def test_check_warning_only_diagnostics_exit_zero(self):
        with tempfile.TemporaryDirectory() as work:
            root = self._temp_repo_root(work)
            MemoryGenerator(root).generate()
            route_index = root / ".tw-memory" / "route-index" / "index.generated.json"
            route_index.write_text(
                json.dumps(
                    {
                        "schema_version": "1.0.0",
                        "generated_at": "2026-04-30T00:00:00+00:00",
                        "repo_hash": "sha256:test",
                        "shards": [],
                        "padding": "x" * 210_000,
                    }
                ),
                encoding="utf-8",
            )

            result = subprocess.run(
                [sys.executable, str(CLI), "check", "--format", "json"],
                cwd=root,
                text=True,
                stdout=subprocess.PIPE,
                stderr=subprocess.PIPE,
                timeout=10,
            )

            self.assertEqual(result.returncode, 0, result.stderr)
            payload = json.loads(result.stdout)
            self.assertTrue(any(item["level"] == "warning" for item in payload["diagnostics"]))

    def test_check_error_diagnostics_exit_nonzero(self):
        with tempfile.TemporaryDirectory() as work:
            root = self._temp_repo_root(work)
            source_index = root / ".tw-memory" / "source-index" / "docs.generated.json"
            source_index.parent.mkdir(parents=True)
            source_index.write_text("{not-json", encoding="utf-8")

            result = subprocess.run(
                [sys.executable, str(CLI), "check", "--format", "json"],
                cwd=root,
                text=True,
                stdout=subprocess.PIPE,
                stderr=subprocess.PIPE,
                timeout=10,
            )

            self.assertNotEqual(result.returncode, 0)
            payload = json.loads(result.stdout)
            self.assertTrue(any(item["level"] == "error" for item in payload["diagnostics"]))

    def test_generate_can_emit_json_with_paths_and_diagnostics(self):
        result = subprocess.run(
            [sys.executable, str(CLI), "generate", "--format", "json"],
            cwd=REPO_ROOT,
            text=True,
            stdout=subprocess.PIPE,
            stderr=subprocess.PIPE,
            timeout=10,
        )

        self.assertEqual(result.returncode, 0, result.stderr)
        payload = json.loads(result.stdout)
        self.assertGreater(payload["generated_count"], 0)
        self.assertGreater(len(payload["generated_paths"]), 0)
        self.assertEqual(payload["diagnostics"], [])
        self.assertIn(".tw-memory/route-index/index.generated.json", payload["generated_paths"])

    def test_read_emits_utf8_when_console_encoding_is_narrow(self):
        with tempfile.TemporaryDirectory() as work:
            root = self._temp_repo_root(work)
            docs = root / "docs"
            docs.mkdir()
            (docs / "cache.md").write_text("\ufeff# 缓存\n\nRedis 缓存包装。\n", encoding="utf-8")

            generate = subprocess.run(
                [sys.executable, str(CLI), "generate", "--format", "brief"],
                cwd=root,
                stdout=subprocess.PIPE,
                stderr=subprocess.PIPE,
                timeout=10,
            )
            self.assertEqual(generate.returncode, 0, generate.stderr.decode("utf-8", errors="replace"))

            env = os.environ.copy()
            env["PYTHONIOENCODING"] = "cp1252:strict"
            result = subprocess.run(
                [sys.executable, str(CLI), "read", "--chunk-id", "docs.cache#chunk-001"],
                cwd=root,
                env=env,
                stdout=subprocess.PIPE,
                stderr=subprocess.PIPE,
                timeout=10,
            )

            self.assertEqual(result.returncode, 0, result.stderr.decode("utf-8", errors="replace"))
            self.assertIn(b"docs/cache.md", result.stdout)
            self.assertIn("Redis 缓存包装。".encode("utf-8"), result.stdout)


if __name__ == "__main__":
    unittest.main()
