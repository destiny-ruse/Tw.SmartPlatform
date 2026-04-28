#!/usr/bin/env python3
from __future__ import annotations

import argparse
import copy
import fnmatch
import json
import os
import re
import subprocess
import sys
from dataclasses import dataclass
from datetime import UTC, datetime
from pathlib import Path
from typing import Any


GENERATOR_VERSION = "1.0.0"
REPO_ROOT = Path(__file__).resolve().parents[2]
KNOWLEDGE_DIR = REPO_ROOT / "docs" / "knowledge"
GRAPH_DIR = KNOWLEDGE_DIR / "graph"
GENERATED_DIR = KNOWLEDGE_DIR / "generated"
TAXONOMY_PATH = KNOWLEDGE_DIR / "taxonomy.yaml"
TEMPLATE_DIR = Path(__file__).resolve().parent / "templates"

REQUIRED_FIELDS = [
    "schema_version",
    "id",
    "kind",
    "name",
    "status",
    "summary",
    "owners",
    "tags",
    "source",
    "provenance",
]
KIND_DIRECTORIES = {
    "capability": "capabilities",
    "module": "modules",
    "contract": "contracts",
    "integration": "integrations",
    "decision": "decisions",
}
ID_PATTERN = re.compile(r"^[a-z][a-z0-9-]*(\.[a-z0-9][a-z0-9-]*)+$")
VERSION_PATTERN = re.compile(r"^\d+\.\d+\.\d+$")
TOKEN_PATTERN = re.compile(r"^[a-z][a-z0-9-]*$")
GENERATED_SUFFIX = ".generated"
STATUS_LABELS = {
    "A": "新增",
    "M": "修改",
    "D": "删除",
    "R": "重命名",
    "C": "复制",
}


@dataclass(frozen=True)
class Diagnostic:
    level: str
    code: str
    location: str
    message_zh: str
    suggestion_zh: str = ""

    def format(self) -> str:
        label = "错误" if self.level == "ERROR" else "警告"
        lines = [
            f"{label} [{self.code}]",
            f"位置: {self.location}",
            f"说明: {self.message_zh}",
        ]
        if self.suggestion_zh:
            lines.append(f"建议: {self.suggestion_zh}")
        return "\n".join(lines)

    def to_json(self) -> dict[str, str]:
        return {
            "level": self.level,
            "code": self.code,
            "location": self.location,
            "message_zh": self.message_zh,
            "suggestion_zh": self.suggestion_zh,
        }


@dataclass(frozen=True)
class GraphNode:
    path: Path
    data: dict[str, Any]
    lines: list[str]


class YamlSubsetError(ValueError):
    pass


def rel_path(path: Path) -> str:
    try:
        return path.resolve().relative_to(REPO_ROOT.resolve()).as_posix()
    except ValueError:
        return path.as_posix()


def error(
    code: str,
    path: Path | str,
    message_zh: str,
    suggestion_zh: str = "",
) -> Diagnostic:
    location = rel_path(path) if isinstance(path, Path) else path
    return Diagnostic("ERROR", code, location, message_zh, suggestion_zh)


def warn(
    code: str,
    path: Path | str,
    message_zh: str,
    suggestion_zh: str = "",
) -> Diagnostic:
    location = rel_path(path) if isinstance(path, Path) else path
    return Diagnostic("WARN", code, location, message_zh, suggestion_zh)


def has_errors(messages: list[Diagnostic]) -> bool:
    return any(message.level == "ERROR" for message in messages)


def emit(messages: list[Diagnostic]) -> None:
    for index, message in enumerate(messages):
        if index:
            print()
        print(message.format())


def read_text(path: Path) -> str:
    return path.read_text(encoding="utf-8")


def write_text(path: Path, content: str) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(content, encoding="utf-8", newline="\n")


def graph_path_for_kind_id(kind: str, node_id: str) -> Path:
    directory = KIND_DIRECTORIES.get(kind)
    if not directory:
        raise RuntimeError(f"unsupported graph kind: {kind}")
    return GRAPH_DIR / directory / f"{node_id}.yaml"


def display_name_from_id(node_id: str) -> str:
    return node_id.split(".")[-1].replace("-", " ").title()


def pascal_name_from_token(token: str) -> str:
    return "".join(part.title() for part in token.split("-"))


def default_module_path(node_id: str) -> str:
    parts = node_id.split(".")
    if len(parts) >= 4 and parts[:3] == ["backend", "dotnet", "services"]:
        return f"backend/dotnet/Services/{pascal_name_from_token(parts[3])}"
    if len(parts) >= 4 and parts[:3] in (
        ["backend", "dotnet", "building-blocks"],
        ["backend", "dotnet", "packages"],
    ):
        return f"backend/dotnet/BuildingBlocks/src/Tw.{pascal_name_from_token(parts[3])}"
    if len(parts) >= 4 and parts[0] == "backend" and parts[2] in {"services", "packages"}:
        return "/".join(parts[:3] + [parts[3]])
    if len(parts) >= 3 and parts[0] == "frontend" and parts[1] == "apps":
        return f"frontend/apps/{parts[2].replace('-', '.')}"
    if len(parts) >= 3 and parts[0] == "frontend" and parts[1] == "packages":
        return f"frontend/packages/{parts[2]}"
    return ""


def default_module_type(node_id: str) -> str:
    parts = node_id.split(".")
    if len(parts) >= 3 and parts[0] == "frontend" and parts[1] == "apps":
        return "frontend-app"
    if len(parts) >= 3 and parts[0] == "frontend" and parts[1] == "packages":
        return "frontend-package"
    if len(parts) >= 3 and parts[:3] in (
        ["backend", "dotnet", "building-blocks"],
        ["backend", "dotnet", "packages"],
    ):
        return "building-block"
    if ".services." in node_id:
        return "microservice"
    return "framework-package"


def default_contract_path(node_id: str) -> str:
    parts = node_id.split(".")
    if len(parts) >= 3 and parts[0] == "contracts" and parts[1] == "openapi":
        return f"contracts/openapi/{parts[2]}.yaml"
    if len(parts) >= 3 and parts[0] == "contracts" and parts[1] == "grpc":
        return f"contracts/protos/{parts[2]}.proto"
    return ""


