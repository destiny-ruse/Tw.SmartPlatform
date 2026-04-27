import contextlib
import io
import json
import tempfile
import textwrap
import unittest
from pathlib import Path

from tools.standards import standards


@contextlib.contextmanager
def isolated_repo():
    old_values = {
        "REPO_ROOT": standards.REPO_ROOT,
        "STANDARDS_DIR": standards.STANDARDS_DIR,
        "INDEX_PATH": standards.INDEX_PATH,
        "RULES_DIR": standards.RULES_DIR,
        "TEMPLATES_DIR": standards.TEMPLATES_DIR,
    }
    with tempfile.TemporaryDirectory() as temp:
        root = Path(temp)
        standards.REPO_ROOT = root
        standards.STANDARDS_DIR = root / "docs" / "standards"
        standards.INDEX_PATH = standards.STANDARDS_DIR / "index.generated.json"
        standards.RULES_DIR = root / "tools" / "standards" / "rules"
        standards.TEMPLATES_DIR = root / "tools" / "standards" / "templates"
        try:
            yield root
        finally:
            for name, value in old_values.items():
                setattr(standards, name, value)


def write_file(root: Path, relative_path: str, content: str) -> Path:
    path = root / relative_path
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(textwrap.dedent(content).lstrip(), encoding="utf-8", newline="\n")
    return path


def write_standard(root: Path, relative_path: str, body: str, extra_metadata: str = "") -> Path:
    return write_file(
        root,
        relative_path,
        f"""
        ---
        id: rules.api-response-shape
        title: API 统一响应结构
        doc_type: rule
        status: active
        version: 1.0.0
        owners: [architecture-team]
        roles: [backend, ai]
        stacks: [dotnet, java, python]
        tags: [api, error-handling]
        summary: 规定 REST API 响应结构、空值表达和错误响应关系。
        machine_rules: []
        supersedes: []
        superseded_by:
        review_after: 2026-10-27
        {extra_metadata}
        ---

        # API 统一响应结构

        {body}
        """,
    )


def diagnostic_text(messages):
    return "\n".join(message.format() for message in messages)


def error_messages(messages):
    return [message for message in messages if message.level == "ERROR"]


