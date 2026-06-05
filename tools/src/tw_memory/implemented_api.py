from __future__ import annotations

import re
from dataclasses import dataclass
from pathlib import Path

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
    usage_docs: list[str]
    source_files: list[Path]
    usage_doc_paths: list[Path]


def collect_package_api(root: Path, package: DiscoveredPackage) -> PackageApi:
    """Collect implemented public API facts for a discovered package."""
    if package.ecosystem == "dotnet":
        public_types, di_registrations, source_files = _collect_dotnet_public_api(root, package.root_dir)
    else:
        public_types, di_registrations, source_files = [], [], []

    namespaces = sorted({public_type.namespace for public_type in public_types})
    namespaces.extend(
        namespace
        for namespace in _di_namespaces(di_registrations)
        if namespace not in namespaces
    )
    usage_doc_paths = _usage_doc_paths(root, package)

    return PackageApi(
        public_namespaces=sorted(namespaces),
        public_types=public_types,
        di_registrations=di_registrations,
        usage_docs=[repo_relative(root, path) for path in usage_doc_paths],
        source_files=source_files,
        usage_doc_paths=usage_doc_paths,
    )


def _collect_dotnet_public_api(root: Path, package_root: Path) -> tuple[list[PublicType], list[str], list[Path]]:
    public_types: list[PublicType] = []
    di_registrations: list[str] = []
    source_files: set[Path] = set()

    for source_file in sorted(package_root.rglob("*.cs")):
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


def _is_ignored(path: Path) -> bool:
    return any(part in _IGNORED_DIRS for part in path.parts)


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


def _usage_doc_paths(root: Path, package: DiscoveredPackage) -> list[Path]:
    docs_root = _usage_docs_root(root, package)
    if not docs_root.exists():
        return []
    return sorted(path for path in docs_root.rglob("*.md") if path.is_file())


def _usage_docs_root(root: Path, package: DiscoveredPackage) -> Path:
    language = "dotnet" if package.ecosystem == "dotnet" else package.ecosystem
    return root / "docs/shared-packages" / language / package.canonical_key
