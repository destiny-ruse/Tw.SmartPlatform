#!/usr/bin/env python3
from __future__ import annotations

import argparse
import difflib
import json
import re
import sys
from dataclasses import dataclass
from datetime import date, datetime, timedelta, timezone
from pathlib import Path
from typing import Any
from urllib.parse import unquote


GENERATOR_VERSION = "2.0.0"
REPO_ROOT = Path(__file__).resolve().parents[2]
STANDARDS_DIR = REPO_ROOT / "docs" / "standards"
INDEX_PATH = STANDARDS_DIR / "index.generated.json"
RULES_DIR = REPO_ROOT / "tools" / "standards" / "rules"
TEMPLATES_DIR = REPO_ROOT / "tools" / "standards" / "templates"

REQUIRED_FIELDS = [
    "id",
    "title",
    "doc_type",
    "status",
    "version",
    "owners",
    "roles",
    "stacks",
    "tags",
    "summary",
    "machine_rules",
    "supersedes",
    "superseded_by",
    "review_after",
]
FORBIDDEN_FIELDS = {"applies_to"}
VALID_DOC_TYPES = {"rule", "reference", "process", "decision"}
VALID_STATUSES = {"draft", "active", "deprecated", "superseded"}
VALID_ROLES = {"architect", "backend", "frontend", "qa", "devops", "ai"}
VALID_STACKS = {"dotnet", "java", "python", "vue-ts", "uniapp"}
ID_PATTERN = re.compile(r"^[a-z][a-z0-9-]*(\.[a-z][a-z0-9-]*)+$")
VERSION_PATTERN = re.compile(r"^\d+\.\d+\.\d+$")
TOKEN_PATTERN = re.compile(r"^[a-z][a-z0-9-]*$")
ANCHOR_PATTERN = re.compile(r"^<!--\s*anchor:\s*([a-z][a-z0-9-]*)\s*-->$")
REGION_START_PATTERN = re.compile(r"^<!--\s*region:\s*([a-z][a-z0-9-]*)\s*-->$")
REGION_END_PATTERN = re.compile(r"^<!--\s*endregion:\s*([a-z][a-z0-9-]*)\s*-->$")
STANDARD_REF_PATTERN = re.compile(
    r"(?<![A-Za-z0-9_.-])([a-z][a-z0-9-]*(?:\.[a-z][a-z0-9-]*)+)#([a-z][a-z0-9-]*)(?::([a-z][a-z0-9-]*))?"
)
HEADING_PATTERN = re.compile(r"^(#{1,6})\s+(.+?)\s*#*\s*$")
LINK_PATTERN = re.compile(r"(?<!!)\[[^\]]+\]\(([^)]+)\)")


@dataclass(frozen=True)
class Diagnostic:
    level: str
    code: str
    location: str
    message: str

    def format(self) -> str:
        return f"{self.level:<5} [{self.code}] {self.location}: {self.message}"


@dataclass(frozen=True)
class StandardDocument:
    path: Path
    metadata: dict[str, Any]
    lines: list[str]
    body_start_line: int


class FrontMatterError(Exception):
    def __init__(self, path: Path, message: str) -> None:
        self.path = path
        self.message = message
        super().__init__(message)


def rel_path(path: Path) -> str:
    try:
        return path.resolve().relative_to(REPO_ROOT.resolve()).as_posix()
    except ValueError:
        return path.as_posix()


def error(code: str, path: Path | str, message: str) -> Diagnostic:
    location = rel_path(path) if isinstance(path, Path) else path
    return Diagnostic("ERROR", code, location, message)


def warn(code: str, path: Path | str, message: str) -> Diagnostic:
    location = rel_path(path) if isinstance(path, Path) else path
    return Diagnostic("WARN", code, location, message)


def has_errors(messages: list[Diagnostic]) -> bool:
    return any(message.level == "ERROR" for message in messages)


def emit(messages: list[Diagnostic]) -> None:
    for message in messages:
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
    if value.startswith("[") and value.endswith("]"):
        inside = value[1:-1].strip()
        if inside == "":
            return []
        return [unquote_text(item.strip()) for item in inside.split(",")]
    if (value.startswith('"') and value.endswith('"')) or (
        value.startswith("'") and value.endswith("'")
    ):
        return value[1:-1]
    if value == "true":
        return True
    if value == "false":
        return False
    return value


def unquote_text(value: str) -> str:
    if (value.startswith('"') and value.endswith('"')) or (
        value.startswith("'") and value.endswith("'")
    ):
        return value[1:-1]
    return value


