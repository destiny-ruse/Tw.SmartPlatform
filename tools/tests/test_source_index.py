from __future__ import annotations

from pathlib import Path

import pytest

from tw_memory.generated_io import assert_generated_memory_path
from tw_memory.hashing import sha256_normalized
from tw_memory.repo import RepoPaths, find_repo_root
from tw_memory.yaml_io import dump_yaml, load_yaml


def test_find_repo_root_from_nested_directory(repo: Path) -> None:
    nested = repo / "backend/dotnet/BuildingBlocks/src"

    assert find_repo_root(nested) == repo


def test_repo_paths_point_to_generated_memory(repo: Path) -> None:
    paths = RepoPaths(repo)

    assert paths.source_index == repo / ".tw-memory/manifest/source-index.generated.json"
    assert paths.standards_route == repo / ".tw-memory/routes/standards.generated.yaml"
    assert paths.package_cards == repo / ".tw-memory/cards/packages"


def test_sha256_normalizes_line_endings(tmp_path: Path) -> None:
    lf = tmp_path / "lf.txt"
    crlf = tmp_path / "crlf.txt"
    lf.write_bytes(b"a\nb\n")
    crlf.write_bytes(b"a\r\nb\r\n")

    assert sha256_normalized(lf) == sha256_normalized(crlf)


def test_yaml_dump_is_deterministic(tmp_path: Path) -> None:
    path = tmp_path / "x.yaml"

    dump_yaml(path, {"b": 2, "a": 1})

    assert path.read_text(encoding="utf-8") == "a: 1\nb: 2\n"
    assert load_yaml(path) == {"a": 1, "b": 2}


def test_generated_memory_path_rejects_manual_file(repo: Path) -> None:
    with pytest.raises(ValueError, match="generated"):
        assert_generated_memory_path(repo, repo / ".tw-memory/README.md")
