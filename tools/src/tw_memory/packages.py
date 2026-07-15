from __future__ import annotations

import json
import stat
from dataclasses import dataclass
from pathlib import Path
from typing import Any
from xml.etree import ElementTree

from tw_memory.repo import RepoPaths


@dataclass(frozen=True)
class DiscoveredPackage:
    """Shared package discovered from repository package roots."""

    canonical_key: str
    ecosystem: str
    root_dir: Path
    project_file: Path
    charter_path: Path
    dependencies: list[str]
    source_kind: str


@dataclass(frozen=True)
class PackageDiscovery:
    """Governed packages and diagnostics produced by strict discovery."""

    packages: list[DiscoveredPackage]
    diagnostics: list[str]


def _parse_package_refs(csproj: Path) -> list[str]:
    root = ElementTree.fromstring(csproj.read_text(encoding="utf-8"))
    refs: list[str] = []
    for item in root.iter():
        tag = item.tag.rsplit("}", 1)[-1]
        if tag not in {"PackageReference", "ProjectReference"}:
            continue
        include = item.attrib.get("Include")
        if include:
            refs.append(Path(include).stem if tag == "ProjectReference" else include)
    return sorted(refs)


def discover_packages(root: Path) -> list[DiscoveredPackage]:
    """Discover all shared packages in canonical order."""
    return discover_package_inventory(root).packages


def discover_package_inventory(root: Path) -> PackageDiscovery:
    """Discover governed packages while preserving invalid-shape diagnostics."""
    dotnet_packages, diagnostics = _discover_dotnet_inventory(root)
    packages = [*dotnet_packages, *discover_frontend_packages(root)]
    packages.sort(key=lambda package: (package.canonical_key.casefold(), package.project_file.as_posix()))
    return PackageDiscovery(packages=packages, diagnostics=sorted(diagnostics))


def discover_dotnet_packages(root: Path) -> list[DiscoveredPackage]:
    """Discover valid governed .NET packages and packageable tools."""
    packages, _ = _discover_dotnet_inventory(root)
    return packages


def _discover_dotnet_inventory(root: Path) -> tuple[list[DiscoveredPackage], list[str]]:
    paths = RepoPaths(root)
    packages: list[DiscoveredPackage] = []
    diagnostics: list[str] = []
    packages.extend(
        _discover_governed_root(
            paths.dotnet_packages_root,
            repository_root=root.resolve(),
            expected_parts=3,
            source_kind="building-block",
            expected_shape="Capability/Package/Package.csproj",
            diagnostics=diagnostics,
        )
    )
    packages.extend(
        _discover_governed_root(
            paths.dotnet_tools_root,
            repository_root=root.resolve(),
            expected_parts=2,
            source_kind="tool",
            expected_shape="Tool/Tool.csproj",
            diagnostics=diagnostics,
        )
    )

    packages_by_key: dict[str, list[DiscoveredPackage]] = {}
    for package in packages:
        packages_by_key.setdefault(package.canonical_key.casefold(), []).append(package)
    for duplicates in packages_by_key.values():
        if len(duplicates) < 2:
            continue
        canonical_key = duplicates[0].canonical_key
        projects = ", ".join(str(package.project_file) for package in duplicates)
        diagnostics.append(f"duplicate canonical package key {canonical_key!r}: {projects}")

    packages.sort(key=lambda package: (package.canonical_key.casefold(), package.project_file.as_posix()))
    return packages, diagnostics


def _discover_governed_root(
    packages_root: Path,
    *,
    repository_root: Path,
    expected_parts: int,
    source_kind: str,
    expected_shape: str,
    diagnostics: list[str],
) -> list[DiscoveredPackage]:
    if not packages_root.exists():
        return []

    packages: list[DiscoveredPackage] = []
    try:
        governed_root = packages_root.resolve()
        governed_root.relative_to(repository_root)
    except (OSError, RuntimeError, ValueError) as error:
        diagnostics.append(f"{packages_root}: governed root escapes repository {repository_root}: {error}")
        return packages
    for candidate in sorted(packages_root.rglob("*")):
        try:
            metadata = candidate.lstat()
            attributes = getattr(metadata, "st_file_attributes", 0)
            reparse_flag = getattr(stat, "FILE_ATTRIBUTE_REPARSE_POINT", 0x400)
            is_reparse = stat.S_ISLNK(metadata.st_mode) or bool(attributes & reparse_flag)
            if not is_reparse or not candidate.is_dir():
                continue
            candidate.resolve().relative_to(governed_root)
        except ValueError:
            diagnostics.append(
                f"{candidate}: governed directory escapes governed root {packages_root}"
            )
        except (OSError, RuntimeError) as error:
            diagnostics.append(f"{candidate}: cannot validate governed directory boundary: {error}")
    for csproj in sorted(packages_root.rglob("*.csproj")):
        relative = csproj.relative_to(packages_root)
        if _is_ignored_dotnet_candidate(relative, source_kind):
            continue
        try:
            csproj.resolve().relative_to(governed_root)
        except (OSError, RuntimeError, ValueError):
            diagnostics.append(f"{csproj}: governed project escapes governed root {packages_root}")
            continue
        if len(relative.parts) != expected_parts:
            diagnostics.append(f"{csproj}: invalid governed project shape; expected {expected_shape}")
            continue
        package_dir_name = relative.parts[-2]
        if csproj.stem != package_dir_name:
            diagnostics.append(
                f"{csproj}: project stem {csproj.stem!r} must match package directory {package_dir_name!r}"
            )
            continue
        try:
            dependencies = _parse_package_refs(csproj)
        except (ElementTree.ParseError, OSError, UnicodeError) as error:
            diagnostics.append(f"{csproj}: invalid project XML: {error}")
            continue
        packages.append(
            DiscoveredPackage(
                canonical_key=csproj.stem,
                ecosystem="dotnet",
                root_dir=csproj.parent,
                project_file=csproj,
                charter_path=csproj.parent / "package-charter.yaml",
                dependencies=dependencies,
                source_kind=source_kind,
            )
        )
    return packages


def _is_ignored_dotnet_candidate(relative: Path, source_kind: str) -> bool:
    folded_parts = [part.casefold() for part in relative.parts]
    if any(part in {"bin", "obj"} for part in folded_parts):
        return True
    return source_kind == "tool" and len(folded_parts) >= 2 and folded_parts[:2] == ["tw.templates", "content"]


def discover_frontend_packages(root: Path) -> list[DiscoveredPackage]:
    """Discover frontend shared packages under frontend/packages."""
    packages_root = RepoPaths(root).frontend_packages_root
    if not packages_root.exists():
        return []

    packages: list[DiscoveredPackage] = []
    for package_json in sorted(packages_root.glob("*/package.json")):
        data = _load_json_object(package_json)
        name = data.get("name")
        if not name:
            continue
        packages.append(
            DiscoveredPackage(
                canonical_key=str(name),
                ecosystem="frontend",
                root_dir=package_json.parent,
                project_file=package_json,
                charter_path=package_json.parent / "package-charter.yaml",
                dependencies=_frontend_dependencies(data),
                source_kind="frontend",
            )
        )
    return packages


def _load_json_object(path: Path) -> dict[str, Any]:
    data = json.loads(path.read_text(encoding="utf-8"))
    return data if isinstance(data, dict) else {}


def _frontend_dependencies(data: dict[str, Any]) -> list[str]:
    refs: set[str] = set()
    for field_name in ("dependencies", "devDependencies", "peerDependencies"):
        field_value = data.get(field_name) or {}
        if isinstance(field_value, dict):
            refs.update(str(name) for name in field_value)
    return sorted(refs)
