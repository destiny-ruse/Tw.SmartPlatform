from __future__ import annotations

import os
import re
import stat
from dataclasses import dataclass
from pathlib import Path, PurePosixPath, PureWindowsPath
from xml.etree import ElementTree

from tw_memory.generated_io import repo_relative
from tw_memory.packages import DiscoveredPackage

_NAMESPACE = re.compile(r"^namespace\s+([A-Za-z_][A-Za-z0-9_.]*)(?:\s*;|\s*\{)?")
_PUBLIC_TYPE = re.compile(
    r"\bpublic\s+"
    r"(?P<modifiers>(?:(?:abstract|sealed|static|partial|readonly)\s+)*)"
    r"(?P<kind>class|interface|record(?:\s+class|\s+struct)?|struct|enum)\s+"
    r"(?P<name>[A-Za-z_][A-Za-z0-9_]*)"
)
_DI_REGISTRATION = re.compile(
    r"\bpublic\s+static\s+(?:(?:global::)?[A-Za-z_][A-Za-z0-9_.<>]*\.)?"
    r"IServiceCollection\s+(?P<name>Add[A-Za-z0-9_]*)\s*\("
)
_IGNORED_DIRS = {"bin", "obj"}


class PackageApiError(ValueError):
    """A package project model cannot be evaluated within its governed boundary."""


@dataclass(frozen=True)
class PublicType:
    """Public type declaration discovered from a package source file."""

    namespace: str
    kind: str
    name: str
    path: str
    line: int

    @property
    def display(self) -> str:
        """Return a compact display string for generated memory cards."""
        return f"{self.kind} {self.name} - {self.namespace} ({self.path}:{self.line})"


@dataclass(frozen=True)
class PackageApi:
    """Implemented public API facts discovered from package source and docs."""

    public_namespaces: list[str]
    public_types: list[PublicType]
    di_registrations: list[str]
    package_docs: list[str]
    source_files: list[Path]
    package_doc_paths: list[Path]


def collect_package_api(root: Path, package: DiscoveredPackage) -> PackageApi:
    """Collect implemented public API facts for a discovered package."""
    if package.ecosystem == "dotnet":
        public_types, di_registrations, source_files = _collect_dotnet_public_api(root, package.project_file)
    else:
        public_types, di_registrations, source_files = [], [], []

    namespaces = sorted({public_type.namespace for public_type in public_types})
    namespaces.extend(
        namespace
        for namespace in _di_namespaces(di_registrations)
        if namespace not in namespaces
    )
    package_doc_paths = _package_doc_paths(root, package)

    return PackageApi(
        public_namespaces=sorted(namespaces),
        public_types=public_types,
        di_registrations=di_registrations,
        package_docs=[repo_relative(root, path) for path in package_doc_paths],
        source_files=source_files,
        package_doc_paths=package_doc_paths,
    )


def _collect_dotnet_public_api(root: Path, project_file: Path) -> tuple[list[PublicType], list[str], list[Path]]:
    public_types: list[PublicType] = []
    di_registrations: list[str] = []
    source_files: set[Path] = set()

    for source_file in _compile_source_files(project_file):
        if _is_ignored(source_file):
            continue

        namespace = ""
        current_type = ""
        file_has_public_api = False
        for line_number, line in enumerate(source_file.read_text(encoding="utf-8").splitlines(), start=1):
            namespace_match = _NAMESPACE.match(line.strip())
            if namespace_match:
                namespace = namespace_match.group(1)
                current_type = ""
                continue

            type_match = _PUBLIC_TYPE.search(line)
            if type_match and namespace:
                current_type = type_match.group("name")
                public_types.append(
                    PublicType(
                        namespace=namespace,
                        kind=_type_kind(type_match.group("modifiers"), type_match.group("kind")),
                        name=current_type,
                        path=repo_relative(root, source_file),
                        line=line_number,
                    )
                )
                file_has_public_api = True

            registration_match = _DI_REGISTRATION.search(line)
            if registration_match and namespace:
                method_name = registration_match.group("name")
                owner = f"{namespace}.{current_type}" if current_type else namespace
                di_registrations.append(f"{owner}.{method_name}")
                file_has_public_api = True

        if file_has_public_api:
            source_files.add(source_file)

    return (
        sorted(public_types, key=lambda item: (item.namespace, item.name, item.path, item.line)),
        sorted(set(di_registrations)),
        sorted(source_files),
    )


def _compile_source_files(project_file: Path) -> list[Path]:
    project_root = project_file.parent.resolve()
    project = ElementTree.fromstring(project_file.read_text(encoding="utf-8"))
    default_compile_items = True
    for element in project.iter():
        tag = element.tag.rsplit("}", 1)[-1]
        if tag in {"EnableDefaultItems", "EnableDefaultCompileItems"} and element.text:
            if element.text.strip().casefold() == "false":
                default_compile_items = False

    source_files: set[Path] = set()
    if default_compile_items:
        for path in project_root.rglob("*.cs"):
            if _is_ignored(path):
                continue
            source_files.add(_validated_source_path(project_root, path, "default Compile item"))
    for element in project.iter():
        if element.tag.rsplit("}", 1)[-1] != "Compile":
            continue
        for path in _expand_msbuild_paths(
            project_root,
            element.attrib.get("Include", ""),
            attribute="Include",
        ):
            source_files.add(path)
        excluded = {
            path
            for attribute in ("Remove", "Exclude")
            for path in _expand_msbuild_paths(
                project_root,
                element.attrib.get(attribute, ""),
                attribute=attribute,
            )
        }
        source_files.difference_update(excluded)
    return sorted(path for path in source_files if path.is_file() and path.suffix.casefold() == ".cs")