def parse_front_matter(path: Path, lines: list[str]) -> tuple[dict[str, Any], int]:
    if not lines or lines[0].strip() != "---":
        raise FrontMatterError(path, "missing opening front matter marker")

    closing_index = None
    for index in range(1, len(lines)):
        if lines[index].strip() == "---":
            closing_index = index
            break
    if closing_index is None:
        raise FrontMatterError(path, "missing closing front matter marker")

    metadata = parse_metadata_lines(path, lines[1:closing_index])
    return metadata, closing_index + 2


def parse_metadata_lines(path: Path, lines: list[str]) -> dict[str, Any]:
    metadata: dict[str, Any] = {}
    index = 0

    while index < len(lines):
        raw_line = lines[index]
        stripped = raw_line.strip()
        if not stripped or stripped.startswith("#"):
            index += 1
            continue
        if raw_line.startswith((" ", "\t")):
            raise FrontMatterError(path, f"unexpected indentation near '{stripped}'")
        if ":" not in raw_line:
            raise FrontMatterError(path, f"invalid metadata line '{stripped}'")

        key, raw_value = raw_line.split(":", 1)
        key = key.strip()
        if not key:
            raise FrontMatterError(path, "metadata key cannot be empty")

        if raw_value.strip():
            metadata[key] = parse_scalar(raw_value)
            index += 1
            continue

        index += 1
        values: list[Any] = []
        consumed_block = False

        while index < len(lines):
            child_line = lines[index]
            child_stripped = child_line.strip()
            if not child_stripped:
                index += 1
                continue
            if not child_line.startswith("  "):
                break
            if not child_stripped.startswith("- "):
                raise FrontMatterError(
                    path,
                    f"unsupported block value for '{key}' near '{child_stripped}'",
                )

            consumed_block = True
            item_text = child_stripped[2:].strip()
            if ":" in item_text and not item_text.startswith("["):
                item: dict[str, Any] = {}
                item_key, item_value = item_text.split(":", 1)
                item[item_key.strip()] = parse_scalar(item_value)
                index += 1

                while index < len(lines):
                    nested_line = lines[index]
                    nested_stripped = nested_line.strip()
                    if not nested_stripped:
                        index += 1
                        continue
                    if not nested_line.startswith("    "):
                        break
                    if ":" not in nested_stripped:
                        raise FrontMatterError(
                            path,
                            f"invalid nested metadata line '{nested_stripped}'",
                        )
                    nested_key, nested_value = nested_stripped.split(":", 1)
                    item[nested_key.strip()] = parse_scalar(nested_value)
                    index += 1
                values.append(item)
            else:
                values.append(parse_scalar(item_text))
                index += 1

        metadata[key] = values if consumed_block else None

    return metadata


def standard_files() -> list[Path]:
    if not STANDARDS_DIR.exists():
        return []
    return sorted(
        path
        for path in STANDARDS_DIR.rglob("*.md")
        if path.name != "README.md"
        and not any(part.startswith("_") for part in path.relative_to(STANDARDS_DIR).parts)
    )


def load_standard_docs() -> tuple[list[StandardDocument], list[Diagnostic]]:
    docs: list[StandardDocument] = []
    messages: list[Diagnostic] = []

    for path in standard_files():
        try:
            text = read_text(path)
            lines = text.splitlines()
            metadata, body_start_line = parse_front_matter(path, lines)
            docs.append(StandardDocument(path, metadata, lines, body_start_line))
        except UnicodeDecodeError:
            messages.append(error("standard-encoding", path, "file must be UTF-8"))
        except FrontMatterError as exc:
            messages.append(error("standard-metadata", exc.path, exc.message))

    return docs, messages


