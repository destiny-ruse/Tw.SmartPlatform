from __future__ import annotations

import fnmatch
import json
import re
import subprocess
import sys
from collections import defaultdict
from pathlib import Path, PurePosixPath

import yaml

from tw_memory.charter import Charter, load_charter, validate_charter
from tw_memory.cards import render_package_card, render_public_api_card
from tw_memory.discovery import discover_contract_files, discover_skill_files
from tw_memory.generated_io import (
    generated_memory_safety_errors,
    generated_repo_relative,
    repo_relative,
)
from tw_memory.hashing import sha256_normalized
from tw_memory.implemented_api import PackageApi, PackageApiError, collect_package_api
from tw_memory.packages import DiscoveredPackage, discover_package_inventory
from tw_memory.repo import RepoPaths, find_repo_root
from tw_memory.rules_boundary import find_formal_standard_refs, find_rule_files, find_rules_boundary_violations
from tw_memory.secret_scan import scan_secrets
from tw_memory.source_index import load_source_index
from tw_memory.yaml_io import load_yaml

_FORBIDDEN_TRACKED = (".codegraph", ".tw-memory/runtime")
_MARKDOWN_LINK = re.compile(r"\[[^\]]*\]\(([^)]+)\)")
_DOTNET_MIGRATION_DOCS = frozenset(
    {
        "2026-07-building-blocks-adoption-baseline.md",
        "2026-07-building-blocks-consolidation.md",
    }
)


def _matches_any(value: str, patterns: list[str]) -> bool:
    return any(fnmatch.fnmatch(value, pattern) for pattern in patterns)


def _dependency_errors(package: DiscoveredPackage, charter: Charter) -> list[str]:
    errors: list[str] = []
    rules = charter.dependency_rules
    for dependency in package.dependencies:
        if _matches_any(dependency, rules.forbid):
            errors.append(f"{package.project_file}: dependency {dependency} is forbidden by charter")
        if rules.allow and not _matches_any(dependency, rules.allow):
            errors.append(f"{package.project_file}: dependency {dependency} is not allowed by charter")
    return errors


def _charter_secret_errors(charter: Charter) -> list[str]:
    text = charter.path.read_text(encoding="utf-8")
    return [
        f"{charter.path}: secret hit {hit.kind}"
        for hit in scan_secrets(text)
    ]


def _public_capability_errors(charters: list[Charter]) -> list[str]:
    owners_by_capability: dict[str, list[tuple[str, Charter]]] = defaultdict(list)
    for charter in charters:
        for capability in charter.public_capabilities:
            key = " ".join(capability.casefold().split())
            if key:
                owners_by_capability[key].append((key, charter))

    errors: list[str] = []
    for owners in owners_by_capability.values():
        if len(owners) < 2:
            continue
        packages = ", ".join(sorted(charter.package for _, charter in owners))
        errors.append(f"public_capabilities overlap: {packages}")

    return errors