def init_template_values(kind: str, node_id: str) -> dict[str, str]:
    target = graph_path_for_kind_id(kind, node_id)
    today = datetime.now(UTC).date().isoformat()
    module_path = default_module_path(node_id) if kind == "module" else ""
    contract_path = default_contract_path(node_id) if kind == "contract" else ""
    return {
        "id": node_id,
        "kind": kind,
        "name": display_name_from_id(node_id),
        "summary": f"{display_name_from_id(node_id)} 的知识图谱节点。",
        "declared_in": rel_path(target),
        "today": today,
        "path": module_path or contract_path,
        "evidence": module_path or contract_path or rel_path(target),
        "module_type": default_module_type(node_id),
        "stack": node_id.split(".")[1] if node_id.startswith("backend.") else "vue-ts",
        "contract_type": "openapi" if node_id.startswith("contracts.openapi.") else "grpc",
    }


def render_template(template: str, values: dict[str, str]) -> str:
    rendered = template
    for key, value in values.items():
        rendered = rendered.replace(f"{{{{{key}}}}}", value)
    return rendered


def parse_scalar(raw_value: str) -> Any:
    value = raw_value.strip()
    if value == "":
        return None
    if (value.startswith('"') and value.endswith('"')) or (
        value.startswith("'") and value.endswith("'")
    ):
        return value[1:-1]
    if value == "true":
        return True
    if value == "false":
        return False
    if value == "null":
        return None
    if value.startswith("[") and value.endswith("]"):
        inside = value[1:-1].strip()
        if not inside:
            return []
        return [parse_scalar(item.strip()) for item in inside.split(",")]
    return value


def parse_yaml_subset(lines: list[str]) -> dict[str, Any]:
    tokens: list[tuple[int, str]] = []
    for raw_line in lines:
        if not raw_line.strip() or raw_line.lstrip().startswith("#"):
            continue
        if "\t" in raw_line[: len(raw_line) - len(raw_line.lstrip(" \t"))]:
            raise YamlSubsetError("YAML subset only supports spaces for indentation")
        indent = len(raw_line) - len(raw_line.lstrip(" "))
        tokens.append((indent, raw_line.strip()))

    def split_key_value(content: str) -> tuple[str, str]:
        if ":" not in content:
            raise YamlSubsetError(f"Invalid YAML subset line: {content}")
        key, value = content.split(":", 1)
        key = key.strip()
        if not key:
            raise YamlSubsetError(f"Invalid YAML subset key: {content}")
        return key, value.strip()

    def parse_map(index: int, indent: int) -> tuple[dict[str, Any], int]:
        result: dict[str, Any] = {}
        while index < len(tokens):
            current_indent, content = tokens[index]
            if current_indent < indent:
                break
            if current_indent > indent:
                raise YamlSubsetError(f"Unexpected indentation near: {content}")
            if content.startswith("- "):
                break

            key, value = split_key_value(content)
            index += 1
            if value:
                result[key] = parse_scalar(value)
                continue
            if index >= len(tokens) or tokens[index][0] <= current_indent:
                result[key] = {}
                continue
            child_indent, child_content = tokens[index]
            if child_content.startswith("- "):
                result[key], index = parse_list(index, child_indent)
            else:
                result[key], index = parse_map(index, child_indent)
        return result, index

    def parse_list(index: int, indent: int) -> tuple[list[Any], int]:
        result: list[Any] = []
        while index < len(tokens):
            current_indent, content = tokens[index]
            if current_indent < indent:
                break
            if current_indent > indent:
                raise YamlSubsetError(f"Unexpected indentation near: {content}")
            if not content.startswith("- "):
                break

            item_text = content[2:].strip()
            index += 1
            if not item_text:
                if index < len(tokens) and tokens[index][0] > current_indent:
                    child_indent, child_content = tokens[index]
                    if child_content.startswith("- "):
                        item, index = parse_list(index, child_indent)
                    else:
                        item, index = parse_map(index, child_indent)
                    result.append(item)
                else:
                    result.append(None)
                continue

            if ":" in item_text and not item_text.startswith(("'", '"')):
                key, value = split_key_value(item_text)
                item_map = {key: parse_scalar(value) if value else {}}
                if index < len(tokens) and tokens[index][0] > current_indent:
                    nested, index = parse_map(index, tokens[index][0])
                    item_map.update(nested)
                result.append(item_map)
                continue

            result.append(parse_scalar(item_text))
        return result, index

    payload, index = parse_map(0, 0)
    if index != len(tokens):
        raise YamlSubsetError(f"Unexpected YAML subset content near: {tokens[index][1]}")
    return payload


def graph_files() -> list[Path]:
    if not GRAPH_DIR.exists():
        return []
    return sorted(
        path
        for path in GRAPH_DIR.rglob("*.yaml")
        if GENERATED_SUFFIX not in path.name
    )


def load_yaml_file(path: Path) -> dict[str, Any]:
    return parse_yaml_subset(read_text(path).splitlines())


def load_taxonomy() -> dict[str, Any]:
    if not TAXONOMY_PATH.exists():
        return {}
    return load_yaml_file(TAXONOMY_PATH)


def tokenize_query(text: str, taxonomy: dict[str, Any]) -> list[str]:
    query_text = text.strip().lower()
    tokens: list[str] = []
    seen: set[str] = set()

    def add_token(value: Any) -> None:
        token = str(value).strip().lower()
        if token and token not in seen:
            seen.add(token)
            tokens.append(token)

    query_aliases = taxonomy.get("query_aliases")
    if isinstance(query_aliases, dict):
        for alias, values in query_aliases.items():
            alias_text = str(alias).strip().lower()
            if alias_text and alias_text in query_text:
                if isinstance(values, list):
                    for value in values:
                        add_token(value)
                else:
                    add_token(values)

    for token in re.findall(r"[a-z0-9][a-z0-9-]*", query_text):
        add_token(token)
    if query_text:
        add_token(query_text)
    return tokens


def append_search_value(parts: list[str], value: Any) -> None:
    if isinstance(value, list):
        for item in value:
            append_search_value(parts, item)
        return
    if value not in (None, "", [], {}):
        parts.append(str(value).lower())


def node_search_text(node: GraphNode) -> str:
    data = node.data
    parts: list[str] = []
    for key in ["id", "name", "summary", "tags", "aliases"]:
        append_search_value(parts, data.get(key))
    return " ".join(parts)


def query_nodes(text: str, limit: int = 5) -> list[dict[str, Any]]:
    taxonomy = load_taxonomy()
    tokens = tokenize_query(text, taxonomy)
    if not tokens:
        return []

    nodes, _messages = load_graph_nodes()
    ranked: list[tuple[int, str, GraphNode]] = []
    for node in nodes:
        search_text = node_search_text(node)
        score = sum(1 for token in tokens if token in search_text)
        if score:
            ranked.append((score, str(node.data.get("id", "")), node))

    results: list[dict[str, Any]] = []
    for _score, _node_id, node in sorted(ranked, key=lambda item: (-item[0], item[1]))[:limit]:
        data = node.data
        node_id = str(data.get("id", ""))
        results.append(
            {
                "id": node_id,
                "kind": data.get("kind"),
                "name": data.get("name"),
                "summary": data.get("summary"),
                "read": [
                    generated_path("_index", "sections", f"{node_id}.generated.json"),
                    rel_path(node.path),
                ],
            }
        )
    return results


