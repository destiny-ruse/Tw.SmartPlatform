from __future__ import annotations

import json
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
    packages = [*discover_dotnet_packages(root), *discover_frontend_packages(root)]
    return sorted(packages, key=lambda package: package.canonical_key)


def discover_dotnet_packages(root: Path) -> list[DiscoveredPackage]:
    """Discover .NET shared packages under BuildingBlocks/src."""
    packages_root = RepoPaths(root).dotnet_packages_root
    if not packages_root.exists():
        return []

    packages: list[DiscoveredPackage] = []
    for csproj in sorted(packages_root.glob("*/*.csproj")):
        packages.append(
            DiscoveredPackage(
                canonical_key=csproj.stem,
                ecosystem="dotnet",
                root_dir=csproj.parent,
                project_file=csproj,
                charter_path=csproj.parent / "package-charter.yaml",
                dependencies=_parse_package_refs(csproj),
            )
        )
    return packages


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
