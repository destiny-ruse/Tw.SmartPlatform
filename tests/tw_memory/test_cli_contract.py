import json
import shutil
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[2]
CLI = REPO_ROOT / "tools" / "tw-memory" / "tw_memory.py"


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

    def test_unimplemented_command_exits_nonzero(self):
        result = subprocess.run(
            [sys.executable, str(CLI), "preflight", "--task", "x"],
            cwd=REPO_ROOT,
            text=True,
            stdout=subprocess.PIPE,
            stderr=subprocess.PIPE,
            timeout=10,
        )

        self.assertNotEqual(result.returncode, 0)
        self.assertIn("not implemented", result.stderr)

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
            route_index = root / ".tw-memory" / "route-index" / "index.generated.json"
            route_index.parent.mkdir(parents=True)
            route_index.write_text(json.dumps({"shards": [], "padding": "x" * 210_000}), encoding="utf-8")

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


if __name__ == "__main__":
    unittest.main()