def load_graph_nodes() -> tuple[list[GraphNode], list[Diagnostic]]:
    nodes: list[GraphNode] = []
    messages: list[Diagnostic] = []
    for path in graph_files():
        lines = read_text(path).splitlines()
        try:
            data = parse_yaml_subset(lines)
        except YamlSubsetError as exc:
            messages.append(
                error(
                    "knowledge.yaml-parse",
                    path,
                    f"无法解析知识图谱 YAML: {exc}",
                    "请检查缩进、列表和映射结构。",
                )
            )
            continue
        nodes.append(GraphNode(path, data, lines))
    return nodes, messages


def collect_validation_messages() -> list[Diagnostic]:
    messages: list[Diagnostic] = []
    try:
        taxonomy = load_taxonomy()
    except YamlSubsetError as exc:
        return [
            error(
                "knowledge.taxonomy-parse",
                TAXONOMY_PATH,
                f"无法解析知识分类配置: {exc}",
                "请检查 taxonomy.yaml 的缩进、列表和映射结构。",
            )
        ]

    nodes, load_messages = load_graph_nodes()
    messages.extend(load_messages)
    valid_kinds = set(taxonomy.get("valid_kinds") or [])
    valid_statuses = set(taxonomy.get("valid_statuses") or [])
    valid_module_types = set(taxonomy.get("valid_module_types") or [])
    seen_ids: dict[str, Path] = {}

    for node in nodes:
        data = node.data
        location = node.path
        for field in REQUIRED_FIELDS:
            if field not in data or data[field] in (None, "", [], {}):
                messages.append(
                    error(
                        "knowledge.required-field",
                        location,
                        f"缺少必填字段 {field}",
                        "请补齐知识图谱节点的必填元数据。",
                    )
                )

        schema_version = data.get("schema_version")
        if schema_version and not VERSION_PATTERN.match(str(schema_version)):
            messages.append(
                error(
                    "knowledge.schema-version",
                    location,
                    f"schema_version 必须符合 x.y.z 格式，当前为 {schema_version}",
                    "请使用语义化版本格式，例如 1.0.0。",
                )
            )

        node_id = data.get("id")
        if node_id:
            node_id_text = str(node_id)
            if not ID_PATTERN.match(node_id_text):
                messages.append(
                    error(
                        "knowledge.id-format",
                        location,
                        f"id 格式无效: {node_id_text}",
                        "请使用小写点分层级标识，例如 backend.dotnet.services.authentication。",
                    )
                )
            if node_id_text in seen_ids:
                messages.append(
                    error(
                        "knowledge.duplicate-id",
                        location,
                        f"重复的知识图谱节点 id: {node_id_text}",
                        f"首次声明位置: {rel_path(seen_ids[node_id_text])}",
                    )
                )
            else:
                seen_ids[node_id_text] = node.path

        kind = data.get("kind")
        if kind and valid_kinds and kind not in valid_kinds:
            messages.append(
                error(
                    "knowledge.invalid-kind",
                    location,
                    f"kind 不在 taxonomy.yaml 允许范围内: {kind}",
                    "请使用 taxonomy.yaml valid_kinds 中声明的类型。",
                )
            )

        status = data.get("status")
        if status and valid_statuses and status not in valid_statuses:
            messages.append(
                error(
                    "knowledge.invalid-status",
                    location,
                    f"status 不在 taxonomy.yaml 允许范围内: {status}",
                    "请使用 taxonomy.yaml valid_statuses 中声明的状态。",
                )
            )

        module_type = data.get("module_type")
        if kind == "module" and module_type and valid_module_types and module_type not in valid_module_types:
            messages.append(
                error(
                    "knowledge.invalid-module-type",
                    location,
                    f"module_type 不在 taxonomy.yaml 允许范围内: {module_type}",
                    "请使用 taxonomy.yaml valid_module_types 中声明的模块类型。",
                )
            )

        source = data.get("source")
        if isinstance(source, dict):
            declared_in = source.get("declared_in")
            actual_path = rel_path(node.path)
            if not declared_in:
                messages.append(
                    error(
                        "knowledge.declared-in",
                        location,
                        f"source.declared_in 必须声明为实际路径 {actual_path}",
                        "请补充 source.declared_in，使其与文件路径保持一致。",
                    )
                )
            elif declared_in != actual_path:
                messages.append(
                    error(
                        "knowledge.declared-in",
                        location,
                        f"source.declared_in 必须等于实际路径 {actual_path}",
                        "请更新 source.declared_in，使其与文件路径保持一致。",
                    )
                )
            evidence = source.get("evidence")
            if not evidence:
                messages.append(
                    error(
                        "knowledge.required-field",
                        location,
                        "缺少必填字段 source.evidence",
                        "请补齐知识图谱节点的必填元数据。",
                    )
                )
            elif not isinstance(evidence, list):
                messages.append(
                    error(
                        "knowledge.source-format",
                        location,
                        "source.evidence 必须是证据路径列表",
                        "请按模板修正 source.evidence 字段结构。",
                    )
                )
                evidence = []
            if isinstance(evidence, list):
                for item in evidence:
                    evidence_path = REPO_ROOT / str(item)
                    if not evidence_path.exists():
                        messages.append(
                            warn(
                                "knowledge.missing-evidence",
                                location,
                                f"证据文件不存在: {item}",
                                "请补充证据文件，或从 source.evidence 中移除无效路径。",
                            )
                        )
        elif "source" in data:
            messages.append(
                error(
                    "knowledge.source-format",
                    location,
                    "source 必须是包含 declared_in 和 evidence 的映射",
                    "请按模板修正 source 字段结构。",
                )
            )

        provenance = data.get("provenance")
        if isinstance(provenance, dict):
            for field in ["created_by", "created_at", "updated_by", "updated_at"]:
                field_path = f"provenance.{field}"
                if field not in provenance or provenance[field] in (None, "", [], {}):
                    messages.append(
                        error(
                            "knowledge.required-field",
                            location,
                            f"缺少必填字段 {field_path}",
                            "请补齐知识图谱节点的必填元数据。",
                        )
                    )
        elif "provenance" in data:
            messages.append(
                error(
                    "knowledge.provenance-format",
                    location,
                    "provenance 必须是包含创建和更新信息的映射",
                    "请按模板修正 provenance 字段结构。",
                )
            )

    messages.extend(validate_graph_references(nodes))
    return messages