def _source_index_errors(
    repo: Path,
    source_index: Path,
    package_charters: list[tuple[DiscoveredPackage, Charter]],
    package_apis: dict[Path, PackageApi],
) -> list[str]:
    try:
        data = load_source_index(source_index)
    except (json.JSONDecodeError, OSError, UnicodeError) as error:
        return [f"{source_index}: invalid source index JSON: {error}"]
    sources = data.get("sources")
    if not isinstance(sources, dict):
        return [f"{source_index}: sources must be a mapping"]

    expected_sources: dict[str, dict[str, str]] = {}

    def register_source(source_type: str, key: str, path: str, extractor: str) -> None:
        source_id = f"{source_type}:{key}"
        expected_sources[source_id] = {
            "source_id": source_id,
            "source_type": source_type,
            "path": path,
            "hash_algorithm": "sha256",
            "extractor": extractor,
        }

    rule_files = find_rule_files(repo)
    for rel in find_formal_standard_refs(repo, rule_files):
        path = repo / rel
        if path.exists():
            register_source("standard", rel.replace("/", ":"), rel, "engineering-standard-segment:v1")
    for package, _ in package_charters:
        register_source(
            "charter",
            f"package-charter:{package.canonical_key}",
            repo_relative(repo, package.charter_path),
            "package-charter:v1",
        )
        api = package_apis.get(package.project_file)
        if api is None:
            continue
        for source_file in api.source_files:
            source_path = repo_relative(repo, source_file)
            register_source(
                "package-source",
                f"{package.canonical_key}:{source_path}",
                source_path,
                "implemented-public-api:v1",
            )
        for package_doc in api.package_doc_paths:
            doc_path = repo_relative(repo, package_doc)
            register_source(
                "package-doc",
                f"{package.canonical_key}:{doc_path}",
                doc_path,
                "shared-package-doc:v1",
            )
    for contract_file in discover_contract_files(repo):
        contract_path = repo_relative(repo, contract_file)
        register_source("contract", contract_path, contract_path, "contract-discovery:v1")
    for skill_file in discover_skill_files(repo):
        skill_path = repo_relative(repo, skill_file)
        register_source("skill", skill_path, skill_path, "skill-discovery:v1")

    errors = _parity_errors(
        source_index,
        "governed source",
        set(expected_sources),
        {str(source_id) for source_id in sources},
    )
    repo_resolved = repo.resolve()
    for source_id, raw_entry in sorted(sources.items()):
        if not isinstance(raw_entry, dict):
            errors.append(f"{source_index}: source {source_id} entry must be a mapping")
            continue
        entry = raw_entry
        raw_path = entry.get("path")
        if not isinstance(raw_path, str) or not raw_path:
            errors.append(f"{source_index}: source {source_id} missing path")
            continue

        path = (repo / raw_path).resolve()
        try:
            path.relative_to(repo_resolved)
        except ValueError:
            errors.append(f"{source_index}: source {source_id} path escapes repository: {raw_path}")
            continue

        expected_metadata = expected_sources.get(str(source_id))
        expected_path = expected_metadata.get("path") if expected_metadata is not None else None
        if expected_metadata is not None:
            for field in ("source_id", "source_type", "hash_algorithm", "extractor"):
                expected_value = expected_metadata[field]
                if entry.get(field) != expected_value:
                    errors.append(
                        f"{source_index}: source {source_id} metadata {field} "
                        f"must be {expected_value!r}, got {entry.get(field)!r}"
                    )
        if expected_path is not None and raw_path != expected_path:
            errors.append(
                f"{source_index}: source {source_id} path {raw_path} "
                f"does not match expected path {expected_path}"
            )
        if not path.is_file():
            errors.append(f"{source_index}: source {source_id} missing {raw_path}")
            continue

        expected_hash = entry.get("sha256")
        if not isinstance(expected_hash, str) or expected_hash != sha256_normalized(path):
            errors.append(f"{source_index}: source {source_id} hash stale")
    return errors


def _git_paths(repo: Path, args: list[str]) -> list[str]:
    result = subprocess.run(
        ["git", "-C", str(repo), *args],
        check=False,
        capture_output=True,
        text=True,
    )
    if result.returncode != 0:
        return []
    return [line.strip().replace("\\", "/") for line in result.stdout.splitlines() if line.strip()]


def _tracked(repo: Path, staged: bool) -> list[str]:
    tracked = set(_git_paths(repo, ["ls-files"]))
    if staged:
        staged_deletions = set(_git_paths(repo, ["diff", "--cached", "--name-only", "--diff-filter=D"]))
        tracked.difference_update(staged_deletions)
        tracked.update(_git_paths(repo, ["diff", "--cached", "--name-only", "--diff-filter=d"]))
    return sorted(tracked)


def _tracked_errors(repo: Path, staged: bool) -> list[str]:
    errors: list[str] = []
    for path in _tracked(repo, staged):
        for forbidden in _FORBIDDEN_TRACKED:
            if path == forbidden or path.startswith(f"{forbidden}/"):
                errors.append(f"{path}: runtime/local index path must not be tracked or staged")
                break
    return errors


def _package_doc_errors(package: DiscoveredPackage, api: PackageApi) -> list[str]:
    if api.package_docs:
        return []
    package_docs_root = f"docs/shared-packages/{'dotnet' if package.ecosystem == 'dotnet' else package.ecosystem}/{package.canonical_key}"
    return [f"{package_docs_root}: missing shared package reference docs"]


def _generated_text_error(path: Path, expected: str) -> list[str]:
    if not path.exists():
        return [f"{path}: missing generated memory file"]
    actual = path.read_text(encoding="utf-8").replace("\r\n", "\n").replace("\r", "\n")
    if actual != expected:
        return [f"{path}: generated memory stale; run python -m tw_memory generate"]
    return []