def collect_validation_messages() -> list[Diagnostic]:
    docs, messages = load_standard_docs()
    if not docs:
        messages.append(error("standard-metadata", STANDARDS_DIR, "no standard documents found"))
        return messages

    seen_ids: dict[str, Path] = {}
    today = date.today()

    for doc in docs:
        metadata = doc.metadata
        for field in REQUIRED_FIELDS:
            if field not in metadata:
                messages.append(
                    error("standard-metadata", doc.path, f'missing required field "{field}"')
                )

        for field in FORBIDDEN_FIELDS:
            if field in metadata:
                messages.append(
                    error("standard-metadata", doc.path, f'field "{field}" is not allowed in v2')
                )

        standard_id = metadata.get("id")
        if isinstance(standard_id, str):
            if not ID_PATTERN.match(standard_id):
                messages.append(
                    error(
                        "standard-metadata",
                        doc.path,
                        'field "id" must use dotted lowercase form',
                    )
                )
            elif standard_id in seen_ids:
                messages.append(
                    error(
                        "standard-metadata",
                        doc.path,
                        f'duplicate id "{standard_id}" also used by {rel_path(seen_ids[standard_id])}',
                    )
                )
            else:
                seen_ids[standard_id] = doc.path
        elif "id" in metadata:
            messages.append(error("standard-metadata", doc.path, 'field "id" must be a string'))

        title = metadata.get("title")
        if not isinstance(title, str) or not title.strip():
            messages.append(
                error("standard-metadata", doc.path, 'field "title" must be a non-empty string')
            )

        doc_type = metadata.get("doc_type")
        if doc_type not in VALID_DOC_TYPES:
            messages.append(
                error(
                    "standard-metadata",
                    doc.path,
                    f'field "doc_type" must be one of {sorted(VALID_DOC_TYPES)}',
                )
            )

        status = metadata.get("status")
        if status not in VALID_STATUSES:
            messages.append(
                error(
                    "standard-metadata",
                    doc.path,
                    f'field "status" must be one of {sorted(VALID_STATUSES)}',
                )
            )

        version = metadata.get("version")
        if not isinstance(version, str) or not VERSION_PATTERN.match(version):
            messages.append(
                error(
                    "standard-metadata",
                    doc.path,
                    'field "version" must use MAJOR.MINOR.PATCH',
                )
            )

        for field in ("owners", "roles", "tags"):
            value = metadata.get(field)
            if not is_non_empty_string_list(value):
                messages.append(
                    error(
                        "standard-metadata",
                        doc.path,
                        f'field "{field}" must be a non-empty string array',
                    )
                )

        stacks = metadata.get("stacks")
        if not is_string_list(stacks):
            messages.append(
                error("standard-metadata", doc.path, 'field "stacks" must be a string array')
            )

        for role in metadata.get("roles") or []:
            if role not in VALID_ROLES:
                messages.append(error("standard-metadata", doc.path, f'unknown role "{role}"'))

        for stack in metadata.get("stacks") or []:
            if stack not in VALID_STACKS:
                messages.append(error("standard-metadata", doc.path, f'unknown stack "{stack}"'))

        for tag in metadata.get("tags") or []:
            if not isinstance(tag, str) or not TOKEN_PATTERN.match(tag):
                messages.append(error("standard-metadata", doc.path, f'invalid tag "{tag}"'))

        summary = metadata.get("summary")
        if not isinstance(summary, str) or not summary.strip():
            messages.append(
                error("standard-metadata", doc.path, 'field "summary" must be a non-empty string')
            )

        review_after = metadata.get("review_after")
        if isinstance(review_after, str):
            try:
                review_date = date.fromisoformat(review_after)
                if review_date < today:
                    messages.append(
                        warn(
                            "standard-review",
                            doc.path,
                            'field "review_after" is in the past',
                        )
                    )
            except ValueError:
                messages.append(
                    error(
                        "standard-metadata",
                        doc.path,
                        'field "review_after" must use YYYY-MM-DD',
                    )
                )
        elif "review_after" in metadata:
            messages.append(
                error("standard-metadata", doc.path, 'field "review_after" must be a string')
            )

        supersedes = metadata.get("supersedes")
        if supersedes is not None and not is_string_list(supersedes):
            messages.append(
                error("standard-metadata", doc.path, 'field "supersedes" must be a string array')
            )

        superseded_by = metadata.get("superseded_by")
        if status == "superseded" and not superseded_by:
            messages.append(
                error(
                    "standard-metadata",
                    doc.path,
                    'status "superseded" requires field "superseded_by"',
                )
            )
        if superseded_by and superseded_by == standard_id:
            messages.append(
                error(
                    "standard-metadata",
                    doc.path,
                    'field "superseded_by" cannot reference the same standard id',
                )
            )

        machine_rules = metadata.get("machine_rules", [])
        if machine_rules is None:
            machine_rules = []
        if not isinstance(machine_rules, list):
            messages.append(
                error("standard-metadata", doc.path, 'field "machine_rules" must be an array')
            )
        else:
            for index, rule in enumerate(machine_rules, start=1):
                if not isinstance(rule, dict):
                    messages.append(
                        error(
                            "standard-metadata",
                            doc.path,
                            f"machine_rules item {index} must be an object",
                        )
                    )
                    continue
                for field in ("id", "path", "type"):
                    if not rule.get(field):
                        messages.append(
                            error(
                                "standard-metadata",
                                doc.path,
                                f'machine_rules item {index} missing field "{field}"',
                            )
                        )

        _, section_messages = build_section_index(doc)
        messages.extend(section_messages)

    return messages