def append_dangling_reference(
    messages: list[Diagnostic],
    node: GraphNode,
    field_path: str,
    expected_kind: str,
    referenced_id: Any,
) -> None:
    messages.append(
        error(
            "knowledge.dangling-reference",
            node.path,
            f"{field_path} 引用未声明的 {expected_kind} 图谱节点: {referenced_id}",
            f"请新增对应 {expected_kind} 节点，或从 {field_path} 中移除无效引用。",
        )
    )


def validate_graph_references(nodes: list[GraphNode]) -> list[Diagnostic]:
    messages: list[Diagnostic] = []
    ids_by_kind: dict[str, set[str]] = {}
    for node in nodes:
        node_id = node.data.get("id")
        kind = node.data.get("kind")
        if node_id and kind:
            ids_by_kind.setdefault(str(kind), set()).add(str(node_id))

    module_ids = ids_by_kind.get("module", set())
    capability_ids = ids_by_kind.get("capability", set())
    contract_ids = ids_by_kind.get("contract", set())

    def require_many(
        node: GraphNode,
        field_path: str,
        expected_kind: str,
        valid_ids: set[str],
        values: Any,
    ) -> None:
        if not isinstance(values, list):
            return
        for value in values:
            if value and str(value) not in valid_ids:
                append_dangling_reference(messages, node, field_path, expected_kind, value)

    def require_one(
        node: GraphNode,
        field_path: str,
        expected_kind: str,
        valid_ids: set[str],
        value: Any,
    ) -> None:
        if value and str(value) not in valid_ids:
            append_dangling_reference(messages, node, field_path, expected_kind, value)

    for node in nodes:
        data = node.data
        kind = data.get("kind")

        if kind == "capability":
            provided_by = data.get("provided_by")
            if isinstance(provided_by, dict):
                require_many(
                    node,
                    "provided_by.modules",
                    "module",
                    module_ids,
                    provided_by.get("modules"),
                )

        if kind == "module":
            provides = data.get("provides")
            if isinstance(provides, dict):
                require_many(
                    node,
                    "provides.capabilities",
                    "capability",
                    capability_ids,
                    provides.get("capabilities"),
                )

        if kind == "decision":
            applies_to = data.get("applies_to")
            if isinstance(applies_to, dict):
                require_many(
                    node,
                    "applies_to.capabilities",
                    "capability",
                    capability_ids,
                    applies_to.get("capabilities"),
                )

        if kind == "integration":
            require_one(node, "caller", "module", module_ids, data.get("caller"))
            require_one(node, "callee", "module", module_ids, data.get("callee"))
            require_one(node, "contract", "contract", contract_ids, data.get("contract"))
            tooling = data.get("tooling")
            if isinstance(tooling, dict):
                require_many(
                    node,
                    "tooling.required_capabilities",
                    "capability",
                    capability_ids,
                    tooling.get("required_capabilities"),
                )

    return messages


def utc_now() -> str:
    return datetime.now(UTC).replace(microsecond=0).isoformat().replace("+00:00", "Z")


def generated_path(*parts: str) -> str:
    return (GENERATED_DIR.joinpath(*parts)).resolve().relative_to(REPO_ROOT.resolve()).as_posix()


def generator_metadata(generated_at: str) -> dict[str, Any]:
    return {
        "schema_version": "1.0.0",
        "generator": {
            "name": "tools/knowledge/knowledge.py",
            "version": GENERATOR_VERSION,
        },
        "generated_at": generated_at,
    }


def section_index(node: GraphNode, generated_at: str) -> dict[str, Any]:
    sections: list[dict[str, Any]] = []
    current: dict[str, Any] | None = None
    for line_number, raw_line in enumerate(node.lines, start=1):
        if not raw_line.strip() or raw_line.lstrip().startswith("#"):
            continue
        indent = len(raw_line) - len(raw_line.lstrip(" "))
        if indent != 0 or ":" not in raw_line:
            continue
        if current:
            current["end_line"] = line_number - 1
            sections.append(current)
        key = raw_line.split(":", 1)[0].strip()
        current = {
            "key": key,
            "start_line": line_number,
            "end_line": line_number,
        }
    if current:
        current["end_line"] = len(node.lines)
        sections.append(current)

    return {
        **generator_metadata(generated_at),
        "id": node.data.get("id"),
        "path": rel_path(node.path),
        "sections": sections,
    }


def shard_paths(data: dict[str, Any]) -> list[str]:
    paths: list[str] = []
    for key, directory in [
        ("kind", "by-kind"),
        ("domain", "by-domain"),
        ("stack", "by-stack"),
    ]:
        value = data.get(key)
        if value:
            paths.append(generated_path("_index", directory, f"{value}.generated.json"))
    for key, directory in [
        ("tags", "by-tag"),
        ("owners", "by-owner"),
    ]:
        values = data.get(key)
        if isinstance(values, list):
            for value in values:
                if value:
                    paths.append(generated_path("_index", directory, f"{value}.generated.json"))
    return sorted(set(paths))


def light_node(node: GraphNode) -> dict[str, Any]:
    data = node.data
    result: dict[str, Any] = {}
    for key in [
        "id",
        "kind",
        "name",
        "summary",
        "tags",
    ]:
        if key in data:
            result[key] = copy.deepcopy(data[key])
    node_id = str(data.get("id"))
    result["path"] = rel_path(node.path)
    result["sections_index"] = generated_path("_index", "sections", f"{node_id}.generated.json")
    result["shards"] = shard_paths(data)
    return result