def _generated_card_errors(
    repo: Path,
    package_charters: list[tuple[DiscoveredPackage, Charter]],
    package_apis: dict[Path, PackageApi],
) -> list[str]:
    paths = RepoPaths(repo)
    errors: list[str] = []
    expected_paths: set[Path] = set()

    for package, charter in package_charters:
        api = package_apis.get(package.project_file)
        if api is None:
            continue
        rel = repo_relative(repo, package.root_dir)
        source_ref = f"charter:package-charter:{package.canonical_key}"
        package_card = paths.package_cards / f"{package.canonical_key}.generated.md"
        public_api_card = paths.public_api_cards / f"{package.canonical_key}.generated.md"
        expected_paths.update((package_card, public_api_card))
        errors.extend(
            _generated_text_error(
                package_card,
                render_package_card(package.canonical_key, rel, charter, [source_ref]),
            )
        )
        errors.extend(
            _generated_text_error(
                public_api_card,
                render_public_api_card(package.canonical_key, rel, charter, [source_ref], api),
            )
        )

    for cards_root in (paths.package_cards, paths.public_api_cards):
        if not cards_root.exists():
            continue
        for card in sorted(cards_root.glob("*.generated.md")):
            if card not in expected_paths:
                errors.append(f"{card}: orphan generated memory card")
    return errors