def is_string_list(value: Any) -> bool:
    return isinstance(value, list) and all(isinstance(item, str) for item in value)


def is_non_empty_string_list(value: Any) -> bool:
    return is_string_list(value) and len(value) > 0 and all(item.strip() for item in value)


def collect_machine_rule_messages() -> list[Diagnostic]:
    docs, messages = load_standard_docs()
    if has_errors(messages):
        return messages

    standards_by_id = {
        doc.metadata.get("id"): doc
        for doc in docs
        if isinstance(doc.metadata.get("id"), str)
    }
    referenced_rule_paths: dict[Path, tuple[StandardDocument, dict[str, Any]]] = {}

    for doc in docs:
        standard_id = doc.metadata.get("id")
        status = doc.metadata.get("status")
        machine_rules = doc.metadata.get("machine_rules") or []
        if not isinstance(machine_rules, list):
            continue

        for rule in machine_rules:
            if not isinstance(rule, dict):
                continue
            rule_path_value = rule.get("path")
            if not isinstance(rule_path_value, str) or not rule_path_value.strip():
                continue

            rule_path = (REPO_ROOT / rule_path_value).resolve()
            referenced_rule_paths[rule_path] = (doc, rule)
            if not rule_path.exists():
                messages.append(
                    error(
                        "machine-rule",
                        doc.path,
                        f'machine rule path does not exist: "{rule_path_value}"',
                    )
                )
                continue

            try:
                rule_data = json.loads(read_text(rule_path))
            except json.JSONDecodeError as exc:
                messages.append(
                    error(
                        "machine-rule",
                        rule_path,
                        f"rule file is not valid JSON: {exc.msg}",
                    )
                )
                continue

            for field in ("id", "standard_id", "type", "version"):
                if not rule_data.get(field):
                    messages.append(
                        error("machine-rule", rule_path, f'missing required field "{field}"')
                    )

            if rule_data.get("id") != rule.get("id"):
                messages.append(
                    error(
                        "machine-rule",
                        rule_path,
                        'field "id" must match the document machine_rules id',
                    )
                )
            if rule_data.get("standard_id") != standard_id:
                messages.append(
                    error(
                        "machine-rule",
                        rule_path,
                        'field "standard_id" must match the standard document id',
                    )
                )
            if rule_data.get("type") != rule.get("type"):
                messages.append(
                    error(
                        "machine-rule",
                        rule_path,
                        'field "type" must match the document machine_rules type',
                    )
                )
            if status == "active" and rule_data.get("status") == "deprecated":
                messages.append(
                    error("machine-rule", rule_path, "active standards cannot bind deprecated rules")
                )

    if RULES_DIR.exists():
        for rule_path in sorted(RULES_DIR.rglob("*.json")):
            resolved = rule_path.resolve()
            if resolved not in referenced_rule_paths:
                messages.append(
                    error(
                        "machine-rule",
                        rule_path,
                        "rule file is not referenced by any standard document",
                    )
                )
                continue

            try:
                rule_data = json.loads(read_text(rule_path))
            except json.JSONDecodeError:
                continue
            standard_id = rule_data.get("standard_id")
            if standard_id and standard_id not in standards_by_id:
                messages.append(
                    error(
                        "machine-rule",
                        rule_path,
                        f'field "standard_id" references unknown standard "{standard_id}"',
                    )
                )

    return messages


def markdown_files_for_link_check() -> list[Path]:
    roots = [
        STANDARDS_DIR,
        REPO_ROOT / "docs" / "rfcs",
        REPO_ROOT / "docs" / "adrs",
    ]
    files: list[Path] = []
    for root in roots:
        if root.exists():
            files.extend(sorted(root.rglob("*.md")))
    return files


def collect_link_messages() -> list[Diagnostic]:
    messages: list[Diagnostic] = []
    anchor_cache: dict[Path, set[str]] = {}

    for path in markdown_files_for_link_check():
        text = read_text(path)
        for match in LINK_PATTERN.finditer(text):
            target = match.group(1).strip()
            if not target or is_external_link(target):
                continue

            target = target.split()[0].strip("<>")
            target_path_text, _, anchor = target.partition("#")

            if target_path_text:
                target_path = (path.parent / unquote(target_path_text)).resolve()
            else:
                target_path = path.resolve()

            if not target_path.exists():
                messages.append(
                    error(
                        "doc-link",
                        path,
                        f'link target does not exist: "{target}"',
                    )
                )
                continue

            if anchor:
                if target_path.suffix.lower() != ".md":
                    messages.append(
                        error(
                            "doc-link",
                            path,
                            f'link target has an anchor but is not Markdown: "{target}"',
                        )
                    )
                    continue
                anchors = anchor_cache.setdefault(target_path, markdown_anchors(target_path))
                normalized_anchor = normalize_anchor(anchor)
                if normalized_anchor not in anchors:
                    messages.append(
                        error(
                            "doc-link",
                            path,
                            f'anchor "#{anchor}" does not exist in {rel_path(target_path)}',
                        )
                    )

    return messages