def build_edges(nodes: list[GraphNode]) -> list[dict[str, str]]:
    edges: set[tuple[str, str, str]] = set()
    for node in nodes:
        data = node.data
        node_id = data.get("id")
        if not node_id:
            continue

        if data.get("kind") == "capability":
            standards = data.get("standards")
            if isinstance(standards, list):
                for standard_id in standards:
                    if standard_id:
                        edges.add((str(node_id), "governed_by", str(standard_id)))

            provided_by = data.get("provided_by")
            if isinstance(provided_by, dict) and isinstance(provided_by.get("modules"), list):
                for module_id in provided_by["modules"]:
                    if module_id:
                        edges.add((str(module_id), "provides", str(node_id)))

        if data.get("kind") == "module":
            provides = data.get("provides")
            if isinstance(provides, dict) and isinstance(provides.get("capabilities"), list):
                for capability_id in provides["capabilities"]:
                    if capability_id:
                        edges.add((str(node_id), "provides", str(capability_id)))

        if data.get("kind") == "decision":
            applies_to = data.get("applies_to")
            if isinstance(applies_to, dict) and isinstance(applies_to.get("capabilities"), list):
                for capability_id in applies_to["capabilities"]:
                    if capability_id:
                        edges.add((str(node_id), "applies_to", str(capability_id)))

        if data.get("kind") == "integration":
            caller = data.get("caller")
            if caller:
                edges.add((str(node_id), "caller", str(caller)))

            callee = data.get("callee")
            if callee:
                edges.add((str(node_id), "callee", str(callee)))

            contract = data.get("contract")
            if contract:
                edges.add((str(node_id), "contract", str(contract)))

            tooling = data.get("tooling")
            if isinstance(tooling, dict) and isinstance(tooling.get("required_capabilities"), list):
                for capability_id in tooling["required_capabilities"]:
                    if capability_id:
                        edges.add((str(node_id), "requires", str(capability_id)))

            standards = data.get("standards")
            if isinstance(standards, list):
                for standard_id in standards:
                    if standard_id:
                        edges.add((str(node_id), "governed_by", str(standard_id)))

    return [
        {"from": source, "type": edge_type, "to": target}
        for source, edge_type, target in sorted(edges)
    ]


def add_shard(
    shards: dict[str, list[dict[str, Any]]],
    path: str,
    node: dict[str, Any],
) -> None:
    shards.setdefault(path, []).append(node)


def build_indexes(existing_generated_at: str | None = None) -> tuple[dict[str, Any], list[Diagnostic]]:
    generated_at = existing_generated_at or utc_now()
    messages = collect_validation_messages()
    if has_errors(messages):
        return {}, messages

    nodes, _load_messages = load_graph_nodes()
    sorted_nodes = sorted(nodes, key=lambda item: str(item.data.get("id", "")))
    light_nodes = [light_node(node) for node in sorted_nodes]
    full_nodes = [copy.deepcopy(node.data) for node in sorted_nodes]
    edges = build_edges(sorted_nodes)

    payloads: dict[str, Any] = {
        generated_path("index.generated.json"): {
            **generator_metadata(generated_at),
            "nodes": light_nodes,
        },
        generated_path("memory.generated.json"): {
            **generator_metadata(generated_at),
            "nodes": full_nodes,
        },
        generated_path("edges.generated.json"): {
            **generator_metadata(generated_at),
            "edges": edges,
        },
        generated_path("diagnostics.generated.json"): {
            **generator_metadata(generated_at),
            "diagnostics": [message.to_json() for message in messages],
        },
    }

    shards: dict[str, list[dict[str, Any]]] = {}
    for node in light_nodes:
        for path in node["shards"]:
            add_shard(shards, path, node)

    for path in sorted(shards):
        payloads[path] = {
            **generator_metadata(generated_at),
            "nodes": sorted(shards[path], key=lambda item: str(item.get("id", ""))),
        }

    for node in sorted_nodes:
        node_id = str(node.data.get("id"))
        payloads[generated_path("_index", "sections", f"{node_id}.generated.json")] = section_index(
            node,
            generated_at,
        )

    return payloads, messages


def json_text(payload: Any) -> str:
    return json.dumps(payload, ensure_ascii=False, indent=2, sort_keys=True) + "\n"


def generated_index_files() -> list[Path]:
    if not GENERATED_DIR.exists():
        return []
    return sorted(GENERATED_DIR.rglob("*.generated.json"))


def write_indexes(payloads: dict[str, Any]) -> None:
    expected_paths = {(REPO_ROOT / relative_path).resolve() for relative_path in payloads}
    for existing_path in generated_index_files():
        if existing_path.resolve() not in expected_paths:
            existing_path.unlink()

    for relative_path, payload in sorted(payloads.items()):
        write_text(REPO_ROOT / relative_path, json_text(payload))


def normalize_generated_at(payload: Any, generated_at: str) -> Any:
    if isinstance(payload, dict):
        return {
            key: generated_at if key == "generated_at" else normalize_generated_at(value, generated_at)
            for key, value in payload.items()
        }
    if isinstance(payload, list):
        return [normalize_generated_at(value, generated_at) for value in payload]
    return payload


def existing_generated_at() -> str | None:
    index_path = GENERATED_DIR / "index.generated.json"
    if not index_path.exists():
        return None
    try:
        payload = json.loads(read_text(index_path))
    except json.JSONDecodeError:
        return None
    generated_at = payload.get("generated_at") if isinstance(payload, dict) else None
    if generated_at:
        return str(generated_at)
    return None


def collect_index_messages() -> list[Diagnostic]:
    fixed_generated_at = "2026-04-27T00:00:00Z"
    generated, messages = build_indexes(existing_generated_at=fixed_generated_at)
    if has_errors(messages):
        return messages

    for relative_path, expected_payload in generated.items():
        path = REPO_ROOT / relative_path
        if not path.exists():
            messages.append(
                error(
                    "knowledge.index-missing",
                    path,
                    "生成索引文件不存在。",
                    "请运行 python tools/knowledge/knowledge.py generate。",
                )
            )
            continue

        try:
            existing_payload = json.loads(read_text(path))
        except json.JSONDecodeError:
            messages.append(
                error(
                    "knowledge.index-invalid-json",
                    path,
                    "生成索引不是有效 JSON。",
                    "请运行 python tools/knowledge/knowledge.py generate。",
                )
            )
            continue

        if normalize_generated_at(existing_payload, fixed_generated_at) != expected_payload:
            messages.append(
                error(
                    "knowledge.index-stale",
                    path,
                    "生成索引不是最新。",
                    "请运行 python tools/knowledge/knowledge.py generate。",
                )
            )

    expected_paths = {(REPO_ROOT / relative_path).resolve() for relative_path in generated}
    for existing_path in generated_index_files():
        if existing_path.resolve() not in expected_paths:
            messages.append(
                error(
                    "knowledge.index-obsolete",
                    existing_path,
                    "存在过期生成索引。",
                    "请运行 python tools/knowledge/knowledge.py generate。",
                )
            )

    return messages


def repository_skill_names(root: Path = REPO_ROOT) -> list[str]:
    source_root = root / ".agents" / "skills"
    if not source_root.exists():
        return []
    return sorted(
        path.name
        for path in source_root.iterdir()
        if path.is_dir() and (path / "SKILL.md").exists()
    )


def claude_skill_link_plan(root: Path = REPO_ROOT) -> dict[str, str]:
    return {
        skill: claude_skill_link_target(skill)
        for skill in repository_skill_names(root)
    }


def claude_skill_link_target(skill: str) -> str:
    return os.path.join("..", "..", ".agents", "skills", skill)


