from __future__ import annotations

import os
import stat
from pathlib import Path


class GeneratedPathError(ValueError):
    """A generated-memory path is outside its authorized mutation boundary."""


def repo_relative(root: Path, path: Path) -> str:
    """Return a resolved repository-relative path with slash separators."""
    return path.resolve().relative_to(root.resolve()).as_posix()


def _lexical_relative(root: Path, path: Path) -> Path:
    root_absolute = Path(os.path.abspath(root))
    path_absolute = Path(os.path.abspath(path))
    try:
        return path_absolute.relative_to(root_absolute)
    except ValueError as error:
        raise GeneratedPathError(f"{path}: generated path is outside repository {root}") from error


def _path_entry_exists(path: Path) -> bool:
    return os.path.lexists(path)


def _is_reparse_point(path: Path) -> bool:
    metadata = path.lstat()
    attributes = getattr(metadata, "st_file_attributes", 0)
    reparse_flag = getattr(stat, "FILE_ATTRIBUTE_REPARSE_POINT", 0x400)
    return stat.S_ISLNK(metadata.st_mode) or bool(attributes & reparse_flag)


def _assert_generated_relative(root: Path, path: Path) -> Path:
    relative = _lexical_relative(root, path)
    if not relative.parts or relative.parts[0] != ".tw-memory":
        raise GeneratedPathError(f"{path}: generated path is outside .tw-memory")
    if "runtime" in relative.parts[1:]:
        raise GeneratedPathError(f"{path}: runtime state is not generated commit memory")
    return relative


def assert_generated_memory_path(root: Path, path: Path) -> None:
    """Ensure a lexical .tw-memory path is a generated commit-layer file."""
    relative = _assert_generated_relative(root, path)
    value = relative.as_posix()
    is_card = (
        len(relative.parts) == 4
        and relative.parts[1] == "cards"
        and relative.parts[2] in {"packages", "public-apis"}
        and value.endswith(".generated.md")
    )
    is_route = (
        len(relative.parts) == 3
        and relative.parts[1] == "routes"
        and relative.name
        in {
            "apis.generated.yaml",
            "codegraph-queries.generated.yaml",
            "frontend.generated.yaml",
            "packages.generated.yaml",
            "services.generated.yaml",
            "skills.generated.yaml",
            "standards.generated.yaml",
        }
    )
    is_manifest = (
        len(relative.parts) == 3
        and relative.parts[1] == "manifest"
        and relative.name in {
            "source-index.generated.json",
            "taxonomy.generated.yaml",
        }
    )
    if not (is_card or is_route or is_manifest):
        raise GeneratedPathError(f"{path}: path is not a generated memory file")


def generated_repo_relative(root: Path, path: Path) -> str:
    """Return an authorized generated path without following filesystem links."""
    assert_generated_memory_path(root, path)
    return _lexical_relative(root, path).as_posix()


def _validate_directory_chain(root: Path, directory: Path) -> None:
    relative = _assert_generated_relative(root, directory)
    root_absolute = Path(os.path.abspath(root))
    repo_resolved = root.resolve()
    current = root_absolute
    for part in relative.parts:
        current /= part
        if not _path_entry_exists(current):
            continue
        if _is_reparse_point(current):
            raise GeneratedPathError(
                f"{current}: generated output directory must not be a symlink or reparse point"
            )
        if not stat.S_ISDIR(current.lstat().st_mode):
            raise GeneratedPathError(f"{current}: generated output root must be a directory")
        try:
            current.resolve(strict=True).relative_to(repo_resolved)
        except (OSError, RuntimeError, ValueError) as error:
            raise GeneratedPathError(
                f"{current}: unsafe generated output directory: {error}"
            ) from error


def validate_generated_directory(root: Path, directory: Path) -> None:
    """Validate an existing-or-planned generated directory without reading through links."""
    _validate_directory_chain(root, directory)


def validate_generated_file(root: Path, path: Path) -> None:
    """Validate an existing-or-planned generated file and its full directory chain."""
    assert_generated_memory_path(root, path)
    _validate_directory_chain(root, path.parent)
    if not _path_entry_exists(path):
        return
    if _is_reparse_point(path):
        raise GeneratedPathError(
            f"{path}: generated output file must not be a symlink or reparse point"
        )
    if not stat.S_ISREG(path.lstat().st_mode):
        raise GeneratedPathError(f"{path}: generated output target must be a regular file")
    try:
        path.resolve(strict=True).relative_to(root.resolve())
    except (OSError, RuntimeError, ValueError) as error:
        raise GeneratedPathError(f"{path}: unsafe generated output file: {error}") from error


def validate_generated_deletion(root: Path, path: Path, authority: Path) -> None:
    """Revalidate one orphan deletion against its exact authoritative card root."""
    validate_generated_directory(root, authority)
    try:
        lexical_relative = Path(os.path.abspath(path)).relative_to(Path(os.path.abspath(authority)))
    except ValueError as error:
        raise GeneratedPathError(
            f"{path}: generated deletion candidate is outside {authority}"
        ) from error
    if len(lexical_relative.parts) != 1:
        raise GeneratedPathError(
            f"{path}: generated deletion candidate must be a direct child of {authority}"
        )
    validate_generated_file(root, path)
    if not _path_entry_exists(path):
        raise GeneratedPathError(f"{path}: generated deletion candidate no longer exists")
    try:
        resolved_relative = path.resolve(strict=True).relative_to(authority.resolve(strict=True))
    except (OSError, RuntimeError, ValueError) as error:
        raise GeneratedPathError(f"{path}: unsafe generated deletion candidate: {error}") from error
    if len(resolved_relative.parts) != 1:
        raise GeneratedPathError(
            f"{path}: generated deletion candidate must resolve directly below {authority}"
        )


def generated_memory_safety_errors(
    root: Path,
    *,
    directories: tuple[Path, ...] = (),
    output_files: set[Path] | None = None,
    deletion_candidates: dict[Path, Path] | None = None,
) -> list[str]:
    """Collect generated-memory path diagnostics without traversing unsafe entries."""
    errors: list[str] = []
    for directory in sorted(set(directories)):
        try:
            validate_generated_directory(root, directory)
        except (GeneratedPathError, OSError, RuntimeError) as error:
            errors.append(str(error))
    for output_file in sorted(output_files or set()):
        try:
            validate_generated_file(root, output_file)
        except (GeneratedPathError, OSError, RuntimeError) as error:
            errors.append(str(error))
    for candidate, authority in sorted((deletion_candidates or {}).items()):
        try:
            validate_generated_deletion(root, candidate, authority)
        except (GeneratedPathError, OSError, RuntimeError) as error:
            errors.append(str(error))
    return errors


def write_generated_text(root: Path, path: Path, content: str) -> None:
    """Write generated text after revalidating every filesystem mutation boundary."""
    validate_generated_file(root, path)
    path.parent.mkdir(parents=True, exist_ok=True)
    validate_generated_file(root, path)
    normalized = content.replace("\r\n", "\n").replace("\r", "\n")
    validate_generated_file(root, path)
    path.write_text(normalized, encoding="utf-8", newline="\n")


def unlink_generated_file(root: Path, path: Path, authority: Path) -> None:
    """Delete one generated orphan after an immediate authority revalidation."""
    validate_generated_deletion(root, path, authority)
    path.unlink()
