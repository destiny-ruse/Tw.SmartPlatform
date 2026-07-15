from __future__ import annotations

import sys
from pathlib import Path
from typing import Any

import yaml

from tw_memory import GENERATOR_VERSION
from tw_memory.cards import render_package_card, render_public_api_card
from tw_memory.charter import load_charter
from tw_memory.codegraph_routes import default_codegraph_queries
from tw_memory.discovery import discover_contract_files, discover_skill_files
from tw_memory.generated_io import (
    GeneratedPathError,
    generated_memory_safety_errors,
    generated_repo_relative,
    repo_relative,
    unlink_generated_file,
    write_generated_text,
)
from tw_memory.implemented_api import PackageApiError, collect_package_api
from tw_memory.markdown_segments import segment_markdown
from tw_memory.packages import discover_package_inventory
from tw_memory.repo import RepoPaths, find_repo_root
from tw_memory.routes import write_route
from tw_memory.rules_boundary import find_formal_standard_refs, find_rule_files
from tw_memory.source_index import make_source_entry, write_source_index

SCHEMA_VERSION = "1.0.0"


def _generated_safety_errors(
    repo: Path,
    paths: RepoPaths,
    *,
    output_files: set[Path] | None = None,
    deletion_candidates: dict[Path, Path] | None = None,
) -> list[str]:
    """Validate generated output roots and planned file mutations."""
    return generated_memory_safety_errors(
        repo,
        directories=(
            paths.tw_memory,
            paths.cards,
            paths.package_cards,
            paths.public_api_cards,
            paths.routes,
            paths.manifest,
        ),
        output_files=output_files,
        deletion_candidates=deletion_candidates,
    )


def _write_generated_yaml(root: Path, path: Path, data: dict[str, Any]) -> None:
    text = yaml.safe_dump(
        data,
        allow_unicode=True,
        sort_keys=True,
        default_flow_style=False,
    )
    write_generated_text(root, path, text)


def _generated_card_path(root: Path, path: Path) -> str:
    return generated_repo_relative(root, path)