def is_external_link(target: str) -> bool:
    lowered = target.lower()
    return (
        "://" in lowered
        or lowered.startswith("mailto:")
        or lowered.startswith("tel:")
    )


def markdown_anchors(path: Path) -> set[str]:
    anchors: set[str] = set()
    counts: dict[str, int] = {}
    in_fence = False
    for line in read_text(path).splitlines():
        stripped = line.strip()
        if stripped.startswith(("```", "~~~")):
            in_fence = not in_fence
            continue
        if in_fence:
            continue
        match = HEADING_PATTERN.match(line)
        if not match:
            continue
        title = match.group(2).strip()
        anchor = make_anchor(title, counts)
        anchors.add(anchor)
    return anchors


def normalize_anchor(anchor: str) -> str:
    return unquote(anchor).strip().lower()


def make_anchor(title: str, counts: dict[str, int]) -> str:
    title = re.sub(r"\s*#+\s*$", "", title).strip().lower()
    chars: list[str] = []
    for char in title:
        if char.isalnum() or char in {" ", "-"}:
            chars.append(char)
    base = re.sub(r"\s+", "-", "".join(chars)).strip("-")
    if not base:
        base = "section"

    count = counts.get(base, 0)
    counts[base] = count + 1
    if count == 0:
        return base
    return f"{base}-{count}"


def section_index_path(standard_id: str) -> str:
    return f"docs/standards/_index/sections/{standard_id}.generated.json"


def shard_path(kind: str, value: str) -> str:
    return f"docs/standards/_index/by-{kind}/{value}.generated.json"


def standard_entry(doc: StandardDocument) -> dict[str, Any]:
    metadata = doc.metadata
    standard_id = metadata.get("id")
    shards: list[str] = []
    for role in metadata.get("roles") or []:
        shards.append(shard_path("role", role))
    for stack in metadata.get("stacks") or []:
        shards.append(shard_path("stack", stack))
    shards.append(shard_path("doc-type", metadata.get("doc_type")))
    for tag in metadata.get("tags") or []:
        shards.append(shard_path("tag", tag))

    return {
        "id": standard_id,
        "title": metadata.get("title"),
        "doc_type": metadata.get("doc_type"),
        "status": metadata.get("status"),
        "version": metadata.get("version"),
        "path": rel_path(doc.path),
        "roles": metadata.get("roles") or [],
        "stacks": metadata.get("stacks") or [],
        "tags": metadata.get("tags") or [],
        "summary": metadata.get("summary"),
        "sections_index": section_index_path(standard_id),
        "shards": sorted(shards),
    }


def build_l1_payload(
    kind: str,
    value: str,
    entries: list[dict[str, Any]],
    generated_at: str,
) -> dict[str, Any]:
    return {
        "schema_version": "2.0.0",
        "generated_at": generated_at,
        "generator": {"name": "tools/standards/standards.py", "version": GENERATOR_VERSION},
        "kind": kind,
        "value": value,
        "standards": sorted(
            [
                {
                    "id": entry["id"],
                    "title": entry["title"],
                    "doc_type": entry["doc_type"],
                    "status": entry["status"],
                    "version": entry["version"],
                    "path": entry["path"],
                    "roles": entry["roles"],
                    "stacks": entry["stacks"],
                    "tags": entry["tags"],
                    "summary": entry["summary"],
                    "sections_index": entry["sections_index"],
                }
                for entry in entries
            ],
            key=lambda item: item["id"],
        ),
    }


