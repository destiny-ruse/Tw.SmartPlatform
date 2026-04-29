import tempfile
import unittest
from pathlib import Path

from tools.tw_memory_test_support import import_engine


engine = import_engine()
SourceScanner = engine.scanner.SourceScanner


class ScannerTests(unittest.TestCase):
    def test_scan_finds_docs_readmes_package_files_and_language_roots(self):
        with tempfile.TemporaryDirectory() as work:
            root = Path(work)
            (root / "docs").mkdir()
            (root / "docs" / "README.md").write_text("# Docs\n", encoding="utf-8")
            (root / "backend" / "dotnet" / "ServiceA").mkdir(parents=True)
            (root / "backend" / "dotnet" / "ServiceA" / "README.md").write_text("# Service A\n", encoding="utf-8")
            (root / "frontend").mkdir()
            (root / "frontend" / "package.json").write_text('{"name":"frontend"}\n', encoding="utf-8")
            (root / ".tw-memory").mkdir()
            (root / ".tw-memory" / "old.generated.json").write_text("{}", encoding="utf-8")

            records = SourceScanner(root).scan()
            paths = {record.source_path for record in records}

            self.assertIn("docs/README.md", paths)
            self.assertIn("backend/dotnet/ServiceA/README.md", paths)
            self.assertIn("frontend/package.json", paths)
            self.assertNotIn(".tw-memory/old.generated.json", paths)

    def test_scan_records_hash_type_language_and_service(self):
        with tempfile.TemporaryDirectory() as work:
            root = Path(work)
            (root / "backend" / "dotnet" / "Services" / "Notice").mkdir(parents=True)
            path = root / "backend" / "dotnet" / "Services" / "Notice" / "README.md"
            path.write_text("# Notice Service\n", encoding="utf-8")

            [record] = SourceScanner(root).scan()

            self.assertEqual(record.source_type, "readme")
            self.assertEqual(record.language, "dotnet")
            self.assertEqual(record.service, "notice")
            self.assertTrue(record.source_hash.startswith("sha256:"))


if __name__ == "__main__":
    unittest.main()