def _topology_errors(repo: Path, packages: list[DiscoveredPackage]) -> list[str]:
    paths = RepoPaths(repo)
    governed_dotnet = [package for package in packages if package.ecosystem == "dotnet"]
    governed_roots_exist = paths.dotnet_packages_root.exists() or paths.dotnet_tools_root.exists()
    if not paths.dotnet_topology.exists():
        return [f"{paths.dotnet_topology}: missing approved .NET package topology"] if governed_roots_exist else []

    try:
        topology = json.loads(paths.dotnet_topology.read_text(encoding="utf-8"))
    except (json.JSONDecodeError, OSError, UnicodeError) as error:
        return [f"{paths.dotnet_topology}: invalid topology JSON: {error}"]
    if not isinstance(topology, dict):
        return [f"{paths.dotnet_topology}: topology must be a mapping"]

    runtime_entries = topology.get("runtimeProjects")
    tool_entries = topology.get("toolProjects")
    retired_entries = topology.get("retiredPackages")
    errors: list[str] = []
    if not isinstance(runtime_entries, list):
        errors.append(f"{paths.dotnet_topology}: runtimeProjects must be a list")
        runtime_entries = []
    if not isinstance(tool_entries, list):
        errors.append(f"{paths.dotnet_topology}: toolProjects must be a list")
        tool_entries = []
    if not isinstance(retired_entries, list):
        errors.append(f"{paths.dotnet_topology}: retiredPackages must be a list")
        retired_entries = []

    runtime_paths: list[str] = []
    tool_paths: list[str] = []
    seen_runtime_paths: dict[str, str] = {}
    seen_tool_paths: dict[str, str] = {}
    approved_keys: dict[str, tuple[str, str]] = {}
    approved_repo_paths: dict[str, tuple[str, str]] = {}

    def register_approved_project(kind: str, path: str, repo_path: str) -> None:
        canonical_key = PurePosixPath(path).stem
        folded_key = canonical_key.casefold()
        previous_key = approved_keys.get(folded_key)
        if previous_key is not None and previous_key != (kind, path):
            errors.append(
                f"{paths.dotnet_topology}: duplicate approved canonical key {canonical_key}: "
                f"{previous_key[0]} {previous_key[1]}, {kind} {path}"
            )
        else:
            approved_keys[folded_key] = (kind, path)

        folded_repo_path = repo_path.casefold()
        previous_path = approved_repo_paths.get(folded_repo_path)
        if previous_path is not None and previous_path != (kind, path):
            errors.append(
                f"{paths.dotnet_topology}: duplicate project path across runtime/tool {repo_path}: "
                f"{previous_path[0]} {previous_path[1]}, {kind} {path}"
            )
        else:
            approved_repo_paths[folded_repo_path] = (kind, path)

    for index, entry in enumerate(runtime_entries):
        if not isinstance(entry, dict):
            errors.append(f"{paths.dotnet_topology}: runtimeProjects[{index}] must be a mapping")
            continue
        raw_path = entry.get("path")
        if not isinstance(raw_path, str) or not raw_path:
            errors.append(f"{paths.dotnet_topology}: runtimeProjects[{index}] missing path")
            continue
        project_path = raw_path.replace("\\", "/")
        parts = PurePosixPath(project_path).parts
        if (
            len(parts) != 3
            or any(part in {".", ".."} for part in parts)
            or PurePosixPath(project_path).suffix.casefold() != ".csproj"
            or parts[-2] != PurePosixPath(project_path).stem
        ):
            errors.append(
                f"{paths.dotnet_topology}: invalid runtime project path {project_path}; "
                "expected Capability/Package/Package.csproj"
            )
            continue
        runtime_paths.append(project_path)
        folded_path = project_path.casefold()
        if folded_path in seen_runtime_paths:
            errors.append(f"{paths.dotnet_topology}: duplicate runtime project path {project_path}")
            continue
        seen_runtime_paths[folded_path] = project_path
        register_approved_project(
            "runtime",
            project_path,
            f"backend/dotnet/BuildingBlocks/src/{project_path}",
        )

    for index, entry in enumerate(tool_entries):
        if not isinstance(entry, str) or not entry:
            errors.append(f"{paths.dotnet_topology}: toolProjects[{index}] must be a non-empty path")
            continue
        project_path = entry.replace("\\", "/")
        parts = PurePosixPath(project_path).parts
        if (
            len(parts) != 6
            or parts[:4] != ("backend", "dotnet", "tools", "src")
            or any(part in {".", ".."} for part in parts)
            or PurePosixPath(project_path).suffix.casefold() != ".csproj"
            or parts[-2] != PurePosixPath(project_path).stem
        ):
            errors.append(
                f"{paths.dotnet_topology}: invalid tool project path {project_path}; "
                "expected backend/dotnet/tools/src/Tool/Tool.csproj"
            )
            continue
        tool_paths.append(project_path)
        folded_path = project_path.casefold()
        if folded_path in seen_tool_paths:
            errors.append(f"{paths.dotnet_topology}: duplicate tool project path {project_path}")
            continue
        seen_tool_paths[folded_path] = project_path
        register_approved_project("tool", project_path, project_path)

    expected_runtime = set(runtime_paths)
    expected_tools = set(tool_paths)
    actual_runtime = {
        package.project_file.relative_to(paths.dotnet_packages_root).as_posix()
        for package in governed_dotnet
        if package.source_kind == "building-block"
    }
    actual_tools = {
        repo_relative(repo, package.project_file)
        for package in governed_dotnet
        if package.source_kind == "tool"
    }
    for project_path in sorted(expected_runtime - actual_runtime):
        errors.append(f"{paths.dotnet_topology}: approved runtime project missing {project_path}")
    for project_path in sorted(actual_runtime - expected_runtime):
        errors.append(f"{paths.dotnet_topology}: discovered runtime project is not approved {project_path}")
    for project_path in sorted(expected_tools - actual_tools):
        errors.append(f"{paths.dotnet_topology}: approved tool project missing {project_path}")
    for project_path in sorted(actual_tools - expected_tools):
        errors.append(f"{paths.dotnet_topology}: discovered tool project is not approved {project_path}")

    retired_ids: set[str] = set()
    for index, entry in enumerate(retired_entries):
        if not isinstance(entry, dict):
            errors.append(f"{paths.dotnet_topology}: retiredPackages[{index}] must be a mapping")
            continue
        if "packageId" not in entry:
            errors.append(f"{paths.dotnet_topology}: retiredPackages[{index}] missing packageId")
            continue
        package_id = entry.get("packageId")
        if not isinstance(package_id, str) or not package_id.strip():
            errors.append(
                f"{paths.dotnet_topology}: retiredPackages[{index}] packageId must be a non-empty string"
            )
            continue
        retired_ids.add(package_id.casefold())
    for package in governed_dotnet:
        if package.canonical_key.casefold() in retired_ids:
            errors.append(f"{package.project_file}: retired PackageId {package.canonical_key} is forbidden")
    return errors


def _markdown_links(path: Path) -> set[str]:
    if not path.exists():
        return set()
    links: set[str] = set()
    for match in _MARKDOWN_LINK.finditer(path.read_text(encoding="utf-8")):
        raw_target = match.group(1).strip().split(maxsplit=1)[0].strip("<>")
        target = raw_target.split("#", 1)[0].replace("\\", "/")
        if target and not target.startswith(("http://", "https://", "mailto:")):
            links.add(target)
    return links


