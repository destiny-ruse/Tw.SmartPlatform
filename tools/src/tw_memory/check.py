from __future__ import annotations

import fnmatch
import subprocess
import sys
from collections import defaultdict
from pathlib import Path

from tw_memory.charter import Charter, load_charter, validate_charter
from tw_memory.cards import render_package_card, render_public_api_card
from tw_memory.generated_io import repo_relative
from tw_memory.hashing import sha256_normalized
from tw_memory.implemented_api import PackageApi, collect_package_api
from tw_memory.packages import DiscoveredPackage, discover_packages
from tw_memory.repo import RepoPaths, find_repo_root
from tw_memory.rules_boundary import find_rule_files, find_rules_boundary_violations
from tw_memory.secret_scan import scan_secrets
from tw_memory.source_index import load_source_index

_FORBIDDEN_TRACKED = (".codegraph", ".tw-memory/runtime")


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

    capabilities = [
        (" ".join(capability.casefold().split()), charter)
        for charter in charters
        for capability in charter.public_capabilities
        if " ".join(capability.casefold().split())
    ]
    for index, (left, left_charter) in enumerate(capabilities):
        for right, right_charter in capabilities[index + 1:]:
            if left_charter.package == right_charter.package or left == right:
                continue
            if _overlaps(left, right):
                packages = ", ".join(sorted((left_charter.package, right_charter.package)))
                errors.append(f"public_capabilities overlap: {packages}")
    return errors


def _overlaps(a: str, b: str) -> bool:
    return a == b or a.startswith(f"{b}.") or b.startswith(f"{a}.")


def _source_index_errors(repo: Path, source_index: Path) -> list[str]:
    data = load_source_index(source_index)
    sources = data.get("sources")
    if not isinstance(sources, dict):
        return [f"{source_index}: sources must be a mapping"]

    errors: list[str] = []
    for source_id, raw_entry in sorted(sources.items()):
        entry = raw_entry if isinstance(raw_entry, dict) else {}
        raw_path = entry.get("path")
        if not isinstance(raw_path, str) or not raw_path:
            errors.append(f"{source_index}: source {source_id} missing path")
            continue

        path = repo / raw_path
        if not path.exists():
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


def _usage_doc_errors(package: DiscoveredPackage, api: PackageApi) -> list[str]:
    if api.usage_docs:
        return []
    package_docs_root = f"docs/shared-packages/{'dotnet' if package.ecosystem == 'dotnet' else package.ecosystem}/{package.canonical_key}"
    return [f"{package_docs_root}: missing shared package usage docs"]


def _generated_text_error(path: Path, expected: str) -> list[str]:
    if not path.exists():
        return [f"{path}: missing generated memory file"]
    actual = path.read_text(encoding="utf-8").replace("\r\n", "\n").replace("\r", "\n")
    if actual != expected:
        return [f"{path}: generated memory stale; run python -m tw_memory generate"]
    return []


def _generated_card_errors(repo: Path, package_charters: list[tuple[DiscoveredPackage, Charter]]) -> list[str]:
    paths = RepoPaths(repo)
    errors: list[str] = []
    expected_paths: set[Path] = set()

    for package, charter in package_charters:
        api = collect_package_api(repo, package)
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


def _package_errors(repo: Path, packages: list[DiscoveredPackage]) -> tuple[list[str], list[tuple[DiscoveredPackage, Charter]]]:
    errors: list[str] = []
    package_charters: list[tuple[DiscoveredPackage, Charter]] = []
    for package in packages:
        if not package.charter_path.exists():
            errors.append(f"{package.root_dir}: missing package-charter.yaml")
            continue

        charter = load_charter(package.charter_path)
        package_charters.append((package, charter))
        errors.extend(validate_charter(charter))
        if charter.package != package.canonical_key:
            errors.append(
                f"{package.charter_path}: package {charter.package!r} does not match {package.canonical_key!r}"
            )
        errors.extend(_dependency_errors(package, charter))
        errors.extend(_charter_secret_errors(charter))
        errors.extend(_usage_doc_errors(package, collect_package_api(repo, package)))
    return errors, package_charters


def run_check(root: str | None = None, *, staged: bool = False) -> int:
    """Validate generated memory gates and source governance facts."""
    repo = Path(root).resolve() if root else find_repo_root()
    paths = RepoPaths(repo)
    errors: list[str] = []

    rule_files = find_rule_files(repo)
    errors.extend(find_rules_boundary_violations(repo, rule_files))

    packages = discover_packages(repo)
    package_errors, package_charters = _package_errors(repo, packages)
    errors.extend(package_errors)
    charters = [charter for _, charter in package_charters]
    errors.extend(_public_capability_errors(charters))
    errors.extend(_generated_card_errors(repo, package_charters))

    if paths.source_index.exists():
        errors.extend(_source_index_errors(repo, paths.source_index))
    elif charters:
        errors.append(f"{repo_relative(repo, paths.source_index)}: missing source-index.generated.json")

    errors.extend(_tracked_errors(repo, staged))

    if errors:
        for error in errors:
            print(error, file=sys.stderr)
        return 1

    print(f"[check] passed for {len(charters)} package charters")
    return 0
