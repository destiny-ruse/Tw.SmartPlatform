import contextlib
import tempfile
import textwrap
import unittest
from pathlib import Path

from tools.knowledge import knowledge


@contextlib.contextmanager
def isolated_repo():
    old_values = {
        "REPO_ROOT": knowledge.REPO_ROOT,
        "KNOWLEDGE_DIR": knowledge.KNOWLEDGE_DIR,
        "GRAPH_DIR": knowledge.GRAPH_DIR,
        "GENERATED_DIR": knowledge.GENERATED_DIR,
        "TAXONOMY_PATH": knowledge.TAXONOMY_PATH,
    }
    with tempfile.TemporaryDirectory() as temp:
        root = Path(temp)
        knowledge.REPO_ROOT = root
        knowledge.KNOWLEDGE_DIR = root / "docs" / "knowledge"
        knowledge.GRAPH_DIR = knowledge.KNOWLEDGE_DIR / "graph"
        knowledge.GENERATED_DIR = knowledge.KNOWLEDGE_DIR / "generated"
        knowledge.TAXONOMY_PATH = knowledge.KNOWLEDGE_DIR / "taxonomy.yaml"
        try:
            yield root
        finally:
            for name, value in old_values.items():
                setattr(knowledge, name, value)


def write_file(root: Path, relative_path: str, content: str) -> Path:
    path = root / relative_path
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(textwrap.dedent(content).lstrip(), encoding="utf-8", newline="\n")
    return path


def write_taxonomy(root: Path) -> None:
    write_file(
        root,
        "docs/knowledge/taxonomy.yaml",
        """
        schema_version: 1.0.0
        valid_kinds:
          - capability
          - module
          - contract
          - integration
          - decision
        valid_statuses:
          - active
          - draft
          - deprecated
          - superseded
        diagnostics:
          knowledge.missing-module:
            severity: error
            message_zh: 新增服务或模块目录尚未声明 module 图谱节点。
            suggestion_zh: 新增对应 module 图谱节点，或在 taxonomy.yaml 中声明忽略规则。
        query_aliases:
          当前用户:
            - auth
            - user
        """,
    )


def write_module(root: Path) -> None:
    write_file(
        root,
        "docs/knowledge/graph/modules/backend.dotnet.services.authentication.yaml",
        """
        schema_version: 1.0.0
        id: backend.dotnet.services.authentication
        kind: module
        name: 认证服务
        status: active
        summary: 承载用户身份认证、登录流程、令牌签发和认证测试相关服务代码。
        owners:
          - platform
        tags:
          - backend
          - dotnet
          - auth
        module_type: microservice
        stack: dotnet
        path: backend/dotnet/Services/Authentication
        source:
          declared_in: docs/knowledge/graph/modules/backend.dotnet.services.authentication.yaml
          evidence:
            - backend/dotnet/Services/Authentication/README.md
        provenance:
          created_by: human
          created_at: 2026-04-27
          updated_by: human
          updated_at: 2026-04-27
        """,
    )


def diagnostic_text(messages):
    return "\n".join(message.format() for message in messages)


class KnowledgeToolTests(unittest.TestCase):
    def test_parse_yaml_subset_supports_nested_maps_and_lists(self):
        payload = knowledge.parse_yaml_subset(
            textwrap.dedent(
                """
                id: backend.dotnet.services.authentication
                kind: module
                owners:
                  - platform
                source:
                  declared_in: docs/knowledge/graph/modules/backend.dotnet.services.authentication.yaml
                  evidence:
                    - backend/dotnet/Services/Authentication/README.md
                """
            ).strip().splitlines()
        )

        self.assertEqual("module", payload["kind"])
        self.assertEqual(["platform"], payload["owners"])
        self.assertEqual(
            ["backend/dotnet/Services/Authentication/README.md"],
            payload["source"]["evidence"],
        )

    def test_parse_yaml_subset_supports_list_of_maps(self):
        payload = knowledge.parse_yaml_subset(
            textwrap.dedent(
                """
                path_rules:
                  - pattern: backend/dotnet/Services/*/**
                    kind: module
                    module_type: microservice
                    stack: dotnet
                """
            ).strip().splitlines()
        )

        self.assertEqual("backend/dotnet/Services/*/**", payload["path_rules"][0]["pattern"])
        self.assertEqual("module", payload["path_rules"][0]["kind"])
        self.assertEqual("microservice", payload["path_rules"][0]["module_type"])
        self.assertEqual("dotnet", payload["path_rules"][0]["stack"])

    def test_validation_reports_missing_required_fields_in_chinese(self):
        with isolated_repo() as root:
            write_taxonomy(root)
            write_file(
                root,
                "docs/knowledge/graph/modules/broken.yaml",
                """
                schema_version: 1.0.0
                id: backend.dotnet.services.broken
                kind: module
                name: Broken
                status: active
                """,
            )

            messages = knowledge.collect_validation_messages()
            text = diagnostic_text(messages)
            self.assertIn("错误 [knowledge.required-field]", text)
            self.assertIn("缺少必填字段 summary", text)

    def test_validation_accepts_minimal_module_node(self):
        with isolated_repo() as root:
            write_taxonomy(root)
            write_file(root, "backend/dotnet/Services/Authentication/README.md", "# 认证服务\n")
            write_module(root)

            messages = knowledge.collect_validation_messages()
            self.assertEqual([], [message for message in messages if message.level == "ERROR"], diagnostic_text(messages))


if __name__ == "__main__":
    unittest.main()
