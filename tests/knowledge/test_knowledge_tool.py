import argparse
import contextlib
import io
import json
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
        valid_stacks:
          - dotnet
          - java
          - python
          - vue-ts
          - uniapp
          - go
        valid_domains:
          - security
          - integration
          - platform
          - frontend
          - contracts
          - operations
        valid_module_types:
          - microservice
          - building-block
          - framework-package
          - frontend-app
          - frontend-package
        path_rules:
          - pattern: backend/dotnet/Services/*/**
            kind: module
            module_type: microservice
            stack: dotnet
          - pattern: backend/dotnet/BuildingBlocks/src/*/**
            kind: module
            module_type: building-block
            stack: dotnet
          - pattern: backend/java/services/*/**
            kind: module
            module_type: microservice
            stack: java
          - pattern: backend/java/packages/*/**
            kind: module
            module_type: framework-package
            stack: java
          - pattern: backend/python/services/*/**
            kind: module
            module_type: microservice
            stack: python
          - pattern: backend/python/packages/*/**
            kind: module
            module_type: framework-package
            stack: python
          - pattern: backend/go/services/*/**
            kind: module
            module_type: microservice
            stack: go
          - pattern: backend/go/packages/*/**
            kind: module
            module_type: framework-package
            stack: go
          - pattern: frontend/apps/*/**
            kind: module
            module_type: frontend-app
            stack: vue-ts
          - pattern: frontend/packages/*/**
            kind: module
            module_type: frontend-package
            stack: vue-ts
          - pattern: contracts/protos/**/*.proto
            kind: contract
            contract_type: grpc
          - pattern: contracts/openapi/**/*.yaml
            kind: contract
            contract_type: openapi
        diagnostics:
          knowledge.missing-module:
            severity: error
            message_zh: 新增服务或模块目录尚未声明 module 图谱节点。
            suggestion_zh: 新增对应 module 图谱节点，或在 taxonomy.yaml 中声明忽略规则。
          knowledge.missing-capability:
            severity: warning
            message_zh: 新增公共构件目录可能暴露可复用能力，但尚未声明 capability 图谱节点。
            suggestion_zh: 为该公共构件声明 capability，或说明它不对外提供复用能力。
          knowledge.contract-drift:
            severity: error
            message_zh: 公共契约发生变更，但对应 contract 图谱节点未更新。
            suggestion_zh: 更新 contract 节点的版本、兼容性说明或变更证据。
          knowledge.contract-outdated:
            severity: warning
            message_zh: 契约文件发生变更，对应 contract 图谱节点可能未同步更新。
            suggestion_zh: 检查 contract 节点版本、兼容性说明和变更证据是否已更新。
        query_aliases:
          当前用户:
            - auth
            - authentication
            - user
          授权:
            - auth
            - authorization
            - permission
          远程调用:
            - remote-call
            - service-integration
          Spring Boot:
            - java
            - spring
            - packages
          前端:
            - vue
            - frontend
            - apps
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


def write_capability(root: Path) -> None:
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
        provided_by:
          modules:
            - backend.dotnet.services.authentication
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


def write_notice_module(root: Path) -> None:
    write_file(
        root,
        "docs/knowledge/graph/modules/backend.dotnet.services.notice.yaml",
        """
        schema_version: 1.0.0
        id: backend.dotnet.services.notice
        kind: module
        name: 通知服务
        status: draft
        summary: 承载通知服务相关代码与集成边界。
        owners:
          - platform
        tags:
          - backend
          - notice
        module_type: microservice
        stack: dotnet
        path: backend/dotnet/Services/Notice
        source:
          declared_in: docs/knowledge/graph/modules/backend.dotnet.services.notice.yaml
          evidence:
            - backend/dotnet/Services/Notice/README.md
        provenance:
          created_by: human
          created_at: 2026-04-27
          updated_by: human
          updated_at: 2026-04-27
        """,
    )


