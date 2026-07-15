from __future__ import annotations

import json
from pathlib import Path

import tw_memory.packages as packages_module

from conftest import make_csproj, make_tool_csproj, write_text
from tw_memory.packages import discover_packages


def discover_inventory(repo: Path) -> packages_module.PackageDiscovery:
    """Call the strict discovery API after proving that it exists."""
    assert hasattr(packages_module, "discover_package_inventory"), "strict package discovery API is missing"
    return packages_module.discover_package_inventory(repo)


def test_discover_dotnet_package_uses_csproj_file_name(repo: Path) -> None:
    package_dir = make_csproj(
        repo,
        "Foundation",
        "Tw.Core",
        '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><RootNamespace>Tw</RootNamespace></PropertyGroup></Project>',
    )

    package = discover_packages(repo)[0]

    assert package.canonical_key == "Tw.Core"
    assert package.ecosystem == "dotnet"
    assert package.root_dir == package_dir
    assert package.project_file == package_dir / "Tw.Core.csproj"
    assert package.charter_path == package_dir / "package-charter.yaml"


def test_discover_package_references(repo: Path) -> None:
    make_csproj(
        repo,
        "Web",
        "Tw.AspNetCore",
        '<Project Sdk="Microsoft.NET.Sdk"><ItemGroup><PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="1.0.0" /></ItemGroup></Project>',
    )

    package = discover_packages(repo)[0]

    assert package.dependencies == ["Microsoft.AspNetCore.OpenApi"]


def test_discover_project_references(repo: Path) -> None:
    make_csproj(
        repo,
        "Web",
        "Tw.AspNetCore",
        '<Project Sdk="Microsoft.NET.Sdk"><ItemGroup><ProjectReference Include="..\\Tw.Core\\Tw.Core.csproj" /></ItemGroup></Project>',
    )

    package = discover_packages(repo)[0]

    assert package.dependencies == ["Tw.Core"]


def test_discover_packageable_dotnet_tool(repo: Path) -> None:
    package_dir = make_tool_csproj(repo, "Tw.Cli")

    package = discover_packages(repo)[0]

    assert package.canonical_key == "Tw.Cli"
    assert package.root_dir == package_dir
    assert package.project_file == package_dir / "Tw.Cli.csproj"


def test_discovery_reports_flat_building_block_shape(repo: Path) -> None:
    project = write_text(
        repo / "backend/dotnet/BuildingBlocks/src/Tw.Flat/Tw.Flat.csproj",
        '<Project Sdk="Microsoft.NET.Sdk" />',
    )

    discovery = discover_inventory(repo)

    assert discovery.packages == []
    assert any(str(project) in diagnostic and "Capability/Package/Package.csproj" in diagnostic for diagnostic in discovery.diagnostics)


def test_discovery_reports_directory_and_project_stem_mismatch(repo: Path) -> None:
    project = write_text(
        repo / "backend/dotnet/BuildingBlocks/src/Foundation/Tw.Core/Tw.Foundation.csproj",
        '<Project Sdk="Microsoft.NET.Sdk" />',
    )

    discovery = discover_inventory(repo)

    assert discovery.packages == []
    assert any(str(project) in diagnostic and "project stem" in diagnostic for diagnostic in discovery.diagnostics)


def test_discovery_rejects_duplicate_canonical_key_across_governed_roots(repo: Path) -> None:
    make_csproj(repo, "Tooling", "Tw.Cli")
    make_tool_csproj(repo, "Tw.Cli")

    discovery = discover_inventory(repo)

    assert [package.canonical_key for package in discovery.packages] == ["Tw.Cli", "Tw.Cli"]
    assert any("duplicate canonical package key" in diagnostic and "Tw.Cli" in diagnostic for diagnostic in discovery.diagnostics)


def test_discovery_rejects_project_symlink_that_escapes_governed_root(repo: Path) -> None:
    external_project = write_text(
        repo.parent / f"{repo.name}-external/Tw.External.csproj",
        '<Project Sdk="Microsoft.NET.Sdk" />',
    )
    project = repo / "backend/dotnet/BuildingBlocks/src/Foundation/Tw.External/Tw.External.csproj"
    project.parent.mkdir(parents=True)
    project.symlink_to(external_project)

    discovery = discover_inventory(repo)

    assert discovery.packages == []
    assert any(str(project) in diagnostic and "escapes governed root" in diagnostic for diagnostic in discovery.diagnostics)


def test_discovery_reports_directory_symlink_that_escapes_governed_root(repo: Path) -> None:
    external_package = repo.parent / f"{repo.name}-external/Tw.External"
    write_text(
        external_package / "Tw.External.csproj",
        '<Project Sdk="Microsoft.NET.Sdk" />',
    )
    capability = repo / "backend/dotnet/BuildingBlocks/src/Foundation"
    capability.mkdir(parents=True)
    linked_package = capability / "Tw.External"
    linked_package.symlink_to(external_package, target_is_directory=True)

    discovery = discover_inventory(repo)

    assert discovery.packages == []
    assert any(
        str(linked_package) in diagnostic and "governed directory escapes governed root" in diagnostic
        for diagnostic in discovery.diagnostics
    )


def test_discovery_rejects_governed_root_symlink_that_escapes_repository(repo: Path) -> None:
    external_root = repo.parent / f"{repo.name}-external/src"
    write_text(
        external_root / "Foundation/Tw.External/Tw.External.csproj",
        '<Project Sdk="Microsoft.NET.Sdk" />',
    )
    governed_root = repo / "backend/dotnet/BuildingBlocks/src"
    governed_root.rmdir()
    governed_root.symlink_to(external_root, target_is_directory=True)

    discovery = discover_inventory(repo)

    assert discovery.packages == []
    assert any(
        str(governed_root) in diagnostic and "governed root escapes repository" in diagnostic
        for diagnostic in discovery.diagnostics
    )


def test_discovery_reports_malformed_csproj_xml(repo: Path) -> None:
    project_dir = make_csproj(repo, "Foundation", "Tw.Core", "<Project>")

    discovery = discover_inventory(repo)

    assert discovery.packages == []
    assert any(
        str(project_dir / "Tw.Core.csproj") in diagnostic and "invalid project XML" in diagnostic
        for diagnostic in discovery.diagnostics
    )


def test_discover_frontend_package(repo: Path) -> None:
    package_dir = repo / "frontend/packages/ui"
    write_text(
        package_dir / "package.json",
        json.dumps(
            {
                "name": "@tw/ui",
                "dependencies": {"vue": "latest"},
                "devDependencies": {"vite": "latest"},
                "peerDependencies": {"typescript": "latest"},
            }
        ),
    )

    package = discover_packages(repo)[0]

    assert package.canonical_key == "@tw/ui"
    assert package.ecosystem == "frontend"
    assert package.root_dir == package_dir
    assert package.project_file == package_dir / "package.json"
    assert package.dependencies == ["typescript", "vite", "vue"]