def expected_claude_skill_link(destination: Path, target: str) -> bool:
    if not destination.is_symlink():
        return False
    try:
        actual_target = os.readlink(destination)
    except OSError:
        return False
    return actual_target == target and (destination / "SKILL.md").is_file()


def report_claude_skill_link_conflict(destination: Path) -> Diagnostic:
    return error(
        "knowledge.skill-link-conflict",
        destination,
        "Claude Skill 目标已存在且不是预期相对符号链接。",
    )


def sync_claude_skills() -> list[Diagnostic]:
    messages: list[Diagnostic] = []
    source_root = REPO_ROOT / ".agents" / "skills"
    destination_root = REPO_ROOT / ".claude" / "skills"
    plan = claude_skill_link_plan(REPO_ROOT)
    destination_root.mkdir(parents=True, exist_ok=True)

    for skill, target in plan.items():
        source = source_root / skill
        destination = destination_root / skill
        if not source.is_dir():
            messages.append(
                error(
                    "knowledge.skill-missing",
                    source,
                    f"仓库 Skill 不存在: {skill}。",
                )
            )
            continue

        if destination.exists() or destination.is_symlink():
            if expected_claude_skill_link(destination, target):
                continue
            if not destination.is_symlink():
                messages.append(report_claude_skill_link_conflict(destination))
                continue
            # Windows 目录符号链接会保留原始分隔符，错误分隔符会导致链接存在但无法访问
            destination.unlink()

        try:
            destination.symlink_to(target, target_is_directory=True)
        except OSError as exc:
            messages.append(
                error(
                    "knowledge.symlink-failed",
                    destination,
                    f"创建 Claude Skill 相对符号链接失败: {skill}。{exc}",
                    "在支持符号链接的终端重试，或启用系统开发者模式后重试。",
                )
            )

    return messages


def normalized_repo_path(path: str) -> str:
    return Path(path.replace("\\", "/")).as_posix().strip("/")


IGNORED_SCAN_DIRS = {
    ".git",
    ".claude",
    ".codex",
    ".idea",
    ".vs",
    "bin",
    "obj",
    "node_modules",
    "dist",
    "build",
    "__pycache__",
}


def taxonomy_scan_roots(taxonomy: dict[str, Any]) -> list[str]:
    path_rules = taxonomy.get("path_rules")
    if not isinstance(path_rules, list):
        return []
    roots: set[str] = set()
    for rule in path_rules:
        if not isinstance(rule, dict):
            continue
        pattern = str(rule.get("pattern") or "").strip()
        if not pattern:
            continue
        static_parts: list[str] = []
        for part in normalized_repo_path(pattern).split("/"):
            if any(marker in part for marker in "*?["):
                break
            static_parts.append(part)
        root = "/".join(static_parts)
        if root:
            roots.add(root)
    return sorted(roots)


def filesystem_scan_paths() -> list[str]:
    taxonomy = load_taxonomy()

    paths: list[str] = []
    for root_name in taxonomy_scan_roots(taxonomy):
        root = REPO_ROOT / root_name
        if not root.exists():
            continue
        for dirpath, dirnames, filenames in os.walk(root):
            dirnames[:] = [name for name in dirnames if name not in IGNORED_SCAN_DIRS]
            for filename in filenames:
                paths.append(rel_path(Path(dirpath) / filename))
    return sorted(paths)


def first_path_segment_after(prefix: str, path: str) -> str | None:
    prefix_parts = normalized_repo_path(prefix).split("/")
    path_parts = normalized_repo_path(path).split("/")
    if len(path_parts) <= len(prefix_parts):
        return None
    if path_parts[: len(prefix_parts)] != prefix_parts:
        return None
    return path_parts[len(prefix_parts)]


def existing_module_paths(nodes: list[GraphNode]) -> set[str]:
    return {
        normalized_repo_path(str(node.data.get("path")))
        for node in nodes
        if node.data.get("kind") == "module" and node.data.get("path")
    }


def taxonomy_diagnostic(
    taxonomy: dict[str, Any],
    code: str,
    level: str,
    location: str,
    message_zh: str,
    suggestion_zh: str = "",
) -> Diagnostic:
    diagnostics = taxonomy.get("diagnostics")
    if isinstance(diagnostics, dict):
        configured = diagnostics.get(code)
        if isinstance(configured, dict):
            configured_level = str(configured.get("severity") or level).upper()
            if configured_level == "WARNING":
                configured_level = "WARN"
            message_zh = str(configured.get("message_zh") or message_zh)
            suggestion_zh = str(configured.get("suggestion_zh") or suggestion_zh)
            level = configured_level

    if level == "WARN":
        return warn(code, location, message_zh, suggestion_zh)
    return error(code, location, message_zh, suggestion_zh)


def taxonomy_parse_diagnostic(exc: YamlSubsetError) -> Diagnostic:
    return error(
        "knowledge.taxonomy-parse",
        TAXONOMY_PATH,
        f"无法解析知识分类配置: {exc}",
        "请检查 taxonomy.yaml 的缩进、列表和映射结构。",
    )


def path_matches_rule(pattern: str, path: str) -> bool:
    normalized_pattern = normalized_repo_path(pattern)
    normalized_path = normalized_repo_path(path)
    if fnmatch.fnmatchcase(normalized_path, normalized_pattern):
        return True
    if "/**/" in normalized_pattern:
        direct_pattern = normalized_pattern.replace("/**/", "/")
        return fnmatch.fnmatchcase(normalized_path, direct_pattern)
    return False


def path_rule_location(rule: dict[str, Any], changed_path: str) -> str:
    if rule.get("kind") == "contract":
        return changed_path

    pattern_parts = normalized_repo_path(str(rule.get("pattern") or "")).split("/")
    path_parts = normalized_repo_path(changed_path).split("/")
    for index, part in enumerate(pattern_parts):
        if part == "*" and len(path_parts) > index:
            return "/".join(path_parts[: index + 1])
    return changed_path


def path_rule_diagnostic(
    taxonomy: dict[str, Any],
    rule: dict[str, Any],
    location: str,
) -> Diagnostic | None:
    kind = rule.get("kind")
    if kind == "contract":
        return taxonomy_diagnostic(
            taxonomy,
            "knowledge.contract-drift",
            "ERROR",
            location,
            "契约文件变更尚未声明 contract 图谱节点。",
            "新增或更新 path 精确匹配该契约文件的 contract 图谱节点。",
        )

    if kind != "module":
        return None

    if rule.get("module_type") in {"building-block", "framework-package"}:
        return taxonomy_diagnostic(
            taxonomy,
            "knowledge.missing-capability",
            "WARN",
            location,
            "新增公共构件目录可能暴露可复用能力，但尚未声明 capability 图谱节点。",
            "新增对应 module 图谱节点并关联 capability，或说明该公共构件不对外提供复用能力。",
        )

    return taxonomy_diagnostic(
        taxonomy,
        "knowledge.missing-module",
        "ERROR",
        location,
        "新增服务或模块目录尚未声明 module 图谱节点。",
        "新增对应 module 图谱节点，或在 taxonomy.yaml 中声明忽略规则。",
    )


