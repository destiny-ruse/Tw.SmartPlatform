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


def build_index(existing_generated_at: str | None = None) -> tuple[dict[str, Any], list[Diagnostic]]:
    docs, messages = load_standard_docs()
    standards: list[dict[str, Any]] = []

    for doc in sorted(docs, key=lambda item: str(item.metadata.get("id", rel_path(item.path)))):
        metadata = doc.metadata
        standards.append(
            {
                "id": metadata.get("id"),
                "title": metadata.get("title"),
                "status": metadata.get("status"),
                "version": metadata.get("version"),
                "path": rel_path(doc.path),
                "owners": metadata.get("owners") or [],
                "applies_to": metadata.get("applies_to") or [],
                "review_after": metadata.get("review_after"),
                "machine_rules": metadata.get("machine_rules") or [],
                "supersedes": metadata.get("supersedes") or [],
                "superseded_by": metadata.get("superseded_by"),
                "sections": extract_sections(doc),
            }
        )

    generated_at = existing_generated_at or current_utc_timestamp()
    payload = {
        "schema_version": "1.0.0",
        "generator": {
            "name": "tools/standards/standards.py",
            "version": GENERATOR_VERSION,
        },
        "generated_at": generated_at,
        "standards": standards,
    }
    return payload, messages


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


def write_index(payload: dict[str, Any]) -> None:
    content = json.dumps(payload, ensure_ascii=False, indent=2) + "\n"
    write_text(INDEX_PATH, content)


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

    generated, build_messages = build_index(existing.get("generated_at"))
    messages.extend(build_messages)
    if has_errors(messages):
        return messages

    if existing != generated:
        existing_text = json.dumps(existing, ensure_ascii=False, indent=2, sort_keys=True).splitlines()
        generated_text = json.dumps(
            generated,
            ensure_ascii=False,
            indent=2,
            sort_keys=True,
        ).splitlines()
        diff = "\n".join(
            difflib.unified_diff(
                existing_text,
                generated_text,
                fromfile="index.generated.json",
                tofile="generated",
                lineterm="",
            )
        )
        hint = "index is out of date; run generate-index"
        if diff:
            hint += "\n" + diff
        messages.append(error("standard-index", INDEX_PATH, hint))

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

    payload, build_messages = build_index()
    emit(build_messages)
    if has_errors(build_messages):
        return 1

    write_index(payload)
    print(f"Generated {rel_path(INDEX_PATH)}")
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