def build_indexes(
    existing_generated_at: str | None = None,
) -> tuple[dict[str, dict[str, Any]], list[Diagnostic]]:
    docs, messages = load_standard_docs()
    generated_at = existing_generated_at or current_utc_timestamp()
    payloads: dict[str, dict[str, Any]] = {}
    entries: list[dict[str, Any]] = []
    shard_entries: dict[tuple[str, str], list[dict[str, Any]]] = {}

    for doc in sorted(docs, key=lambda item: str(item.metadata.get("id", rel_path(item.path)))):
        entry = standard_entry(doc)
        entries.append(entry)
        section_payload, section_messages = build_section_index(doc)
        messages.extend(section_messages)
        payloads[entry["sections_index"]] = section_payload

        for role in entry["roles"]:
            shard_entries.setdefault(("role", role), []).append(entry)
        for stack in entry["stacks"]:
            shard_entries.setdefault(("stack", stack), []).append(entry)
        shard_entries.setdefault(("doc-type", entry["doc_type"]), []).append(entry)
        for tag in entry["tags"]:
            shard_entries.setdefault(("tag", tag), []).append(entry)

    payloads["docs/standards/index.generated.json"] = {
        "schema_version": "2.0.0",
        "generator": {"name": "tools/standards/standards.py", "version": GENERATOR_VERSION},
        "generated_at": generated_at,
        "standards": entries,
    }

    for (kind, value), shard_standards in sorted(shard_entries.items()):
        payloads[shard_path(kind, value)] = build_l1_payload(
            kind,
            value,
            shard_standards,
            generated_at,
        )

    return dict(sorted(payloads.items())), messages


def current_utc_timestamp() -> str:
    return datetime.now(timezone.utc).replace(microsecond=0).isoformat().replace("+00:00", "Z")


def extract_sections(doc: StandardDocument) -> list[dict[str, Any]]:
    headings: list[tuple[int, int, str, str]] = []
    counts: dict[str, int] = {}
    in_fence = False

    for line_number, line in enumerate(doc.lines, start=1):
        stripped = line.strip()
        if stripped.startswith(("```", "~~~")):
            in_fence = not in_fence
            continue
        if in_fence:
            continue
        match = HEADING_PATTERN.match(line)
        if not match:
            continue
        level = len(match.group(1))
        if level not in (2, 3):
            continue
        title = match.group(2).strip()
        headings.append((line_number, level, title, make_anchor(title, counts)))

    sections: list[dict[str, Any]] = []
    for index, (start_line, level, title, anchor) in enumerate(headings):
        if index + 1 < len(headings):
            end_line = headings[index + 1][0] - 1
        else:
            end_line = len(doc.lines)
        sections.append(
            {
                "level": level,
                "title": title,
                "anchor": anchor,
                "start_line": start_line,
                "end_line": end_line,
                "summary": section_summary(doc.lines, start_line, end_line),
            }
        )
    return sections


def build_section_index(doc: StandardDocument) -> tuple[dict[str, Any], list[Diagnostic]]:
    messages: list[Diagnostic] = []
    sections: list[dict[str, Any]] = []
    pending_anchor: tuple[str, int] | None = None
    current_section: dict[str, Any] | None = None
    open_region: tuple[str, int] | None = None
    in_fence = False

    for line_number, line in enumerate(doc.lines, start=1):
        stripped = line.strip()
        if stripped.startswith(("```", "~~~")):
            in_fence = not in_fence
            continue
        if in_fence:
            continue

        anchor_match = ANCHOR_PATTERN.match(stripped)
        if anchor_match:
            pending_anchor = (anchor_match.group(1), line_number)
            continue

        heading_match = HEADING_PATTERN.match(stripped)
        if heading_match and pending_anchor:
            if current_section is not None:
                current_section["end_line"] = line_number - 1
            anchor, _anchor_line = pending_anchor
            current_section = {
                "anchor": anchor,
                "title": heading_match.group(2).strip(),
                "level": len(heading_match.group(1)),
                "start_line": line_number,
                "end_line": len(doc.lines),
                "regions": [],
            }
            sections.append(current_section)
            pending_anchor = None
            continue

        if heading_match and pending_anchor is None:
            continue

        region_start = REGION_START_PATTERN.match(stripped)
        if region_start:
            if current_section is None:
                messages.append(
                    error(
                        "standard-anchor",
                        doc.path,
                        f'region "{region_start.group(1)}" appears before any anchored section',
                    )
                )
            elif open_region is not None:
                messages.append(
                    error(
                        "standard-anchor",
                        doc.path,
                        f'region "{region_start.group(1)}" starts before region "{open_region[0]}" ends',
                    )
                )
            else:
                open_region = (region_start.group(1), line_number)
            continue

        region_end = REGION_END_PATTERN.match(stripped)
        if region_end:
            if open_region is None:
                messages.append(
                    error(
                        "standard-anchor",
                        doc.path,
                        f'endregion "{region_end.group(1)}" has no matching region start',
                    )
                )
            elif region_end.group(1) != open_region[0]:
                messages.append(
                    error(
                        "standard-anchor",
                        doc.path,
                        f'endregion "{region_end.group(1)}" does not match region "{open_region[0]}"',
                    )
                )
                open_region = None
            else:
                assert current_section is not None
                current_section["regions"].append(
                    {
                        "id": open_region[0],
                        "start_line": open_region[1],
                        "end_line": line_number,
                    }
                )
                open_region = None

    if pending_anchor is not None:
        messages.append(
            error(
                "standard-anchor",
                doc.path,
                f'anchor "{pending_anchor[0]}" is not followed by a Markdown heading',
            )
        )
    if open_region is not None:
        messages.append(
            error("standard-anchor", doc.path, f'region "{open_region[0]}" is not closed')
        )
    if not sections:
        messages.append(
            error("standard-anchor", doc.path, "standard must contain at least one explicit anchor")
        )

    seen_anchors: set[str] = set()
    for section in sections:
        anchor = section["anchor"]
        if anchor in seen_anchors:
            messages.append(error("standard-anchor", doc.path, f'duplicate anchor "{anchor}"'))
        seen_anchors.add(anchor)

        seen_regions: set[str] = set()
        for region in section["regions"]:
            region_id = region["id"]
            if region_id in seen_regions:
                messages.append(
                    error(
                        "standard-anchor",
                        doc.path,
                        f'duplicate region "{region_id}" in anchor "{anchor}"',
                    )
                )
            seen_regions.add(region_id)

    return {
        "id": doc.metadata.get("id"),
        "path": rel_path(doc.path),
        "version": doc.metadata.get("version"),
        "sections": sections,
    }, messages