def detect_drift_from_paths(paths: list[str]) -> list[Diagnostic]:
    try:
        taxonomy = load_taxonomy()
    except YamlSubsetError as exc:
        return [taxonomy_parse_diagnostic(exc)]

    nodes, load_messages = load_graph_nodes()
    messages = list(load_messages)
    module_paths = existing_module_paths(nodes)
    contract_paths = {
        normalized_repo_path(str(node.data.get("path")))
        for node in nodes
        if node.data.get("kind") == "contract" and node.data.get("path")
    }
    reported: set[tuple[str, str]] = set()

    def add_once(diagnostic: Diagnostic) -> None:
        key = (diagnostic.code, diagnostic.location)
        if key not in reported:
            reported.add(key)
            messages.append(diagnostic)

    path_rules = taxonomy.get("path_rules")
    if not isinstance(path_rules, list):
        path_rules = []

    for changed_path in sorted(normalized_repo_path(path) for path in paths if path):
        for rule in path_rules:
            if not isinstance(rule, dict):
                continue
            pattern = rule.get("pattern")
            if not pattern or not path_matches_rule(str(pattern), changed_path):
                continue

            location = path_rule_location(rule, changed_path)
            if rule.get("kind") == "contract":
                if changed_path in contract_paths:
                    add_once(
                        taxonomy_diagnostic(
                            taxonomy,
                            "knowledge.contract-outdated",
                            "WARN",
                            changed_path,
                            "契约文件发生变更，对应 contract 图谱节点可能未同步更新。",
                            "检查 contract 节点版本、兼容性说明和变更证据是否已更新。",
                        )
                    )
                    break
            elif rule.get("kind") == "module":
                if location in module_paths:
                    continue
            else:
                continue

            diagnostic = path_rule_diagnostic(taxonomy, rule, location)
            if diagnostic:
                add_once(diagnostic)
            break

    return messages


def changed_current_paths_from_name_status(output: str) -> list[str]:
    paths: list[str] = []
    for raw_line in output.splitlines():
        if not raw_line.strip():
            continue
        parts = raw_line.split("\t")
        status = parts[0]
        if status.startswith("D"):
            continue
        if status.startswith(("R", "C")):
            if len(parts) >= 3:
                paths.append(parts[2])
            continue
        if len(parts) >= 2:
            paths.append(parts[1])
    return paths


def changed_entries_from_name_status(output: str) -> list[tuple[str, str]]:
    entries: list[tuple[str, str]] = []
    for raw_line in output.splitlines():
        if not raw_line.strip():
            continue
        parts = raw_line.split("\t")
        status = parts[0]
        if status.startswith("D") and len(parts) >= 2:
            entries.append((status[0], normalized_repo_path(parts[1])))
            continue
        if status.startswith(("R", "C")) and len(parts) >= 3:
            entries.append((status[0], normalized_repo_path(parts[2])))
            continue
        if len(parts) >= 2:
            entries.append((status[0], normalized_repo_path(parts[1])))
    return entries


def diff_label_for_rule(rule: dict[str, Any]) -> str:
    kind = str(rule.get("kind") or "unknown")
    if kind == "module":
        return f"module [{rule.get('stack')} {rule.get('module_type')}]"
    if kind == "contract":
        return f"contract [{rule.get('contract_type')}]"
    return kind


def diff_groups_from_paths(entries: list[tuple[str, str]]) -> dict[str, list[str]]:
    taxonomy = load_taxonomy()
    path_rules = taxonomy.get("path_rules")
    rules = path_rules if isinstance(path_rules, list) else []
    groups: dict[str, list[str]] = {}
    other_label = "其他文件（不在 taxonomy 规则内）"
    for status, path in entries:
        label = other_label
        normalized_path = normalized_repo_path(path)
        for rule in rules:
            if isinstance(rule, dict) and path_matches_rule(str(rule.get("pattern") or ""), normalized_path):
                label = diff_label_for_rule(rule)
                break
        verb = STATUS_LABELS.get(status[0], "修改")
        groups.setdefault(label, []).append(f"{verb}: {normalized_path}")
    return {key: sorted(value) for key, value in sorted(groups.items())}


def safe_ref_name(value: str) -> str:
    return re.sub(r"[^A-Za-z0-9._-]+", "-", value).strip("-") or "ref"


def save_drift_messages(
    messages: list[Diagnostic],
    base: str,
    head: str,
    today: str | None = None,
) -> Path:
    current_day = today or datetime.now(UTC).date().isoformat()
    year = current_day.split("-", 1)[0]
    target = KNOWLEDGE_DIR / "changes" / year / f"{current_day}-{safe_ref_name(base)}-{safe_ref_name(head)}.json"
    generated_at = utc_now()
    if target.exists():
        try:
            existing_payload = json.loads(read_text(target))
        except (OSError, json.JSONDecodeError):
            existing_payload = {}
        if isinstance(existing_payload, dict) and isinstance(existing_payload.get("generated_at"), str):
            generated_at = existing_payload["generated_at"]
    payload = {
        **generator_metadata(generated_at),
        "from_ref": base,
        "to_ref": head,
        "diagnostics": [message.to_json() for message in messages],
    }
    write_text(target, json_text(payload))
    return target


def reject_option_like_ref(name: str, value: str) -> None:
    if value.startswith("-"):
        raise RuntimeError(f"{name} ref must not start with '-'")


def changed_files_from_git(base: str, head: str) -> list[str]:
    reject_option_like_ref("base", base)
    reject_option_like_ref("head", head)
    result = subprocess.run(
        ["git", "diff", "--name-status", base, head],
        cwd=REPO_ROOT,
        capture_output=True,
        check=False,
        text=True,
    )
    if result.returncode != 0:
        stderr = result.stderr.strip()
        raise RuntimeError(stderr or "git diff failed")
    return changed_current_paths_from_name_status(result.stdout)


def command_check_drift(args: argparse.Namespace) -> int:
    try:
        changed_paths = changed_files_from_git(args.base, args.head)
    except RuntimeError as exc:
        print("错误 [knowledge.git-diff]")
        print(f"说明: {exc}")
        return 1

    messages = detect_drift_from_paths(changed_paths)
    if getattr(args, "save", False):
        saved_path = save_drift_messages(messages, args.base, args.head)
        print(f"Saved {rel_path(saved_path)}")
    emit(messages)
    if has_errors(messages):
        return 1
    if messages:
        print("OK knowledge drift check passed with warnings")
    else:
        print("OK knowledge drift check passed")
    return 0


