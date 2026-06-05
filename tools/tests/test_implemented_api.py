from __future__ import annotations

from pathlib import Path

from conftest import make_csproj, write_text
from tw_memory.implemented_api import collect_package_api
from tw_memory.packages import discover_packages


def test_collect_package_api_discovers_dotnet_public_api_and_usage_docs(repo: Path) -> None:
    package_dir = make_csproj(repo, "Tw.Localization")
    write_text(
        package_dir / "Requests/TextLookupRequest.cs",
        """namespace Tw.Localization.Requests;

public sealed record TextLookupRequest(string ResourceName);
""",
    )
    write_text(
        package_dir / "LocalizationServiceCollectionExtensions.cs",
        """using Microsoft.Extensions.DependencyInjection;

namespace Tw.Localization;

public static class LocalizationServiceCollectionExtensions
{
    public static IServiceCollection AddLocalization(this IServiceCollection services)
    {
        return services;
    }
}
""",
    )
    write_text(
        repo / "docs/shared-packages/dotnet/Tw.Localization/text-localization.md",
        "# 文本本地化使用指南\n",
    )

    package = discover_packages(repo)[0]
    api = collect_package_api(repo, package)

    assert "Tw.Localization" in api.public_namespaces
    assert "Tw.Localization.Requests" in api.public_namespaces
    assert any(public_type.name == "TextLookupRequest" for public_type in api.public_types)
    assert api.di_registrations == [
        "Tw.Localization.LocalizationServiceCollectionExtensions.AddLocalization"
    ]
    assert api.usage_docs == [
        "docs/shared-packages/dotnet/Tw.Localization/text-localization.md"
    ]