def write_remote_service_capability(root: Path) -> None:
    write_file(
        root,
        "docs/knowledge/graph/capabilities/backend.capability.remote-service.yaml",
        """
        schema_version: 1.0.0
        id: backend.capability.remote-service
        kind: capability
        name: 远程服务调用能力
        status: active
        summary: 提供服务间远程调用能力。
        owners:
          - platform
        tags:
          - backend
          - remote-call
        source:
          declared_in: docs/knowledge/graph/capabilities/backend.capability.remote-service.yaml
          evidence:
            - backend/dotnet/Services/Notice/README.md
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
    def test_taxonomy_contains_plan_a_cross_stack_rules(self):
        with isolated_repo() as root:
            write_taxonomy(root)
            taxonomy = knowledge.load_taxonomy()

            patterns = {rule["pattern"] for rule in taxonomy["path_rules"]}
            self.assertIn("backend/java/services/*/**", patterns)
            self.assertIn("backend/java/packages/*/**", patterns)
            self.assertIn("backend/python/services/*/**", patterns)
            self.assertIn("backend/python/packages/*/**", patterns)
            self.assertIn("backend/go/services/*/**", patterns)
            self.assertIn("backend/go/packages/*/**", patterns)
            self.assertIn("frontend/apps/*/**", patterns)
            self.assertIn("contracts/openapi/**/*.yaml", patterns)
            self.assertIn("go", taxonomy["valid_stacks"])
            self.assertIn("framework-package", taxonomy["valid_module_types"])
            self.assertIn("frontend-app", taxonomy["valid_module_types"])
            self.assertIn("knowledge.contract-outdated", taxonomy["diagnostics"])

    def test_production_taxonomy_contains_plan_a_cross_stack_rules(self):
        taxonomy_path = Path(__file__).resolve().parents[2] / "docs" / "knowledge" / "taxonomy.yaml"
        taxonomy = knowledge.load_yaml_file(taxonomy_path)

        patterns = {rule["pattern"] for rule in taxonomy["path_rules"]}
        self.assertIn("backend/java/services/*/**", patterns)
        self.assertIn("backend/java/packages/*/**", patterns)
        self.assertIn("backend/python/services/*/**", patterns)
        self.assertIn("backend/python/packages/*/**", patterns)
        self.assertIn("backend/go/services/*/**", patterns)
        self.assertIn("backend/go/packages/*/**", patterns)
        self.assertIn("frontend/apps/*/**", patterns)
        self.assertIn("contracts/openapi/**/*.yaml", patterns)
        self.assertIn("go", taxonomy["valid_stacks"])
        self.assertIn("valid_module_types", taxonomy)
        self.assertIn("framework-package", taxonomy["valid_module_types"])
        self.assertIn("frontend-app", taxonomy["valid_module_types"])
        self.assertIn("knowledge.contract-outdated", taxonomy["diagnostics"])
        self.assertEqual(["java", "spring", "packages"], taxonomy["query_aliases"]["Spring Boot"])
        self.assertEqual(["vue", "frontend", "apps"], taxonomy["query_aliases"]["前端"])

    def test_validation_rejects_invalid_module_type(self):
        with isolated_repo() as root:
            write_taxonomy(root)
            write_file(root, "backend/java/services/user/README.md", "# 用户服务\n")
            write_file(
                root,
                "docs/knowledge/graph/modules/backend.java.services.user.yaml",
                """
                schema_version: 1.0.0
                id: backend.java.services.user
                kind: module
                name: 用户服务
                status: draft
                summary: 承载 Java 用户服务代码。
                owners:
                  - platform
                tags:
                  - backend
                  - java
                module_type: bad-type
                stack: java
                path: backend/java/services/user
                source:
                  declared_in: docs/knowledge/graph/modules/backend.java.services.user.yaml
                  evidence:
                    - backend/java/services/user/README.md
                provenance:
                  created_by: human
                  created_at: 2026-04-28
                  updated_by: human
                  updated_at: 2026-04-28
                """,
            )

            messages = knowledge.collect_validation_messages()

            self.assertIn("knowledge.invalid-module-type", [message.code for message in messages])
            self.assertIn("bad-type", diagnostic_text(messages))

    def test_framework_package_drift_is_warning(self):
        with isolated_repo() as root:
            write_taxonomy(root)
            write_file(root, "backend/java/packages/common/README.md", "# common\n")

            diagnostics = knowledge.detect_drift_from_paths(["backend/java/packages/common/README.md"])

            self.assertEqual("WARN", diagnostics[0].level)
            self.assertEqual("knowledge.missing-capability", diagnostics[0].code)
            self.assertEqual("backend/java/packages/common", diagnostics[0].location)

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
            self.assertIn("docs/knowledge/generated/diagnostics.generated.json", payloads)
            self.assertIn("docs/knowledge/generated/_index/by-kind/module.generated.json", payloads)
            self.assertIn("docs/knowledge/generated/_index/by-kind/capability.generated.json", payloads)
            self.assertIn("docs/knowledge/generated/_index/by-tag/auth.generated.json", payloads)
            self.assertIn("docs/knowledge/generated/_index/sections/backend.capability.authentication.generated.json", payloads)
            self.assertEqual([], payloads["docs/knowledge/generated/diagnostics.generated.json"]["diagnostics"])

            for path in [
                "docs/knowledge/generated/index.generated.json",
                "docs/knowledge/generated/memory.generated.json",
                "docs/knowledge/generated/edges.generated.json",
                "docs/knowledge/generated/diagnostics.generated.json",
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

    def test_build_edges_includes_decision_applied_capabilities(self):
        with isolated_repo() as root:
            write_taxonomy(root)
            write_file(root, "backend/dotnet/Services/Authentication/README.md", "# 认证服务\n")
            write_module(root)
            write_capability(root)
            write_file(
                root,
                "docs/knowledge/graph/decisions/backend.decision.authentication-ownership.yaml",
                """
                schema_version: 1.0.0
                id: backend.decision.authentication-ownership
                kind: decision
                name: 认证能力归属决策
                status: active
                summary: 用户身份认证能力由 Authentication 服务统一提供。
                owners:
                  - platform
                tags:
                  - backend
                  - auth
                  - decision
                applies_to:
                  capabilities:
                    - backend.capability.authentication
                decision: 用户身份认证能力由 Authentication 服务统一提供。
                consequences:
                  - 业务服务不得自行签发用户访问令牌。
                source:
                  declared_in: docs/knowledge/graph/decisions/backend.decision.authentication-ownership.yaml
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

            edges = payloads["docs/knowledge/generated/edges.generated.json"]["edges"]
            self.assertIn(
                {
                    "from": "backend.decision.authentication-ownership",
                    "type": "applies_to",
                    "to": "backend.capability.authentication",
                },
                edges,
            )

    def test_build_edges_includes_integration_relationships(self):
        with isolated_repo() as root:
            write_taxonomy(root)
            write_file(root, "backend/dotnet/Services/Authentication/README.md", "# 认证服务\n")
            write_file(root, "backend/dotnet/Services/Notice/README.md", "# 通知服务\n")
            write_module(root)
            write_notice_module(root)
            write_remote_service_capability(root)
            write_file(
                root,
                "docs/knowledge/graph/integrations/backend.integration.notice-authentication.yaml",
                """
                schema_version: 1.0.0
                id: backend.integration.notice-authentication
                kind: integration
                name: 通知服务到认证服务集成
                status: draft
                summary: 记录通知服务需要认证主体或用户身份信息时应走契约和统一远程调用能力。
                owners:
                  - platform
                tags:
                  - backend
                  - auth
                  - notice
                  - service-integration
                caller: backend.dotnet.services.notice
                callee: backend.dotnet.services.authentication
                tooling:
                  required_capabilities:
                    - backend.capability.remote-service
                standards:
                  - rules.auth-oauth-oidc#rules
                source:
                  declared_in: docs/knowledge/graph/integrations/backend.integration.notice-authentication.yaml
                  evidence:
                    - backend/dotnet/Services/Notice/README.md
                provenance:
                  created_by: human
                  created_at: 2026-04-27
                  updated_by: human
                  updated_at: 2026-04-27
                """,
            )

            payloads, messages = knowledge.build_indexes(existing_generated_at="2026-04-27T00:00:00Z")
            self.assertEqual([], [message for message in messages if message.level == "ERROR"], diagnostic_text(messages))

            edges = payloads["docs/knowledge/generated/edges.generated.json"]["edges"]
            self.assertIn(
                {
                    "from": "backend.integration.notice-authentication",
                    "type": "caller",
                    "to": "backend.dotnet.services.notice",
                },
                edges,
            )
            self.assertIn(
                {
                    "from": "backend.integration.notice-authentication",
                    "type": "callee",
                    "to": "backend.dotnet.services.authentication",
                },
                edges,
            )
            self.assertIn(
                {
                    "from": "backend.integration.notice-authentication",
                    "type": "requires",
                    "to": "backend.capability.remote-service",
                },
                edges,
            )
            self.assertIn(
                {
                    "from": "backend.integration.notice-authentication",
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

    def test_check_detects_missing_diagnostics_generated_file(self):
        with isolated_repo() as root:
            write_taxonomy(root)
            write_file(root, "backend/dotnet/Services/Authentication/README.md", "# 认证服务\n")
            write_module(root)

            payloads, messages = knowledge.build_indexes(existing_generated_at="2026-04-27T00:00:00Z")
            self.assertEqual([], [message for message in messages if message.level == "ERROR"], diagnostic_text(messages))
            knowledge.write_indexes(payloads)
            (root / "docs/knowledge/generated/diagnostics.generated.json").unlink()

            messages = knowledge.collect_index_messages()

            self.assertIn("knowledge.index-missing", [message.code for message in messages])
            self.assertIn("diagnostics.generated.json", diagnostic_text(messages))

    def test_command_generate_writes_empty_generated_indexes(self):
        with isolated_repo() as root:
            write_taxonomy(root)

            exit_code = knowledge.command_generate(argparse.Namespace())

            self.assertEqual(0, exit_code)
            self.assertTrue((root / "docs/knowledge/generated/index.generated.json").exists())
            self.assertTrue((root / "docs/knowledge/generated/memory.generated.json").exists())
            self.assertTrue((root / "docs/knowledge/generated/edges.generated.json").exists())
            self.assertTrue((root / "docs/knowledge/generated/diagnostics.generated.json").exists())

    def test_command_generate_preserves_existing_generated_at(self):
        with isolated_repo() as root:
            write_taxonomy(root)
            generated_dir = root / "docs/knowledge/generated"
            generated_dir.mkdir(parents=True)
            (generated_dir / "index.generated.json").write_text(
                '{"generated_at": "2026-04-27T12:00:00Z"}\n',
                encoding="utf-8",
            )

            exit_code = knowledge.command_generate(argparse.Namespace())

            self.assertEqual(0, exit_code)
            payload = json.loads((generated_dir / "index.generated.json").read_text(encoding="utf-8"))
            self.assertEqual("2026-04-27T12:00:00Z", payload["generated_at"])

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

    def test_command_query_reports_validation_diagnostics(self):
        with isolated_repo() as root:
            write_taxonomy(root)
            write_file(
                root,
                "docs/knowledge/graph/modules/broken.yaml",
                """
                id: backend.dotnet.services.broken
                  invalid: value
                """,
            )
            stdout = io.StringIO()

            with contextlib.redirect_stdout(stdout):
                exit_code = knowledge.command_query(argparse.Namespace(text="auth", limit=5))

            self.assertEqual(1, exit_code)
            self.assertIn("knowledge.yaml-parse", stdout.getvalue())

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

    def test_detect_drift_reports_frontend_package_from_path_rule(self):
        with isolated_repo() as root:
            write_taxonomy(root)
            write_file(root, "frontend/packages/orders/src/index.ts", "export {}\n")

            diagnostics = knowledge.detect_drift_from_paths(["frontend/packages/orders/src/index.ts"])

            self.assertEqual("ERROR", diagnostics[0].level)
            self.assertEqual("knowledge.missing-module", diagnostics[0].code)
            self.assertEqual("frontend/packages/orders", diagnostics[0].location)

    def test_detect_drift_ignores_existing_module_path(self):
        with isolated_repo() as root:
            write_taxonomy(root)
            write_file(root, "backend/dotnet/Services/Authentication/README.md", "# 璁よ瘉鏈嶅姟\n")
            write_module(root)

            diagnostics = knowledge.detect_drift_from_paths(
                ["backend/dotnet/Services/Authentication/README.md"]
            )

            self.assertEqual([], diagnostics)

    def test_detect_drift_reports_malformed_taxonomy_instead_of_raising(self):
        with isolated_repo() as root:
            write_file(
                root,
                "docs/knowledge/taxonomy.yaml",
                """
                schema_version: 1.0.0
                  invalid: value
                """,
            )

            diagnostics = knowledge.detect_drift_from_paths(["frontend/packages/orders/src/index.ts"])

            self.assertEqual(1, len(diagnostics), diagnostic_text(diagnostics))
            self.assertEqual("ERROR", diagnostics[0].level)
            self.assertEqual("knowledge.taxonomy-parse", diagnostics[0].code)
            self.assertIn("无法解析知识分类配置", diagnostics[0].message_zh)

    def test_validation_reports_dangling_graph_references(self):
        with isolated_repo() as root:
            write_taxonomy(root)
            write_file(root, "backend/dotnet/Services/Authentication/README.md", "# 认证服务\n")
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
                provided_by:
                  modules:
                    - backend.dotnet.services.missing
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

            messages = knowledge.collect_validation_messages()

            dangling = [message for message in messages if message.code == "knowledge.dangling-reference"]
            self.assertEqual(1, len(dangling), diagnostic_text(messages))
            self.assertEqual("ERROR", dangling[0].level)
            self.assertIn("backend.dotnet.services.missing", dangling[0].message_zh)

    def test_claude_skill_links_use_relative_targets(self):
        with isolated_repo() as root:
            expected = knowledge.claude_skill_link_plan(root)

            self.assertEqual(
                "../../.agents/skills/tw-requirement-router",
                expected["tw-requirement-router"],
            )
            self.assertEqual(
                "../../.agents/skills/tw-knowledge-maintenance",
                expected["tw-knowledge-maintenance"],
            )

    def test_sync_claude_skills_reports_missing_source_skill(self):
        with isolated_repo():
            messages = knowledge.sync_claude_skills()

            self.assertEqual("knowledge.skill-missing", messages[0].code)
            self.assertEqual("仓库 Skill 不存在: tw-requirement-router。", messages[0].message_zh)

    def test_sync_claude_skills_reports_conflicting_destination(self):
        with isolated_repo() as root:
            source = root / ".agents" / "skills" / "tw-requirement-router"
            source.mkdir(parents=True)
            destination = root / ".claude" / "skills" / "tw-requirement-router"
            destination.parent.mkdir(parents=True)
            destination.write_text("not a symlink\n", encoding="utf-8")

            messages = knowledge.sync_claude_skills()

            conflicts = [message for message in messages if message.code == "knowledge.skill-link-conflict"]
            self.assertEqual(1, len(conflicts), diagnostic_text(messages))
            self.assertEqual(
                "Claude Skill 目标已存在且不是预期相对符号链接。",
                conflicts[0].message_zh,
            )

    def test_command_sync_skills_rejects_unsupported_target(self):
        stdout = io.StringIO()
        with contextlib.redirect_stdout(stdout):
            exit_code = knowledge.command_sync_skills(argparse.Namespace(target="other"))

        self.assertEqual(1, exit_code)
        self.assertIn("错误 [knowledge.unsupported-target]", stdout.getvalue())

    def test_command_sync_skills_prints_ok_for_claude_target(self):
        original_sync_claude_skills = knowledge.sync_claude_skills
        knowledge.sync_claude_skills = lambda: []
        stdout = io.StringIO()
        try:
            with contextlib.redirect_stdout(stdout):
                exit_code = knowledge.command_sync_skills(argparse.Namespace(target="claude"))
        finally:
            knowledge.sync_claude_skills = original_sync_claude_skills

        self.assertEqual(0, exit_code)
        self.assertIn("OK Claude Code Skill 相对符号链接已同步", stdout.getvalue())


    def test_changed_current_paths_from_name_status_ignores_deleted_paths(self):
        paths = knowledge.changed_current_paths_from_name_status(
            "D\tbackend/dotnet/Services/User/README.md\n"
        )

        self.assertEqual([], paths)

    def test_changed_current_paths_from_name_status_uses_rename_new_path(self):
        paths = knowledge.changed_current_paths_from_name_status(
            "R100\told/path.proto\tcontracts/protos/new/path.proto\n"
        )

        self.assertEqual(["contracts/protos/new/path.proto"], paths)

    def test_changed_files_from_git_rejects_option_like_base_ref(self):
        with self.assertRaisesRegex(RuntimeError, "must not start with '-'"):
            knowledge.changed_files_from_git("-bad", "HEAD")

    def test_command_check_drift_prints_ok_for_no_diagnostics(self):
        original_changed_files_from_git = knowledge.changed_files_from_git
        original_detect_drift_from_paths = knowledge.detect_drift_from_paths
        knowledge.changed_files_from_git = lambda _base, _head: []
        knowledge.detect_drift_from_paths = lambda _paths: []
        stdout = io.StringIO()
        try:
            with contextlib.redirect_stdout(stdout):
                exit_code = knowledge.command_check_drift(argparse.Namespace(base="HEAD", head="HEAD"))
        finally:
            knowledge.changed_files_from_git = original_changed_files_from_git
            knowledge.detect_drift_from_paths = original_detect_drift_from_paths

        self.assertEqual(0, exit_code)
        self.assertIn("OK knowledge drift check passed", stdout.getvalue())

    def test_command_check_drift_prints_ok_with_warnings(self):
        original_changed_files_from_git = knowledge.changed_files_from_git
        original_detect_drift_from_paths = knowledge.detect_drift_from_paths
        knowledge.changed_files_from_git = lambda _base, _head: [
            "backend/dotnet/BuildingBlocks/src/Caching/README.md"
        ]
        knowledge.detect_drift_from_paths = lambda _paths: [
            knowledge.warn("knowledge.missing-capability", "backend/dotnet/BuildingBlocks/src/Caching", "warning")
        ]
        stdout = io.StringIO()
        try:
            with contextlib.redirect_stdout(stdout):
                exit_code = knowledge.command_check_drift(argparse.Namespace(base="HEAD", head="HEAD"))
        finally:
            knowledge.changed_files_from_git = original_changed_files_from_git
            knowledge.detect_drift_from_paths = original_detect_drift_from_paths

        self.assertEqual(0, exit_code)
        self.assertIn("OK knowledge drift check passed with warnings", stdout.getvalue())

    def test_command_check_drift_returns_one_for_errors(self):
        original_changed_files_from_git = knowledge.changed_files_from_git
        original_detect_drift_from_paths = knowledge.detect_drift_from_paths
        knowledge.changed_files_from_git = lambda _base, _head: ["contracts/protos/user/user.proto"]
        knowledge.detect_drift_from_paths = lambda _paths: [
            knowledge.error("knowledge.contract-drift", "contracts/protos/user/user.proto", "error")
        ]
        stdout = io.StringIO()
        try:
            with contextlib.redirect_stdout(stdout):
                exit_code = knowledge.command_check_drift(argparse.Namespace(base="HEAD", head="HEAD"))
        finally:
            knowledge.changed_files_from_git = original_changed_files_from_git
            knowledge.detect_drift_from_paths = original_detect_drift_from_paths

        self.assertEqual(1, exit_code)
        self.assertNotIn("OK knowledge drift check passed", stdout.getvalue())

    def test_command_check_drift_prints_git_failure(self):
        original_changed_files_from_git = knowledge.changed_files_from_git
        knowledge.changed_files_from_git = lambda _base, _head: (_ for _ in ()).throw(RuntimeError("bad ref"))
        stdout = io.StringIO()
        try:
            with contextlib.redirect_stdout(stdout):
                exit_code = knowledge.command_check_drift(argparse.Namespace(base="-bad", head="HEAD"))
        finally:
            knowledge.changed_files_from_git = original_changed_files_from_git

        self.assertEqual(1, exit_code)
        self.assertIn("错误 [knowledge.git-diff]", stdout.getvalue())
        self.assertIn("bad ref", stdout.getvalue())


if __name__ == "__main__":
    unittest.main()
