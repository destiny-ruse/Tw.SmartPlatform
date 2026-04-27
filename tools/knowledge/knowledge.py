#!/usr/bin/env python3
from __future__ import annotations

import argparse
import re
import sys
from dataclasses import dataclass
from pathlib import Path
from typing import Any


GENERATOR_VERSION = "1.0.0"
REPO_ROOT = Path(__file__).resolve().parents[2]
KNOWLEDGE_DIR = REPO_ROOT / "docs" / "knowledge"
GRAPH_DIR = KNOWLEDGE_DIR / "graph"
GENERATED_DIR = KNOWLEDGE_DIR / "generated"
TAXONOMY_PATH = KNOWLEDGE_DIR / "taxonomy.yaml"

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
ID_PATTERN = re.compile(r"^[a-z][a-z0-9-]*(\.[a-z0-9][a-z0-9-]*)+$")
VERSION_PATTERN = re.compile(r"^\d+\.\d+\.\d+$")
TOKEN_PATTERN = re.compile(r"^[a-z][a-z0-9-]*$")
GENERATED_SUFFIX = ".generated"


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

        source = data.get("source")
        if isinstance(source, dict):
            declared_in = source.get("declared_in")
            actual_path = rel_path(node.path)
            if declared_in and declared_in != actual_path:
                messages.append(
                    error(
                        "knowledge.declared-in",
                        location,
                        f"source.declared_in 必须等于实际路径 {actual_path}",
                        "请更新 source.declared_in，使其与文件路径保持一致。",
                    )
                )
            evidence = source.get("evidence")
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

    return messages


def command_check(_args: argparse.Namespace) -> int:
    messages = collect_validation_messages()
    emit(messages)
    if has_errors(messages):
        return 1
    print("OK knowledge checks passed")
    return 0


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description="Knowledge graph maintenance tool")
    subparsers = parser.add_subparsers(dest="command", required=True)
    check_parser = subparsers.add_parser("check", help="validate knowledge graph nodes")
    check_parser.set_defaults(func=command_check)
    return parser


def main(argv: list[str] | None = None) -> int:
    parser = build_parser()
    args = parser.parse_args(argv)
    return args.func(args)


if __name__ == "__main__":
    sys.exit(main())
