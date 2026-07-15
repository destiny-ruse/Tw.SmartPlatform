from __future__ import annotations

from dataclasses import dataclass
from pathlib import Path


def find_repo_root(start: Path | None = None) -> Path:
    """Find the nearest parent directory that contains a .git marker."""
    current = (start or Path.cwd()).resolve()
    for candidate in (current, *current.parents):
        if (candidate / ".git").exists():
            return candidate
    raise FileNotFoundError("repository root with .git not found")


@dataclass(frozen=True)
class RepoPaths:
    """Repository paths used by the generated memory tooling."""

    root: Path

    @property
    def tw_memory(self) -> Path:
        return self.root / ".tw-memory"

    @property
    def manifest(self) -> Path:
        return self.tw_memory / "manifest"

    @property
    def routes(self) -> Path:
        return self.tw_memory / "routes"

    @property
    def cards(self) -> Path:
        return self.tw_memory / "cards"

    @property
    def source_index(self) -> Path:
        return self.manifest / "source-index.generated.json"

    @property
    def taxonomy(self) -> Path:
        return self.manifest / "taxonomy.generated.yaml"

    @property
    def standards_route(self) -> Path:
        return self.routes / "standards.generated.yaml"

    @property
    def skills_route(self) -> Path:
        return self.routes / "skills.generated.yaml"

    @property
    def codegraph_queries_route(self) -> Path:
        return self.routes / "codegraph-queries.generated.yaml"

    @property
    def packages_route(self) -> Path:
        return self.routes / "packages.generated.yaml"

    @property
    def services_route(self) -> Path:
        return self.routes / "services.generated.yaml"

    @property
    def apis_route(self) -> Path:
        return self.routes / "apis.generated.yaml"

    @property
    def frontend_route(self) -> Path:
        return self.routes / "frontend.generated.yaml"

    @property
    def package_cards(self) -> Path:
        return self.cards / "packages"

    @property
    def public_api_cards(self) -> Path:
        return self.cards / "public-apis"

    @property
    def dotnet_packages_root(self) -> Path:
        return self.root / "backend/dotnet/BuildingBlocks/src"

    @property
    def dotnet_tools_root(self) -> Path:
        """Return the root that contains packageable .NET tool projects."""
        return self.root / "backend/dotnet/tools/src"

    @property
    def dotnet_topology(self) -> Path:
        """Return the approved .NET package topology manifest."""
        return self.root / "backend/dotnet/BuildingBlocks/building-blocks-topology.json"

    @property
    def shared_package_docs_root(self) -> Path:
        """Return the root of shared-package documentation."""
        return self.root / "docs/shared-packages"

    @property
    def dotnet_package_docs_root(self) -> Path:
        """Return the root of governed .NET package documentation."""
        return self.shared_package_docs_root / "dotnet"

    @property
    def frontend_packages_root(self) -> Path:
        return self.root / "frontend/packages"
