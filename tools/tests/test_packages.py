from __future__ import annotations

import json
from pathlib import Path

from conftest import make_csproj, write_text
from tw_memory.packages import discover_packages


def test_discover_dotnet_package_uses_csproj_file_name(repo: Path) -> None:
    package_dir = make_csproj(
        repo,
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
        "Tw.AspNetCore",
        '<Project Sdk="Microsoft.NET.Sdk"><ItemGroup><PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="1.0.0" /></ItemGroup></Project>',
    )

    package = discover_packages(repo)[0]

    assert package.dependencies == ["Microsoft.AspNetCore.OpenApi"]


def test_discover_project_references(repo: Path) -> None:
    make_csproj(
        repo,
        "Tw.AspNetCore",
        '<Project Sdk="Microsoft.NET.Sdk"><ItemGroup><ProjectReference Include="..\\Tw.Core\\Tw.Core.csproj" /></ItemGroup></Project>',
    )

    package = discover_packages(repo)[0]

    assert package.dependencies == ["Tw.Core"]


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