def _docs_errors(
    repo: Path,
    packages: list[DiscoveredPackage],
    package_apis: dict[Path, PackageApi],
) -> list[str]:
    paths = RepoPaths(repo)
    dotnet_packages = [package for package in packages if package.ecosystem == "dotnet"]
    if not dotnet_packages and not paths.dotnet_package_docs_root.exists():
        return []
    if any(package.project_file not in package_apis for package in dotnet_packages):
        return []

    errors: list[str] = []
    package_keys = {package.canonical_key for package in dotnet_packages}
    docs_root = paths.dotnet_package_docs_root
    for child in sorted(docs_root.iterdir()) if docs_root.exists() else []:
        if child.is_file():
            if child.name != "README.md":
                errors.append(f"{child}: orphan .NET shared-package documentation file")
            continue
        if child.is_dir() and (child.name == "migrations" or child.name in package_keys):
            continue
        if child.is_dir():
            errors.append(f"{child}: orphan .NET shared-package documentation directory")
        else:
            errors.append(f"{child}: orphan .NET shared-package documentation entry")

    top_index = paths.shared_package_docs_root / "README.md"
    dotnet_index = docs_root / "README.md"
    expected_top_links = {f"dotnet/{key}/README.md" for key in package_keys}
    actual_top_links = {
        link
        for link in _markdown_links(top_index)
        if link.startswith("dotnet/") and link.endswith("/README.md") and link != "dotnet/README.md"
    }
    expected_dotnet_links = {f"{key}/README.md" for key in package_keys}
    dotnet_links = _markdown_links(dotnet_index)
    actual_dotnet_links = {
        link
        for link in dotnet_links
        if link.endswith("/README.md") and not link.startswith("../")
    }
    errors.extend(_parity_errors(top_index, "package index links", expected_top_links, actual_top_links))
    errors.extend(_parity_errors(dotnet_index, "package index links", expected_dotnet_links, actual_dotnet_links))

    migrations_root = docs_root / "migrations"
    if migrations_root.exists():
        for path in sorted(migrations_root.iterdir()):
            if not path.is_file() or path.name not in _DOTNET_MIGRATION_DOCS:
                errors.append(f"{path}: orphan .NET shared-package migration document")
    for name in sorted(_DOTNET_MIGRATION_DOCS):
        required_doc = migrations_root / name
        if not required_doc.is_file():
            errors.append(f"{required_doc}: missing required .NET shared-package migration document")
    required_migration_links = {f"migrations/{name}" for name in _DOTNET_MIGRATION_DOCS}
    indexed_migrations = {link for link in dotnet_links if link.startswith("migrations/") and link.endswith(".md")}
    errors.extend(_parity_errors(dotnet_index, "migration links", required_migration_links, indexed_migrations))

    for package in dotnet_packages:
        package_docs_root = docs_root / package.canonical_key
        readme = package_docs_root / "README.md"
        if not readme.exists():
            errors.append(f"{readme}: missing shared package README.md")
            continue
        package_docs = {
            path.relative_to(package_docs_root).as_posix()
            for path in package_docs_root.rglob("*.md")
            if path.is_file() and path != readme
        }
        linked_package_docs: set[str] = set()
        for link in _markdown_links(readme):
            linked_path = (readme.parent / link).resolve()
            try:
                relative = linked_path.relative_to(package_docs_root.resolve()).as_posix()
            except ValueError:
                continue
            if relative != "README.md" and relative.endswith(".md"):
                linked_package_docs.add(relative)
        errors.extend(_parity_errors(readme, "local package-doc links", package_docs, linked_package_docs))
    return errors


def _parity_errors(path: Path, label: str, expected: set[str], actual: set[str]) -> list[str]:
    errors = [f"{path}: missing {label} entry {value}" for value in sorted(expected - actual)]
    errors.extend(f"{path}: orphan {label} entry {value}" for value in sorted(actual - expected))
    return errors


def _package_route_errors(repo: Path, packages: list[DiscoveredPackage]) -> list[str]:
    paths = RepoPaths(repo)
    if not paths.packages_route.exists():
        return [f"{paths.packages_route}: missing package route"] if packages else []
    try:
        data = load_yaml(paths.packages_route)
    except (OSError, UnicodeError, yaml.YAMLError) as error:
        return [f"{paths.packages_route}: invalid package route YAML: {error}"]
    routes = data.get("packages") if isinstance(data, dict) else None
    if not isinstance(routes, dict):
        return [f"{paths.packages_route}: packages must be a mapping"]

    expected: dict[str, dict[str, object]] = {}
    for package in packages:
        rel = repo_relative(repo, package.root_dir)
        expected[package.canonical_key] = {
            "path": rel,
            "card": generated_repo_relative(
                repo,
                paths.package_cards / f"{package.canonical_key}.generated.md",
            ),
            "public_api_card": generated_repo_relative(
                repo,
                paths.public_api_cards / f"{package.canonical_key}.generated.md",
            ),
            "source_refs": [f"charter:package-charter:{package.canonical_key}"],
        }
    errors = _parity_errors(paths.packages_route, "package route", set(expected), set(routes))
    for key in sorted(set(expected) & set(routes)):
        if routes[key] != expected[key]:
            errors.append(f"{paths.packages_route}: stale package route {key}")
    return errors


