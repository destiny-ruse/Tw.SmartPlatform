import tempfile
import unittest
from pathlib import Path
from unittest import mock

from tools.tw_memory_test_support import import_engine


engine = import_engine()
SourceScanner = engine.scanner.SourceScanner
GeneratorInfo = engine.models.GeneratorInfo
file_sha256 = engine.hashing.file_sha256


class ScannerTests(unittest.TestCase):
    def test_generator_info_serializes_to_json(self):
        self.assertEqual(
            GeneratorInfo("tw-memory", "1.0.0").to_json(),
            {"name": "tw-memory", "version": "1.0.0"},
        )

    def test_scan_finds_docs_readmes_package_files_and_language_roots(self):
        with tempfile.TemporaryDirectory() as work:
            root = Path(work)
            (root / "docs").mkdir()
            (root / "docs" / "README.md").write_text("# Docs\n", encoding="utf-8")
            (root / "docs-old").mkdir()
            (root / "docs-old" / "README.md").write_text("# Legacy Docs\n", encoding="utf-8")
            (root / "backend" / "dotnet" / "ServiceA").mkdir(parents=True)
            (root / "backend" / "dotnet" / "ServiceA" / "README.md").write_text("# Service A\n", encoding="utf-8")
            (root / "frontend").mkdir()
            (root / "frontend" / "package.json").write_text('{"name":"frontend"}\n', encoding="utf-8")
            (root / ".tw-memory").mkdir()
            (root / ".tw-memory" / "old.generated.json").write_text("{}", encoding="utf-8")

            records = SourceScanner(root).scan()
            paths = {record.source_path for record in records}

            self.assertIn("docs/README.md", paths)
            self.assertNotIn("docs-old/README.md", paths)
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

    def test_scan_classifies_service_marker_as_service_directory(self):
        with tempfile.TemporaryDirectory() as work:
            root = Path(work)
            (root / "backend" / "dotnet" / "Services" / "Notice").mkdir(parents=True)
            path = root / "backend" / "dotnet" / "Services" / "Notice" / "SERVICE.md"
            path.write_text("# Notice Service Marker\n", encoding="utf-8")

            [record] = SourceScanner(root).scan()

            self.assertEqual(record.source_path, "backend/dotnet/Services/Notice/SERVICE.md")
            self.assertEqual(record.source_type, "service-directory")
            self.assertEqual(record.service, "notice")

    def test_scan_includes_agent_skill_files_as_skills(self):
        with tempfile.TemporaryDirectory() as work:
            root = Path(work)
            path = root / ".agents" / "skills" / "tw-dotnet" / "SKILL.md"
            path.parent.mkdir(parents=True)
            path.write_text("---\nname: tw-dotnet\n---\n# TW Dotnet\n", encoding="utf-8")

            [record] = SourceScanner(root).scan()

            self.assertEqual(record.source_path, ".agents/skills/tw-dotnet/SKILL.md")
            self.assertEqual(record.source_type, "skill")

    def test_scan_prunes_without_path_rglob(self):
        with tempfile.TemporaryDirectory() as work:
            root = Path(work)
            (root / "docs").mkdir()
            (root / "docs" / "README.md").write_text("# Docs\n", encoding="utf-8")

            with mock.patch.object(Path, "rglob", side_effect=AssertionError("rglob should not be used")):
                records = SourceScanner(root).scan()

            self.assertEqual([record.source_path for record in records], ["docs/README.md"])

    def test_scan_excludes_ignored_directories_and_generated_prefixes(self):
        with tempfile.TemporaryDirectory() as work:
            root = Path(work)
            (root / "README.md").write_text("# Root\n", encoding="utf-8")

            excluded_paths = [
                ".git/README.md",
                ".worktrees/README.md",
                "frontend/node_modules/package.json",
                "backend/dotnet/Services/Notice/bin/README.md",
                "backend/dotnet/Services/Notice/obj/README.md",
                "backend/python/Notice/__pycache__/README.md",
                ".tw-memory/README.md",
                ".tw-memory/generated/fts/README.md",
                ".tw-memory/generated/vector/README.md",
            ]
            for source_path in excluded_paths:
                path = root / source_path
                path.parent.mkdir(parents=True, exist_ok=True)
                path.write_text("# Excluded\n", encoding="utf-8")

            records = SourceScanner(root).scan()
            paths = {record.source_path for record in records}

            self.assertEqual(paths, {"README.md"})

    def test_file_sha256_hashes_raw_bytes(self):
        with tempfile.TemporaryDirectory() as work:
            path = Path(work) / "README.md"
            path.write_bytes(b"# Docs\r\n")
            crlf_hash = file_sha256(path)
            path.write_bytes(b"# Docs\n")
            lf_hash = file_sha256(path)

            self.assertNotEqual(crlf_hash, lf_hash)


if __name__ == "__main__":
    unittest.main()
