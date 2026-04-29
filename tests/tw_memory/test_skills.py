import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[2]


class ThinSkillTests(unittest.TestCase):
    def test_language_skills_are_thin_and_call_cli(self):
        for name, stack, path_root in [
            ("tw-frontend", "frontend", "frontend/**"),
            ("tw-dotnet", "dotnet", "backend/dotnet/**"),
            ("tw-java", "java", "backend/java/**"),
            ("tw-python", "python", "backend/python/**"),
        ]:
            path = REPO_ROOT / ".agents" / "skills" / name / "SKILL.md"
            text = path.read_text(encoding="utf-8")
            description = next(line for line in text.splitlines() if line.startswith("description: "))

            self.assertIn(f"name: {name}", text)
            self.assertIn("python tools\\tw-memory\\tw_memory.py preflight", text)
            self.assertIn("python tools\\tw-memory\\tw_memory.py query", text)
            self.assertIn("python tools\\tw-memory\\tw_memory.py read", text)
            self.assertIn("python tools\\tw-memory\\tw_memory.py postflight", text)
            self.assertIn("python tools\\tw-memory\\tw_memory.py generate", text)
            self.assertIn("python tools\\tw-memory\\tw_memory.py check", text)
            self.assertIn(f"--stack {stack}", text)
            self.assertIn("Read only the candidates returned by `preflight`", text)
            self.assertIn("Prefer internal", text)
            self.assertIn("company standards before direct third-party usage", text)
            self.assertIn("Do not open generated route shards for full parsing", text)
            self.assertIn("Do not copy manuals or source bodies into `.tw-memory`", text)
            self.assertIn(path_root, description)
            self.assertNotIn("README files, or tests", description)
            self.assertNotIn("reading project memory", description)
            self.assertNotIn(".tw-memory/source-index", text)
            self.assertNotIn(".tw-memory\\source-index", text)
            self.assertNotIn(".tw-memory/route-index", text)
            self.assertNotIn(".tw-memory\\route-index", text)

    def test_tw_memory_skill_is_for_memory_system_only(self):
        text = (REPO_ROOT / ".agents" / "skills" / "tw-memory" / "SKILL.md").read_text(encoding="utf-8")

        self.assertIn("Use only when maintaining the TW memory system itself", text)
        self.assertIn("not for ordinary product code", text)
        for boundary in [
            "manual bodies",
            "standards bodies",
            "source copies",
            "third-party docs",
            "source archives",
            "chat logs",
            "secrets",
            "SQLite files",
            "vector caches",
            "web captures",
        ]:
            self.assertIn(boundary, text)
        self.assertIn("python tools\\tw-memory\\tw_memory.py generate", text)
        self.assertIn("python tools\\tw-memory\\tw_memory.py check", text)
        self.assertIn("python -m unittest discover tests\\tw_memory -v", text)