def _package_errors(
    repo: Path,
    packages: list[DiscoveredPackage],
    package_apis: dict[Path, PackageApi],
) -> tuple[list[str], list[tuple[DiscoveredPackage, Charter]]]:
    errors: list[str] = []
    package_charters: list[tuple[DiscoveredPackage, Charter]] = []
    for package in packages:
        if not package.charter_path.exists():
            errors.append(f"{package.root_dir}: missing package-charter.yaml")
            continue

        try:
            charter = load_charter(package.charter_path)
        except (OSError, UnicodeError, yaml.YAMLError) as error:
            errors.append(f"{package.charter_path}: invalid package charter YAML: {error}")
            continue
        package_charters.append((package, charter))
        errors.extend(validate_charter(charter))
        if charter.package != package.canonical_key:
            errors.append(
                f"{package.charter_path}: package {charter.package!r} does not match {package.canonical_key!r}"
            )
        errors.extend(_dependency_errors(package, charter))
        errors.extend(_charter_secret_errors(charter))
        api = package_apis.get(package.project_file)
        if api is not None:
            errors.extend(_package_doc_errors(package, api))
    return errors, package_charters


def run_check(root: str | None = None, *, staged: bool = False) -> int:
    """Validate generated memory gates and source governance facts."""
    repo = Path(root).resolve() if root else find_repo_root()
    paths = RepoPaths(repo)
    errors: list[str] = []

    card_safety_errors = generated_memory_safety_errors(
        repo,
        directories=(
            paths.tw_memory,
            paths.cards,
            paths.package_cards,
            paths.public_api_cards,
        ),
    )
    route_safety_errors = generated_memory_safety_errors(
        repo,
        directories=(paths.tw_memory, paths.routes),
        output_files={paths.packages_route},
    )
    source_index_safety_errors = generated_memory_safety_errors(
        repo,
        directories=(paths.tw_memory, paths.manifest),
        output_files={paths.source_index},
    )
    if not card_safety_errors:
        card_files = {
            card
            for cards_root in (paths.package_cards, paths.public_api_cards)
            if cards_root.exists()
            for card in cards_root.glob("*.generated.md")
        }
        card_safety_errors.extend(
            generated_memory_safety_errors(
                repo,
                directories=(paths.package_cards, paths.public_api_cards),
                output_files=card_files,
            )
        )
    for error in (*card_safety_errors, *route_safety_errors, *source_index_safety_errors):
        if error not in errors:
            errors.append(error)

    rule_files = find_rule_files(repo)
    errors.extend(find_rules_boundary_violations(repo, rule_files))

    discovery = discover_package_inventory(repo)
    packages = discovery.packages
    errors.extend(discovery.diagnostics)
    package_apis: dict[Path, PackageApi] = {}
    for package in packages:
        try:
            package_apis[package.project_file] = collect_package_api(repo, package)
        except (OSError, UnicodeError, PackageApiError) as error:
            errors.append(f"{package.project_file}: cannot collect package API: {error}")
    errors.extend(_topology_errors(repo, packages))
    errors.extend(_docs_errors(repo, packages, package_apis))
    package_errors, package_charters = _package_errors(repo, packages, package_apis)
    errors.extend(package_errors)
    charters = [charter for _, charter in package_charters]
    errors.extend(_public_capability_errors(charters))
    if not card_safety_errors:
        errors.extend(_generated_card_errors(repo, package_charters, package_apis))
    if not route_safety_errors:
        errors.extend(_package_route_errors(repo, packages))

    if not source_index_safety_errors and paths.source_index.exists():
        errors.extend(_source_index_errors(repo, paths.source_index, package_charters, package_apis))
    elif not source_index_safety_errors and charters:
        errors.append(f"{generated_repo_relative(repo, paths.source_index)}: missing source-index.generated.json")

    errors.extend(_tracked_errors(repo, staged))

    if errors:
        for error in errors:
            print(error, file=sys.stderr)
        return 1

    print(f"[check] passed for {len(charters)} package charters")
    return 0