def section_summary(lines: list[str], start_line: int, end_line: int) -> str:
    in_fence = False
    for line in lines[start_line:end_line]:
        stripped = line.strip()
        if stripped.startswith(("```", "~~~")):
            in_fence = not in_fence
            continue
        if in_fence or not stripped or stripped.startswith("#"):
            continue
        if re.match(r"^\|?\s*-{3,}", stripped):
            continue
        text = re.sub(r"[`*_]", "", stripped)
        if len(text) > 160:
            return text[:157] + "..."
        return text
    return ""


def generated_index_files() -> list[Path]:
    files: list[Path] = []
    if INDEX_PATH.exists():
        files.append(INDEX_PATH)
    index_dir = STANDARDS_DIR / "_index"
    if index_dir.exists():
        files.extend(sorted(index_dir.rglob("*.generated.json")))
    return files


def write_indexes(payloads: dict[str, dict[str, Any]]) -> None:
    expected_paths = {(REPO_ROOT / relative_path).resolve() for relative_path in payloads}
    for existing_path in generated_index_files():
        if existing_path.resolve() not in expected_paths:
            existing_path.unlink()

    for relative_path, payload in sorted(payloads.items()):
        content = json.dumps(payload, ensure_ascii=False, indent=2) + "\n"
        write_text(REPO_ROOT / relative_path, content)


def collect_index_messages() -> list[Diagnostic]:
    messages: list[Diagnostic] = []
    if not INDEX_PATH.exists():
        return [
            error(
                "standard-index",
                INDEX_PATH,
                "index file does not exist; run generate-index",
            )
        ]

    try:
        existing = json.loads(read_text(INDEX_PATH))
    except json.JSONDecodeError as exc:
        return [error("standard-index", INDEX_PATH, f"index is not valid JSON: {exc.msg}")]

    generated, build_messages = build_indexes(existing.get("generated_at"))
    messages.extend(build_messages)
    if has_errors(messages):
        return messages

    for relative_path, expected_payload in generated.items():
        path = REPO_ROOT / relative_path
        if not path.exists():
            messages.append(
                error(
                    "standard-index",
                    path,
                    "generated index file does not exist; run generate-index",
                )
            )
            continue

        try:
            existing_payload = json.loads(read_text(path))
        except json.JSONDecodeError as exc:
            messages.append(error("standard-index", path, f"index is not valid JSON: {exc.msg}"))
            continue

        if existing_payload != expected_payload:
            existing_text = json.dumps(
                existing_payload,
                ensure_ascii=False,
                indent=2,
                sort_keys=True,
            ).splitlines()
            generated_text = json.dumps(
                expected_payload,
                ensure_ascii=False,
                indent=2,
                sort_keys=True,
            ).splitlines()
            diff = "\n".join(
                difflib.unified_diff(
                    existing_text,
                    generated_text,
                    fromfile=relative_path,
                    tofile="generated",
                    lineterm="",
                )
            )
            hint = "index is out of date; run generate-index"
            if diff:
                hint += "\n" + diff
            messages.append(error("standard-index", path, hint))

    expected_paths = {(REPO_ROOT / relative_path).resolve() for relative_path in generated}
    for existing_path in generated_index_files():
        if existing_path.resolve() not in expected_paths:
            messages.append(
                error(
                    "standard-index",
                    existing_path,
                    "obsolete generated index file; run generate-index",
                )
            )

    return messages


