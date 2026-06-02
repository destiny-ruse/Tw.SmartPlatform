from __future__ import annotations

from pathlib import Path
from typing import Any

import yaml

from tw_memory import GENERATOR_VERSION
from tw_memory.cards import render_package_card, render_public_api_card
from tw_memory.charter import load_charter
from tw_memory.codegraph_routes import default_codegraph_queries
from tw_memory.discovery import discover_contract_files, discover_skill_files
from tw_memory.generated_io import repo_relative, write_generated_text
from tw_memory.markdown_segments import segment_markdown
from tw_memory.packages import discover_packages
from tw_memory.repo import RepoPaths, find_repo_root
from tw_memory.routes import write_route
from tw_memory.rules_boundary import find_formal_standard_refs, find_rule_files
from tw_memory.source_index import make_source_entry, write_source_index

SCHEMA_VERSION = "1.0.0"


def _write_generated_yaml(root: Path, path: Path, data: dict[str, Any]) -> None:
    text = yaml.safe_dump(
        data,
        allow_unicode=True,
        sort_keys=True,
        default_flow_style=False,
    )
    write_generated_text(root, path, text)


def _generated_card_path(root: Path, path: Path) -> str:
    return repo_relative(root, path)


def run_generate(root: str | None = None) -> int:
    """Generate deterministic commit-layer memory files from repository sources."""
    repo = Path(root).resolve() if root else find_repo_root()
    paths = RepoPaths(repo)
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
    write_route(paths.standards_route, SCHEMA_VERSION, {"standards": standards})

    packages_route: dict[str, object] = {}
    for package in discover_packages(repo):
        if not package.charter_path.exists():
            continue
        charter = load_charter(package.charter_path)
        rel = repo_relative(repo, package.root_dir)
        source_key = f"package-charter:{package.canonical_key}"
        source_ref = f"charter:{source_key}"
        package_card = paths.package_cards / f"{package.canonical_key}.generated.md"
        public_api_card = paths.public_api_cards / f"{package.canonical_key}.generated.md"
        entries.append(make_source_entry(repo, package.charter_path, "charter", source_key, "package-charter:v1"))
        packages_route[package.canonical_key] = {
            "path": rel,
            "card": _generated_card_path(repo, package_card),
            "public_api_card": _generated_card_path(repo, public_api_card),
            "source_refs": [source_ref],
        }
        write_generated_text(
            repo,
            package_card,
            render_package_card(package.canonical_key, rel, charter, [source_ref]),
        )
        write_generated_text(
            repo,
            public_api_card,
            render_public_api_card(package.canonical_key, rel, charter, [source_ref]),
        )

    write_route(paths.packages_route, SCHEMA_VERSION, {"packages": packages_route})
    write_route(paths.skills_route, SCHEMA_VERSION, {"skills": [repo_relative(repo, path) for path in discover_skill_files(repo)]})
    write_route(paths.services_route, SCHEMA_VERSION, {"services": {}})
    write_route(paths.apis_route, SCHEMA_VERSION, {"apis": [repo_relative(repo, path) for path in discover_contract_files(repo)]})
    write_route(paths.frontend_route, SCHEMA_VERSION, {"frontend": {}})
    write_route(paths.codegraph_queries_route, SCHEMA_VERSION, default_codegraph_queries())
    _write_generated_yaml(
        repo,
        paths.taxonomy,
        {
            "schema_version": SCHEMA_VERSION,
            "generator": GENERATOR_VERSION,
            "source_types": ["standard", "charter", "contract", "structure", "skill"],
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
    for contract_file in discover_contract_files(repo):
        entries.append(
            make_source_entry(
                repo,
                contract_file,
                "contract",
                repo_relative(repo, contract_file),
                "contract-discovery:v1",
            )
        )
    for skill_file in discover_skill_files(repo):
        entries.append(
            make_source_entry(
                repo,
                skill_file,
                "skill",
                repo_relative(repo, skill_file),
                "skill-discovery:v1",
            )
        )
    write_source_index(paths.source_index, entries)
    print(f"[generate] wrote .tw-memory for {len(packages_route)} packages and {len(standards)} standard segments")
    return 0