def _expand_msbuild_paths(project_root: Path, value: str, *, attribute: str) -> list[Path]:
    paths: set[Path] = set()
    for raw_pattern in value.split(";"):
        pattern = raw_pattern.strip().replace("\\", "/")
        if not pattern:
            continue
        parts = PurePosixPath(pattern).parts
        if (
            "$(" in pattern
            or PurePosixPath(pattern).is_absolute()
            or PureWindowsPath(pattern).is_absolute()
            or ".." in parts
            or any(character in pattern for character in "*?[")
            or any(part.casefold() in _IGNORED_DIRS for part in parts)
        ):
            raise PackageApiError(f"unsafe Compile {attribute} path {raw_pattern!r}")
        paths.add(_validated_source_path(project_root, project_root / pattern, f"Compile {attribute}"))
    return sorted(paths)


def _validated_source_path(project_root: Path, source_path: Path, label: str) -> Path:
    resolved = source_path.resolve()
    try:
        resolved.relative_to(project_root)
    except ValueError as error:
        raise PackageApiError(
            f"{label} {source_path} escapes package project directory {project_root}"
        ) from error
    return resolved


def _is_ignored(path: Path) -> bool:
    return any(part.casefold() in _IGNORED_DIRS for part in path.parts)


def _type_kind(modifiers: str, kind: str) -> str:
    tokens = [token for token in modifiers.split() if token != "partial"]
    tokens.append(kind.replace("  ", " "))
    return " ".join(tokens)


def _di_namespaces(registrations: list[str]) -> list[str]:
    namespaces: list[str] = []
    for registration in registrations:
        parts = registration.split(".")
        if len(parts) >= 3:
            namespaces.append(".".join(parts[:-2]))
    return namespaces


def _package_doc_paths(root: Path, package: DiscoveredPackage) -> list[Path]:
    docs_root = _package_docs_root(root, package)
    _validate_package_docs_directory_chain(root, docs_root)
    if not os.path.lexists(docs_root):
        return []
    package_docs: list[Path] = []
    for current_value, directory_names, file_names in os.walk(docs_root, followlinks=False):
        current = Path(current_value)
        _validate_package_docs_directory_chain(root, current)
        for name in sorted(directory_names):
            child = current / name
            if _is_reparse_point(child):
                raise PackageApiError(
                    f"package documentation directory {child} must not be a symlink or reparse point"
                )
            _validate_package_docs_directory_chain(root, child)
        for name in sorted(file_names):
            path = current / name
            if path.suffix.casefold() != ".md":
                continue
            _validate_package_doc_file(root, docs_root, path)
            package_docs.append(path)
    return sorted(package_docs)


def _package_docs_root(root: Path, package: DiscoveredPackage) -> Path:
    language = "dotnet" if package.ecosystem == "dotnet" else package.ecosystem
    return root / "docs/shared-packages" / language / package.canonical_key


def _is_reparse_point(path: Path) -> bool:
    metadata = path.lstat()
    attributes = getattr(metadata, "st_file_attributes", 0)
    reparse_flag = getattr(stat, "FILE_ATTRIBUTE_REPARSE_POINT", 0x400)
    return stat.S_ISLNK(metadata.st_mode) or bool(attributes & reparse_flag)


def _validate_package_docs_directory_chain(root: Path, directory: Path) -> None:
    repo_absolute = Path(os.path.abspath(root))
    directory_absolute = Path(os.path.abspath(directory))
    try:
        relative = directory_absolute.relative_to(repo_absolute)
    except ValueError as error:
        raise PackageApiError(
            f"package documentation directory {directory} escapes repository {root}"
        ) from error

    repo_resolved = root.resolve()
    current = repo_absolute
    for part in relative.parts:
        current /= part
        if not os.path.lexists(current):
            continue
        if _is_reparse_point(current):
            raise PackageApiError(
                f"package documentation directory {current} must not be a symlink or reparse point"
            )
        if not stat.S_ISDIR(current.lstat().st_mode):
            raise PackageApiError(f"package documentation path {current} must be a directory")
        try:
            current.resolve(strict=True).relative_to(repo_resolved)
        except (OSError, RuntimeError, ValueError) as error:
            raise PackageApiError(
                f"package documentation directory {current} escapes repository: {error}"
            ) from error


def _validate_package_doc_file(root: Path, docs_root: Path, path: Path) -> None:
    _validate_package_docs_directory_chain(root, path.parent)
    if _is_reparse_point(path):
        raise PackageApiError(
            f"package documentation file {path} must not be a symlink or reparse point"
        )
    if not stat.S_ISREG(path.lstat().st_mode):
        raise PackageApiError(f"package documentation file {path} must be a regular file")
    try:
        relative = path.resolve(strict=True).relative_to(docs_root.resolve(strict=True))
        path.resolve(strict=True).relative_to(root.resolve())
    except (OSError, RuntimeError, ValueError) as error:
        raise PackageApiError(
            f"package documentation file {path} escapes its package documentation root: {error}"
        ) from error
    if not relative.parts:
        raise PackageApiError(
            f"package documentation file {path} must be below {docs_root}"
        )