def command_new_standard(args: argparse.Namespace) -> int:
    if not ID_PATTERN.match(args.id):
        print('ERROR [standard-new] --id must use dotted lowercase form')
        return 1

    if args.path:
        target_path = (REPO_ROOT / args.path).resolve()
    else:
        target_path = STANDARDS_DIR / Path(*args.id.split(".")).with_suffix(".md")
        target_path = target_path.resolve()

    try:
        target_path.relative_to(STANDARDS_DIR.resolve())
    except ValueError:
        print("ERROR [standard-new] target path must be under docs/standards")
        return 1

    if target_path.exists():
        print(f"ERROR [standard-new] {rel_path(target_path)} already exists")
        return 1

    template_path = TEMPLATES_DIR / "standard.md"
    template = read_text(template_path)
    today = date.today()
    replacements = {
        "{{id}}": args.id,
        "{{title}}": args.title,
        "{{status}}": args.status,
        "{{owner}}": args.owner,
        "{{applies_to}}": args.applies_to,
        "{{review_after}}": (today + timedelta(days=args.review_days)).isoformat(),
        "{{today}}": today.isoformat(),
    }
    content = template
    for placeholder, value in replacements.items():
        content = content.replace(placeholder, value)

    write_text(target_path, content)
    print(f"Created {rel_path(target_path)}")
    return 0


def command_generate_index(_: argparse.Namespace) -> int:
    messages = collect_validation_messages()
    emit(messages)
    if has_errors(messages):
        return 1

    payloads, build_messages = build_indexes()
    emit(build_messages)
    if has_errors(build_messages):
        return 1

    write_indexes(payloads)
    print(f"Generated {len(payloads)} standards index files")
    return 0


def command_validate(_: argparse.Namespace) -> int:
    messages = collect_validation_messages()
    emit(messages)
    if has_errors(messages):
        return 1
    print("OK standards metadata is valid")
    return 0


def command_check_links(_: argparse.Namespace) -> int:
    messages = collect_link_messages()
    emit(messages)
    if has_errors(messages):
        return 1
    print("OK documentation links are valid")
    return 0


def command_check_machine_rules(_: argparse.Namespace) -> int:
    messages = collect_machine_rule_messages()
    emit(messages)
    if has_errors(messages):
        return 1
    print("OK machine rule bindings are valid")
    return 0


def command_check(_: argparse.Namespace) -> int:
    messages: list[Diagnostic] = []
    messages.extend(collect_validation_messages())
    messages.extend(collect_link_messages())
    messages.extend(collect_machine_rule_messages())
    messages.extend(collect_index_messages())
    emit(messages)
    if has_errors(messages):
        return 1
    if messages:
        print("OK standards checks passed with warnings")
    else:
        print("OK standards checks passed")
    return 0


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description="Engineering standards tooling")
    subparsers = parser.add_subparsers(dest="command", required=True)

    new_standard = subparsers.add_parser("new-standard", help="Create a standard skeleton")
    new_standard.add_argument("--id", required=True, help="Dotted standard id")
    new_standard.add_argument("--title", required=True, help="Standard title")
    new_standard.add_argument("--status", default="draft", choices=sorted(VALID_STATUSES))
    new_standard.add_argument("--owner", default="architecture-team")
    new_standard.add_argument("--applies-to", default="standards")
    new_standard.add_argument("--review-days", type=int, default=180)
    new_standard.add_argument("--path", help="Repo-relative target path under docs/standards")
    new_standard.set_defaults(func=command_new_standard)

    generate_index = subparsers.add_parser("generate-index", help="Generate the standards index")
    generate_index.set_defaults(func=command_generate_index)

    validate = subparsers.add_parser("validate", help="Validate standard metadata")
    validate.set_defaults(func=command_validate)

    check_links = subparsers.add_parser("check-links", help="Validate documentation links")
    check_links.set_defaults(func=command_check_links)

    check_machine_rules = subparsers.add_parser(
        "check-machine-rules",
        help="Validate machine rule bindings",
    )
    check_machine_rules.set_defaults(func=command_check_machine_rules)

    check = subparsers.add_parser("check", help="Run all standards checks")
    check.set_defaults(func=command_check)

    return parser


def main(argv: list[str] | None = None) -> int:
    parser = build_parser()
    args = parser.parse_args(argv)
    try:
        return args.func(args)
    except KeyboardInterrupt:
        print("Interrupted", file=sys.stderr)
        return 130


if __name__ == "__main__":
    raise SystemExit(main())