def command_diff(args: argparse.Namespace) -> int:
    try:
        reject_option_like_ref("base", args.base)
        reject_option_like_ref("head", args.head)
        result = subprocess.run(
            ["git", "diff", "--name-status", args.base, args.head],
            cwd=REPO_ROOT,
            capture_output=True,
            check=False,
            text=True,
        )
        if result.returncode != 0:
            raise RuntimeError(result.stderr.strip() or "git diff failed")
    except RuntimeError as exc:
        print("错误 [knowledge.git-diff]")
        print(f"说明: {exc}")
        return 1

    try:
        groups = diff_groups_from_paths(changed_entries_from_name_status(result.stdout))
    except YamlSubsetError as exc:
        emit([taxonomy_parse_diagnostic(exc)])
        return 1

    print(f"变更概览（{args.base} -> {args.head}）")
    if not groups:
        print("无变更。")
        return 0
    for label, items in groups.items():
        print()
        print(label)
        for item in items:
            print(f"  {item}")
    return 0


def command_scan(_args: argparse.Namespace) -> int:
    try:
        paths = filesystem_scan_paths()
    except YamlSubsetError as exc:
        emit([taxonomy_parse_diagnostic(exc)])
        return 1

    messages = detect_drift_from_paths(paths)
    emit(messages)
    if has_errors(messages):
        return 1
    if messages:
        print("OK knowledge scan passed with warnings")
    else:
        print("OK knowledge scan passed")
    return 0


def command_generate(_args: argparse.Namespace) -> int:
    payloads, messages = build_indexes(existing_generated_at=existing_generated_at())
    emit(messages)
    if has_errors(messages):
        return 1

    write_indexes(payloads)
    print(f"Generated {len(payloads)} knowledge index files")
    return 0


def command_check(_args: argparse.Namespace) -> int:
    messages = collect_index_messages()
    emit(messages)
    if has_errors(messages):
        return 1
    print("OK knowledge checks passed")
    return 0


def command_init(args: argparse.Namespace) -> int:
    kind = args.kind
    node_id = args.node_id
    if kind not in KIND_DIRECTORIES:
        print("错误 [knowledge.unsupported-kind]")
        print(f"说明: 不支持的图谱节点类型: {kind}")
        return 1
    if not ID_PATTERN.match(node_id):
        print("错误 [knowledge.id-format]")
        print(f"说明: id 格式无效: {node_id}")
        return 1

    template_path = TEMPLATE_DIR / f"{kind}.yaml"
    target_path = graph_path_for_kind_id(kind, node_id)
    if target_path.exists():
        print("错误 [knowledge.init-exists]")
        print(f"说明: 目标图谱节点已存在: {rel_path(target_path)}")
        return 1
    if not template_path.exists():
        print("错误 [knowledge.template-missing]")
        print(f"说明: 模板不存在: {rel_path(template_path)}")
        return 1

    content = render_template(read_text(template_path), init_template_values(kind, node_id))
    write_text(target_path, content)
    print(f"Created {rel_path(target_path)}")
    return 0


def command_sync_skills(args: argparse.Namespace) -> int:
    if args.target != "claude":
        print("错误 [knowledge.unsupported-target]")
        print(f"说明: 不支持的 Skill 同步目标: {args.target}")
        return 1

    messages = sync_claude_skills()
    emit(messages)
    if has_errors(messages):
        return 1
    print("OK Claude Code Skill 相对符号链接已同步")
    return 0


def command_query(args: argparse.Namespace) -> int:
    messages = collect_validation_messages()
    errors = [message for message in messages if message.level == "ERROR"]
    if errors:
        emit(errors)
        return 1

    results = query_nodes(args.text, args.limit)
    if not results:
        print("未找到匹配的知识图谱节点。")
        return 1

    for index, result in enumerate(results):
        if index:
            print()
        print(f"{result['id']}")
        print(f"类型: {result.get('kind', '')}")
        print(f"名称: {result.get('name', '')}")
        print(f"摘要: {result.get('summary', '')}")
        print("建议读取:")
        for path in result["read"]:
            print(f"- {path}")
    return 0


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description="Knowledge graph maintenance tool")
    subparsers = parser.add_subparsers(dest="command", required=True)
    generate_parser = subparsers.add_parser("generate", help="generate knowledge indexes")
    generate_parser.set_defaults(func=command_generate)
    check_parser = subparsers.add_parser("check", help="validate knowledge graph nodes")
    check_parser.set_defaults(func=command_check)
    scan_parser = subparsers.add_parser("scan", help="scan filesystem paths for knowledge graph drift")
    scan_parser.set_defaults(func=command_scan)
    init_parser = subparsers.add_parser("init", help="initialize a knowledge graph node from template")
    init_parser.add_argument("--kind", required=True, choices=sorted(KIND_DIRECTORIES))
    init_parser.add_argument("--id", dest="node_id", required=True)
    init_parser.set_defaults(func=command_init)
    drift_parser = subparsers.add_parser("check-drift", help="detect knowledge graph drift from git diff")
    drift_parser.add_argument("--from", dest="base", required=True, help="base ref for git diff")
    drift_parser.add_argument("--to", dest="head", required=True, help="head ref for git diff")
    drift_parser.add_argument("--save", action="store_true", help="save diagnostics under docs/knowledge/changes")
    drift_parser.set_defaults(func=command_check_drift)
    diff_parser = subparsers.add_parser("diff", help="summarize changed paths by knowledge taxonomy")
    diff_parser.add_argument("--from", dest="base", required=True, help="base ref for git diff")
    diff_parser.add_argument("--to", dest="head", required=True, help="head ref for git diff")
    diff_parser.set_defaults(func=command_diff)
    sync_skills_parser = subparsers.add_parser("sync-skills", help="sync knowledge skills to tool targets")
    sync_skills_parser.add_argument("--target", required=True, help="skill target to sync")
    sync_skills_parser.set_defaults(func=command_sync_skills)
    query_parser = subparsers.add_parser("query", help="query knowledge graph summaries")
    query_parser.add_argument("--text", required=True, help="query text")
    query_parser.add_argument("--limit", type=int, default=5, help="maximum result count")
    query_parser.set_defaults(func=command_query)
    return parser


def main(argv: list[str] | None = None) -> int:
    parser = build_parser()
    args = parser.parse_args(argv)
    return args.func(args)


if __name__ == "__main__":
    sys.exit(main())
