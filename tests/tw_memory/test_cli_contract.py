import json
import shutil
import subprocess
import sys
import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[2]
CLI = REPO_ROOT / "tools" / "tw-memory" / "tw_memory.py"


class CliContractTests(unittest.TestCase):
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
            self.assertEqual(json.loads(result.stdout), {"sources": [], "writes": []})
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
            [sys.executable, str(CLI), "generate"],
            cwd=REPO_ROOT,
            text=True,
            stdout=subprocess.PIPE,
            stderr=subprocess.PIPE,
            timeout=10,
        )

        self.assertNotEqual(result.returncode, 0)
        self.assertIn("not implemented", result.stderr)


if __name__ == "__main__":
    unittest.main()