class StandardsToolV2Tests(unittest.TestCase):
    def test_v2_metadata_accepts_required_fields_and_rejects_applies_to(self):
        with isolated_repo() as root:
            write_standard(
                root,
                "docs/standards/rules/api-response-shape.md",
                """
                <!-- anchor: rules -->
                ## 规则

                响应对象必须保持稳定结构。
                """,
            )

            messages = standards.collect_validation_messages()
            self.assertEqual([], error_messages(messages), diagnostic_text(messages))

            write_standard(
                root,
                "docs/standards/rules/api-response-shape.md",
                """
                <!-- anchor: rules -->
                ## 规则

                响应对象必须保持稳定结构。
                """,
                extra_metadata="applies_to: [api]",
            )

            messages = standards.collect_validation_messages()
            self.assertIn('field "applies_to" is not allowed in v2', diagnostic_text(messages))

    def test_v2_metadata_reports_missing_routing_fields(self):
        with isolated_repo() as root:
            write_file(
                root,
                "docs/standards/rules/git-commit.md",
                """
                ---
                id: rules.git-commit
                title: Git 提交规范
                status: active
                version: 1.0.0
                owners: [architecture-team]
                machine_rules: []
                supersedes: []
                superseded_by:
                review_after: 2026-10-27
                ---

                # Git 提交规范

                <!-- anchor: rules -->
                ## 规则

                提交信息必须描述可审查的变更。
                """,
            )

            messages = standards.collect_validation_messages()
            text = diagnostic_text(messages)
            self.assertIn('missing required field "doc_type"', text)
            self.assertIn('missing required field "roles"', text)
            self.assertIn('missing required field "stacks"', text)
            self.assertIn('missing required field "tags"', text)
            self.assertIn('missing required field "summary"', text)

    def test_explicit_anchor_and_region_index(self):
        with isolated_repo() as root:
            write_standard(
                root,
                "docs/standards/rules/api-response-shape.md",
                """
                <!-- anchor: rules -->
                ## 规则

                <!-- region: no-null -->
                ### 禁止返回 null

                REST API 响应字段不得返回 `null`。
                <!-- endregion: no-null -->
                """,
            )

            docs, messages = standards.load_standard_docs()
            self.assertEqual([], error_messages(messages), diagnostic_text(messages))
            section_index, section_messages = standards.build_section_index(docs[0])
            self.assertEqual([], error_messages(section_messages), diagnostic_text(section_messages))
            self.assertEqual("rules", section_index["sections"][0]["anchor"])
            self.assertEqual("规则", section_index["sections"][0]["title"])
            self.assertEqual("no-null", section_index["sections"][0]["regions"][0]["id"])
            self.assertLess(
                section_index["sections"][0]["regions"][0]["start_line"],
                section_index["sections"][0]["regions"][0]["end_line"],
            )

    def test_build_indexes_creates_l0_l1_and_l2_without_section_details_in_l0(self):
        with isolated_repo() as root:
            write_standard(
                root,
                "docs/standards/rules/api-response-shape.md",
                """
                <!-- anchor: rules -->
                ## 规则

                响应对象必须保持稳定结构。
                """,
            )

            payloads, messages = standards.build_indexes(existing_generated_at="2026-04-27T00:00:00Z")
            self.assertEqual([], error_messages(messages), diagnostic_text(messages))
            self.assertIn("docs/standards/index.generated.json", payloads)
            self.assertIn("docs/standards/_index/by-role/backend.generated.json", payloads)
            self.assertIn("docs/standards/_index/by-stack/dotnet.generated.json", payloads)
            self.assertIn("docs/standards/_index/by-doc-type/rule.generated.json", payloads)
            self.assertIn("docs/standards/_index/by-tag/api.generated.json", payloads)
            self.assertIn("docs/standards/_index/sections/rules.api-response-shape.generated.json", payloads)

            l0_standard = payloads["docs/standards/index.generated.json"]["standards"][0]
            self.assertNotIn("sections", l0_standard)
            self.assertEqual(
                "docs/standards/_index/sections/rules.api-response-shape.generated.json",
                l0_standard["sections_index"],
            )
            self.assertIn(
                "docs/standards/_index/by-role/backend.generated.json",
                l0_standard["shards"],
            )

    def test_index_check_detects_missing_generated_shard(self):
        with isolated_repo() as root:
            write_standard(
                root,
                "docs/standards/rules/api-response-shape.md",
                """
                <!-- anchor: rules -->
                ## 规则

                响应对象必须保持稳定结构。
                """,
            )
            payloads, messages = standards.build_indexes(existing_generated_at="2026-04-27T00:00:00Z")
            self.assertEqual([], error_messages(messages), diagnostic_text(messages))
            standards.write_indexes(payloads)

            (root / "docs/standards/_index/by-role/backend.generated.json").unlink()
            messages = standards.collect_index_messages()
            self.assertIn("generated index file does not exist; run generate-index", diagnostic_text(messages))

    def test_check_links_validates_standard_references(self):
        with isolated_repo() as root:
            write_standard(
                root,
                "docs/standards/rules/api-response-shape.md",
                """
                <!-- anchor: rules -->
                ## 规则

                <!-- region: no-null -->
                ### 禁止返回 null

                REST API 响应字段不得返回 `null`。
                <!-- endregion: no-null -->
                """,
            )
            payloads, messages = standards.build_indexes(existing_generated_at="2026-04-27T00:00:00Z")
            self.assertEqual([], error_messages(messages), diagnostic_text(messages))
            standards.write_indexes(payloads)

            write_file(
                root,
                "docs/rfcs/example.md",
                """
                # RFC: API 响应

                有效引用：`rules.api-response-shape#rules:no-null`
                无效引用：`rules.api-response-shape#rules:missing`
                """,
            )

            messages = standards.collect_link_messages()
            text = diagnostic_text(messages)
            self.assertIn('unknown standard reference "rules.api-response-shape#rules:missing"', text)
            self.assertNotIn('rules.api-response-shape#rules:no-null"', text)

    def test_new_standard_uses_v2_template_fields(self):
        with isolated_repo() as root:
            write_file(
                root,
                "tools/standards/templates/standard.md",
                """
                ---
                id: {{id}}
                title: {{title}}
                doc_type: {{doc_type}}
                status: {{status}}
                version: 0.1.0
                owners: [{{owner}}]
                roles: [{{roles}}]
                stacks: [{{stacks}}]
                tags: [{{tags}}]
                summary: {{summary}}
                machine_rules: []
                supersedes: []
                superseded_by:
                review_after: {{review_after}}
                ---

                # {{title}}
                """,
            )

            args = standards.build_parser().parse_args(
                [
                    "new-standard",
                    "--id",
                    "rules.example",
                    "--title",
                    "示例规范",
                    "--doc-type",
                    "rule",
                    "--roles",
                    "backend,ai",
                    "--stacks",
                    "dotnet",
                    "--tags",
                    "api,contract",
                    "--summary",
                    "用于验证 v2 模板。",
                    "--path",
                    "docs/standards/rules/example.md",
                ]
            )

            with contextlib.redirect_stdout(io.StringIO()):
                self.assertEqual(0, args.func(args))
            content = (root / "docs/standards/rules/example.md").read_text(encoding="utf-8")
            self.assertIn("doc_type: rule", content)
            self.assertIn("roles: [backend, ai]", content)
            self.assertIn("stacks: [dotnet]", content)
            self.assertIn("tags: [api, contract]", content)
            self.assertIn("summary: 用于验证 v2 模板。", content)
            self.assertNotIn("applies_to:", content)


if __name__ == "__main__":
    unittest.main()