def run_generate(root: str | None = None) -> int:
    """Generate deterministic commit-layer memory files from repository sources."""
    repo = Path(root).resolve() if root else find_repo_root()
    paths = RepoPaths(repo)
    safety_errors = _generated_safety_errors(repo, paths)
    if safety_errors:
        for error in safety_errors:
            print(error, file=sys.stderr)
        return 1

    discovery = discover_package_inventory(repo)
    diagnostics = list(discovery.diagnostics)
    entries: list[dict[str, str]] = []

    rule_files = find_rule_files(repo)
    standard_refs = find_formal_standard_refs(repo, rule_files)
    standards: list[dict[str, object]] = []
    for rel in standard_refs:
        path = repo / rel
        if not path.exists():
            continue
        entries.append(
            make_source_entry(
                repo,
                path,
                "standard",
                rel.replace("/", ":"),
                "engineering-standard-segment:v1",
            )
        )
        standards.extend(segment_markdown(repo, path))

    packages_route: dict[str, object] = {}
    card_contents: dict[Path, str] = {}
    for package in discovery.packages:
        if not package.charter_path.exists():
            diagnostics.append(f"{package.root_dir}: missing package-charter.yaml")
            continue
        try:
            charter = load_charter(package.charter_path)
        except (OSError, UnicodeError, yaml.YAMLError) as error:
            diagnostics.append(f"{package.charter_path}: invalid package charter YAML: {error}")
            continue
        try:
            implemented_api = collect_package_api(repo, package)
        except (OSError, UnicodeError, PackageApiError) as error:
            diagnostics.append(f"{package.project_file}: cannot collect package API: {error}")
            continue
        rel = repo_relative(repo, package.root_dir)
        source_key = f"package-charter:{package.canonical_key}"
        source_ref = f"charter:{source_key}"
        package_card = paths.package_cards / f"{package.canonical_key}.generated.md"
        public_api_card = paths.public_api_cards / f"{package.canonical_key}.generated.md"
        entries.append(make_source_entry(repo, package.charter_path, "charter", source_key, "package-charter:v1"))
        for source_file in implemented_api.source_files:
            source_path = repo_relative(repo, source_file)
            entries.append(
                make_source_entry(
                    repo,
                    source_file,
                    "package-source",
                    f"{package.canonical_key}:{source_path}",
                    "implemented-public-api:v1",
                )
            )
        for package_doc in implemented_api.package_doc_paths:
            doc_path = repo_relative(repo, package_doc)
            entries.append(
                make_source_entry(
                    repo,
                    package_doc,
                    "package-doc",
                    f"{package.canonical_key}:{doc_path}",
                    "shared-package-doc:v1",
                )
            )
        packages_route[package.canonical_key] = {
            "path": rel,
            "card": _generated_card_path(repo, package_card),
            "public_api_card": _generated_card_path(repo, public_api_card),
            "source_refs": [source_ref],
        }
        card_contents[package_card] = render_package_card(
            package.canonical_key,
            rel,
            charter,
            [source_ref],
        )
        card_contents[public_api_card] = render_public_api_card(
            package.canonical_key,
            rel,
            charter,
            [source_ref],
            implemented_api,
        )

    contract_files = discover_contract_files(repo)
    skill_files = discover_skill_files(repo)
    for contract_file in contract_files:
        entries.append(
            make_source_entry(
                repo,
                contract_file,
                "contract",
                repo_relative(repo, contract_file),
                "contract-discovery:v1",
            )
        )
    for skill_file in skill_files:
        entries.append(
            make_source_entry(
                repo,
                skill_file,
                "skill",
                repo_relative(repo, skill_file),
                "skill-discovery:v1",
            )
        )

    if diagnostics:
        for error in sorted(diagnostics):
            print(error, file=sys.stderr)
        return 1

    expected_cards = set(card_contents)
    deletion_candidates: dict[Path, Path] = {}
    for cards_root in (paths.package_cards, paths.public_api_cards):
        if not cards_root.exists():
            continue
        for card in cards_root.glob("*.generated.md"):
            if card not in expected_cards:
                deletion_candidates[card] = cards_root

    output_files = {
        paths.standards_route,
        paths.packages_route,
        paths.skills_route,
        paths.services_route,
        paths.apis_route,
        paths.frontend_route,
        paths.codegraph_queries_route,
        paths.taxonomy,
        paths.source_index,
        *expected_cards,
    }
    safety_errors = _generated_safety_errors(
        repo,
        paths,
        output_files=output_files,
        deletion_candidates=deletion_candidates,
    )
    if safety_errors:
        for error in safety_errors:
            print(error, file=sys.stderr)
        return 1

    try:
        write_route(repo, paths.standards_route, SCHEMA_VERSION, {"standards": standards})
        for card, content in sorted(card_contents.items()):
            write_generated_text(repo, card, content)
        for card, authority in sorted(deletion_candidates.items()):
            unlink_generated_file(repo, card, authority)
        write_route(repo, paths.packages_route, SCHEMA_VERSION, {"packages": packages_route})
        write_route(
            repo,
            paths.skills_route,
            SCHEMA_VERSION,
            {"skills": [repo_relative(repo, path) for path in skill_files]},
        )
        write_route(repo, paths.services_route, SCHEMA_VERSION, {"services": {}})
        write_route(
            repo,
            paths.apis_route,
            SCHEMA_VERSION,
            {"apis": [repo_relative(repo, path) for path in contract_files]},
        )
        write_route(repo, paths.frontend_route, SCHEMA_VERSION, {"frontend": {}})
        write_route(repo, paths.codegraph_queries_route, SCHEMA_VERSION, default_codegraph_queries())
        _write_generated_yaml(
            repo,
            paths.taxonomy,
            {
                "schema_version": SCHEMA_VERSION,
                "generator": GENERATOR_VERSION,
                "source_types": [
                    "standard",
                    "charter",
                    "package-source",
                    "package-doc",
                    "contract",
                    "structure",
                    "skill",
                ],
                "memory_types": [
                    "package-summary",
                    "public-api-summary",
                    "service-summary",
                    "api-summary",
                    "frontend-summary",
                ],
                "lookup_keys": ["package", "service", "api", "frontend-app", "symbol"],
            },
        )
        write_source_index(repo, paths.source_index, entries)
    except GeneratedPathError as error:
        print(error, file=sys.stderr)
        return 1
    print(f"[generate] wrote .tw-memory for {len(packages_route)} packages and {len(standards)} standard segments")
    return 0
