import argparse
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

    def test_validation_requires_source_declared_in(self):
        with isolated_repo() as root:
            write_taxonomy(root)
            write_file(root, "backend/dotnet/Services/Authentication/README.md", "# 认证服务\n")
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
                source:
                  evidence:
                    - backend/dotnet/Services/Authentication/README.md
                provenance:
                  created_by: human
                  created_at: 2026-04-27
                  updated_by: human
                  updated_at: 2026-04-27
                """,
            )

            messages = knowledge.collect_validation_messages()
            text = diagnostic_text(messages)
            self.assertIn("错误 [knowledge.declared-in]", text)
            self.assertIn("source.declared_in", text)

    def test_validation_requires_source_evidence(self):
        with isolated_repo() as root:
            write_taxonomy(root)
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
                source:
                  declared_in: docs/knowledge/graph/modules/backend.dotnet.services.authentication.yaml
                provenance:
                  created_by: human
                  created_at: 2026-04-27
                  updated_by: human
                  updated_at: 2026-04-27
                """,
            )

            messages = knowledge.collect_validation_messages()
            text = diagnostic_text(messages)
            self.assertIn("错误 [knowledge.required-field]", text)
            self.assertIn("source.evidence", text)

    def test_validation_requires_provenance_fields(self):
        with isolated_repo() as root:
            write_taxonomy(root)
            write_file(root, "backend/dotnet/Services/Authentication/README.md", "# 认证服务\n")
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
                source:
                  declared_in: docs/knowledge/graph/modules/backend.dotnet.services.authentication.yaml
                  evidence:
                    - backend/dotnet/Services/Authentication/README.md
                provenance:
                  created_at: 2026-04-27
                """,
            )

            messages = knowledge.collect_validation_messages()
            text = diagnostic_text(messages)
            self.assertIn("错误 [knowledge.required-field]", text)
            self.assertIn("provenance.created_by", text)
            self.assertIn("provenance.updated_by", text)
            self.assertIn("provenance.updated_at", text)

    def test_validation_reports_mismatched_source_declared_in(self):
        with isolated_repo() as root:
            write_taxonomy(root)
            write_file(root, "backend/dotnet/Services/Authentication/README.md", "# 认证服务\n")
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
                source:
                  declared_in: docs/knowledge/graph/modules/wrong.yaml
                  evidence:
                    - backend/dotnet/Services/Authentication/README.md
                provenance:
                  created_by: human
                  created_at: 2026-04-27
                  updated_by: human
                  updated_at: 2026-04-27
                """,
            )

            messages = knowledge.collect_validation_messages()
            text = diagnostic_text(messages)
            self.assertIn("错误 [knowledge.declared-in]", text)
            self.assertIn("source.declared_in", text)

    def test_validation_reports_missing_evidence_file_as_warning(self):
        with isolated_repo() as root:
            write_taxonomy(root)
            write_module(root)

            messages = knowledge.collect_validation_messages()
            self.assertEqual([], [message for message in messages if message.level == "ERROR"], diagnostic_text(messages))
            self.assertIn("警告 [knowledge.missing-evidence]", diagnostic_text(messages))

    def test_validation_accepts_minimal_module_node(self):
        with isolated_repo() as root:
            write_taxonomy(root)
            write_file(root, "backend/dotnet/Services/Authentication/README.md", "# 认证服务\n")
            write_module(root)

            messages = knowledge.collect_validation_messages()
            self.assertEqual([], [message for message in messages if message.level == "ERROR"], diagnostic_text(messages))

    def test_build_indexes_creates_l0_l1_l2_memory_and_edges(self):
        with isolated_repo() as root:
            write_taxonomy(root)
            write_file(root, "backend/dotnet/Services/Authentication/README.md", "# 认证服务\n")
            write_module(root)
            write_file(
                root,
                "docs/knowledge/graph/capabilities/backend.capability.authentication.yaml",
                """
                schema_version: 1.0.0
                id: backend.capability.authentication
                kind: capability
                name: 用户认证能力
                status: active
                summary: 提供用户登录、令牌签发和认证主体解析能力。
                owners:
                  - platform
                tags:
                  - backend
                  - auth
                domain: security
                standards:
                  - rules.auth-oauth-oidc#rules
                provided_by:
                  modules:
                    - backend.dotnet.services.authentication
                reuse:
                  use_when:
                    - 需要用户登录或令牌签发
                  do_not_reimplement:
                    - 不要在业务服务中自行签发访问令牌
                source:
                  declared_in: docs/knowledge/graph/capabilities/backend.capability.authentication.yaml
                  evidence:
                    - backend/dotnet/Services/Authentication/README.md
                provenance:
                  created_by: human
                  created_at: 2026-04-27
                  updated_by: human
                  updated_at: 2026-04-27
                """,
            )

            payloads, messages = knowledge.build_indexes(existing_generated_at="2026-04-27T00:00:00Z")
            self.assertEqual([], [message for message in messages if message.level == "ERROR"], diagnostic_text(messages))
            self.assertIn("docs/knowledge/generated/index.generated.json", payloads)
            self.assertIn("docs/knowledge/generated/memory.generated.json", payloads)
            self.assertIn("docs/knowledge/generated/edges.generated.json", payloads)
            self.assertIn("docs/knowledge/generated/_index/by-kind/module.generated.json", payloads)
            self.assertIn("docs/knowledge/generated/_index/by-kind/capability.generated.json", payloads)
            self.assertIn("docs/knowledge/generated/_index/by-tag/auth.generated.json", payloads)
            self.assertIn("docs/knowledge/generated/_index/sections/backend.capability.authentication.generated.json", payloads)

            for path in [
                "docs/knowledge/generated/index.generated.json",
                "docs/knowledge/generated/memory.generated.json",
                "docs/knowledge/generated/edges.generated.json",
                "docs/knowledge/generated/_index/by-kind/capability.generated.json",
                "docs/knowledge/generated/_index/sections/backend.capability.authentication.generated.json",
            ]:
                self.assertEqual("1.0.0", payloads[path]["schema_version"])

            l0_node = payloads["docs/knowledge/generated/index.generated.json"]["nodes"][0]
            self.assertEqual(
                {"id", "kind", "name", "summary", "tags", "path", "sections_index", "shards"},
                set(l0_node),
            )
            self.assertEqual(
                "docs/knowledge/graph/capabilities/backend.capability.authentication.yaml",
                l0_node["path"],
            )
            self.assertNotIn("reuse", l0_node)
            self.assertNotIn("status", l0_node)
            self.assertNotIn("owners", l0_node)
            self.assertNotIn("domain", l0_node)
            self.assertNotIn("stack", l0_node)
            self.assertNotIn("module_type", l0_node)
            self.assertNotIn("source_path", l0_node)
            self.assertIn("sections_index", l0_node)
            section_payload = payloads["docs/knowledge/generated/_index/sections/backend.capability.authentication.generated.json"]
            self.assertIn("sections", section_payload)
            self.assertNotIn("fields", section_payload)
            summary_section = next(section for section in section_payload["sections"] if section["key"] == "summary")
            self.assertIn("key", summary_section)
            self.assertNotIn("field", summary_section)
            edges = payloads["docs/knowledge/generated/edges.generated.json"]["edges"]
            self.assertIn(
                {
                    "from": "backend.dotnet.services.authentication",
                    "type": "provides",
                    "to": "backend.capability.authentication",
                },
                edges,
            )
            self.assertIn(
                {
                    "from": "backend.capability.authentication",
                    "type": "governed_by",
                    "to": "rules.auth-oauth-oidc#rules",
                },
                edges,
            )

    def test_build_indexes_returns_empty_payloads_when_validation_has_errors(self):
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

            payloads, messages = knowledge.build_indexes(existing_generated_at="2026-04-27T00:00:00Z")

            self.assertEqual({}, payloads)
            self.assertNotEqual([], [message for message in messages if message.level == "ERROR"])

    def test_generate_writes_indexes_and_check_detects_stale_generated_file(self):
        with isolated_repo() as root:
            write_taxonomy(root)
            write_file(root, "backend/dotnet/Services/Authentication/README.md", "# 认证服务\n")
            write_module(root)

            payloads, messages = knowledge.build_indexes(existing_generated_at="2026-04-27T00:00:00Z")
            self.assertEqual([], [message for message in messages if message.level == "ERROR"], diagnostic_text(messages))
            knowledge.write_indexes(payloads)

            index_path = root / "docs/knowledge/generated/index.generated.json"
            self.assertTrue(index_path.exists())
            index_path.write_text('{"broken": true}\n', encoding="utf-8")

            messages = knowledge.collect_index_messages()
            self.assertIn("生成索引不是最新", diagnostic_text(messages))

    def test_command_generate_writes_empty_generated_indexes(self):
        with isolated_repo() as root:
            write_taxonomy(root)

            exit_code = knowledge.command_generate(argparse.Namespace())

            self.assertEqual(0, exit_code)
            self.assertTrue((root / "docs/knowledge/generated/index.generated.json").exists())
            self.assertTrue((root / "docs/knowledge/generated/memory.generated.json").exists())
            self.assertTrue((root / "docs/knowledge/generated/edges.generated.json").exists())

    def test_query_returns_ranked_summaries_and_read_targets(self):
        with isolated_repo() as root:
            write_taxonomy(root)
            write_file(root, "backend/dotnet/Services/Authentication/README.md", "# 认证服务\n")
            write_module(root)

            results = knowledge.query_nodes("当前用户", limit=5)

            self.assertEqual(1, len(results))
            self.assertEqual("backend.dotnet.services.authentication", results[0]["id"])
            self.assertIn("docs/knowledge/generated/_index/sections/backend.dotnet.services.authentication.generated.json", results[0]["read"][0])
            self.assertEqual({"id", "kind", "name", "summary", "read"}, set(results[0]))
            self.assertNotIn("source", results[0])
            self.assertNotIn("provenance", results[0])
            self.assertNotIn("provides", results[0])

    def test_detect_drift_reports_new_service_without_module_node(self):
        with isolated_repo() as root:
            write_taxonomy(root)
            write_file(root, "backend/dotnet/Services/User/README.md", "# 用户服务\n")

            diagnostics = knowledge.detect_drift_from_paths(["backend/dotnet/Services/User/README.md"])

            self.assertEqual("knowledge.missing-module", diagnostics[0].code)
            self.assertIn("新增服务或模块目录尚未声明 module 图谱节点", diagnostics[0].message_zh)

    def test_detect_drift_reports_building_block_without_capability_node(self):
        with isolated_repo() as root:
            write_taxonomy(root)
            write_file(root, "backend/dotnet/BuildingBlocks/src/Caching/README.md", "# Caching\n")

            diagnostics = knowledge.detect_drift_from_paths(
                ["backend/dotnet/BuildingBlocks/src/Caching/README.md"]
            )

            self.assertEqual("WARN", diagnostics[0].level)
            self.assertEqual("knowledge.missing-capability", diagnostics[0].code)

    def test_detect_drift_reports_proto_without_contract_node(self):
        with isolated_repo() as root:
            write_taxonomy(root)
            write_file(root, "contracts/protos/user/user.proto", 'syntax = "proto3";\n')

            diagnostics = knowledge.detect_drift_from_paths(["contracts/protos/user/user.proto"])

            self.assertEqual("ERROR", diagnostics[0].level)
            self.assertEqual("knowledge.contract-drift", diagnostics[0].code)

    def test_detect_drift_ignores_existing_module_path(self):
        with isolated_repo() as root:
            write_taxonomy(root)
            write_file(root, "backend/dotnet/Services/Authentication/README.md", "# 璁よ瘉鏈嶅姟\n")
            write_module(root)

            diagnostics = knowledge.detect_drift_from_paths(
                ["backend/dotnet/Services/Authentication/README.md"]
            )

            self.assertEqual([], diagnostics)


if __name__ == "__main__":
    unittest.main()
