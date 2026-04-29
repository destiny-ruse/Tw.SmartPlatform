import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[2]


class ThinSkillTests(unittest.TestCase):
    def test_language_skills_are_thin_and_call_cli(self):
        for name, stack in [
            ("tw-frontend", "frontend"),
            ("tw-dotnet", "dotnet"),
            ("tw-java", "java"),
            ("tw-python", "python"),
        ]:
            path = REPO_ROOT / ".agents" / "skills" / name / "SKILL.md"
            text = path.read_text(encoding="utf-8")

            self.assertIn(f"name: {name}", text)
            self.assertIn("python tools\\tw-memory\\tw_memory.py preflight", text)
            self.assertIn("python tools\\tw-memory\\tw_memory.py postflight", text)
            self.assertIn(f"--stack {stack}", text)
            self.assertNotIn(".tw-memory/source-index/", text)
            self.assertNotIn(".tw-memory/route-index/", text)

    def test_tw_memory_skill_is_for_memory_system_only(self):
        text = (REPO_ROOT / ".agents" / "skills" / "tw-memory" / "SKILL.md").read_text(encoding="utf-8")

        self.assertIn("Use only when maintaining the TW memory system itself", text)
        self.assertIn("generate", text)
        self.assertIn("check", text)
