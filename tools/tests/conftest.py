from __future__ import annotations

import json
import subprocess
from pathlib import Path

import pytest


@pytest.fixture
def repo(tmp_path: Path) -> Path:
    """Create a minimal repository shape with a .git marker."""
    (tmp_path / ".git").mkdir()
    (tmp_path / ".rules/ai-coding-rules/languages").mkdir(parents=True)
    (tmp_path / ".rules/ai-coding-rules/tasks").mkdir(parents=True)
    (tmp_path / "docs/engineering-standards/03-project-and-code").mkdir(parents=True)
    (tmp_path / "docs/engineering-standards/04-quality").mkdir(parents=True)
    (tmp_path / "backend/dotnet/BuildingBlocks/src").mkdir(parents=True)
    (tmp_path / "backend/dotnet/tools/src").mkdir(parents=True)
    (tmp_path / "contracts/protos").mkdir(parents=True)
    (tmp_path / "frontend/apps").mkdir(parents=True)
    (tmp_path / "frontend/packages").mkdir(parents=True)
    write_text(
        tmp_path / "backend/dotnet/BuildingBlocks/building-blocks-topology.json",
        json.dumps(
            {
                "schemaVersion": 1,
                "runtimeProjects": [],
                "testProjects": [],
                "toolProjects": [],
                "independentContractPackages": [],
                "retiredPackages": [],
            }
        ),
    )
    return tmp_path


@pytest.fixture
def git_repo(tmp_path: Path) -> Path:
    """Create an initialized git repository for commit-boundary tests."""
    subprocess.run(["git", "init", "-q"], cwd=tmp_path, check=True)
    subprocess.run(["git", "config", "user.email", "t@t"], cwd=tmp_path, check=True)
    subprocess.run(["git", "config", "user.name", "t"], cwd=tmp_path, check=True)
    return tmp_path


def write_text(path: Path, content: str) -> Path:
    """Write UTF-8 test content and return the written path."""
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(content, encoding="utf-8")
    return path


def make_csproj(root: Path, capability: str, name: str, body: str = "") -> Path:
    """Create a minimal .NET package project fixture and return its directory."""
    package_dir = root / "backend/dotnet/BuildingBlocks/src" / capability / name
    package_dir.mkdir(parents=True, exist_ok=True)
    content = body or '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup></Project>'
    (package_dir / f"{name}.csproj").write_text(content, encoding="utf-8")
    topology_path = root / "backend/dotnet/BuildingBlocks/building-blocks-topology.json"
    topology = json.loads(topology_path.read_text(encoding="utf-8"))
    topology["runtimeProjects"].append(
        {"path": f"{capability}/{name}/{name}.csproj", "rootNamespace": name}
    )
    topology_path.write_text(json.dumps(topology), encoding="utf-8")
    return package_dir


def make_tool_csproj(root: Path, name: str, body: str = "") -> Path:
    """Create a packageable .NET tool fixture and return its directory."""
    package_dir = root / "backend/dotnet/tools/src" / name
    package_dir.mkdir(parents=True, exist_ok=True)
    content = body or '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup></Project>'
    (package_dir / f"{name}.csproj").write_text(content, encoding="utf-8")
    topology_path = root / "backend/dotnet/BuildingBlocks/building-blocks-topology.json"
    topology = json.loads(topology_path.read_text(encoding="utf-8"))
    topology["toolProjects"].append(
        f"backend/dotnet/tools/src/{name}/{name}.csproj"
    )
    topology_path.write_text(json.dumps(topology), encoding="utf-8")
    return package_dir
