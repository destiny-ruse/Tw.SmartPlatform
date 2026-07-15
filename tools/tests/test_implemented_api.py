from __future__ import annotations

from pathlib import Path

import pytest

from conftest import make_csproj, make_tool_csproj, write_text
from tw_memory.implemented_api import PackageApiError, collect_package_api
from tw_memory.packages import discover_packages


def test_collect_package_api_discovers_dotnet_public_api_and_package_docs(repo: Path) -> None:
    package_dir = make_csproj(repo, "Localization", "Tw.Localization")
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
    assert api.package_docs == [
        "docs/shared-packages/dotnet/Tw.Localization/text-localization.md"
    ]


def test_collect_package_api_uses_only_explicit_compile_items_when_defaults_are_disabled(repo: Path) -> None:
    package_dir = make_tool_csproj(
        repo,
        "Tw.Templates",
        """<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup><EnableDefaultItems>false</EnableDefaultItems></PropertyGroup>
  <ItemGroup><Compile Include="Generator.cs" /><Content Include="content\\**\\*" /></ItemGroup>
</Project>""",
    )
    write_text(
        package_dir / "Generator.cs",
        "namespace Tw.Templates;\n\npublic sealed class TemplateGenerator {}\n",
    )
    write_text(
        package_dir / "content/service/src/Company.Service/Example.cs",
        "namespace Company.Service;\n\npublic sealed class Example {}\n",
    )

    package = discover_packages(repo)[0]
    api = collect_package_api(repo, package)

    assert [public_type.name for public_type in api.public_types] == ["TemplateGenerator"]
    assert all("content/" not in path.as_posix() for path in api.source_files)


def test_collect_templates_api_ignores_content_when_no_compile_items_exist(repo: Path) -> None:
    package_dir = make_tool_csproj(
        repo,
        "Tw.Templates",
        """<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup><EnableDefaultItems>false</EnableDefaultItems></PropertyGroup>
  <ItemGroup><Content Include="content\\**\\*" /></ItemGroup>
</Project>""",
    )
    write_text(
        package_dir / "content/building-block/src/Capability/Tw.Sample/Sample.cs",
        "namespace Tw.Sample;\n\npublic sealed class Sample {}\n",
    )

    package = discover_packages(repo)[0]
    api = collect_package_api(repo, package)

    assert api.public_namespaces == []
    assert api.public_types == []
    assert api.source_files == []


@pytest.mark.parametrize(
    ("include", "source_path"),
    [
        ("../Outside.cs", "../Outside.cs"),
        ("Sources/*.cs", "Sources/Example.cs"),
        ("OBJ/Generated.cs", "OBJ/Generated.cs"),
        ("BIN/Generated.cs", "BIN/Generated.cs"),
    ],
)
def test_collect_package_api_rejects_unsafe_explicit_compile_items(
    repo: Path,
    include: str,
    source_path: str,
) -> None:
    package_dir = make_csproj(
        repo,
        "Foundation",
        "Tw.Core",
        f'<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><EnableDefaultCompileItems>false</EnableDefaultCompileItems></PropertyGroup><ItemGroup><Compile Include="{include}" /></ItemGroup></Project>',
    )
    write_text(
        package_dir / source_path,
        "namespace Tw.Unsafe;\n\npublic sealed class UnsafeType {}\n",
    )

    with pytest.raises(ValueError, match="unsafe Compile"):
        collect_package_api(repo, discover_packages(repo)[0])


def test_collect_package_api_rejects_absolute_compile_item(repo: Path) -> None:
    external_source = write_text(
        repo.parent / f"{repo.name}-external/External.cs",
        "namespace Tw.External;\n\npublic sealed class ExternalType {}\n",
    )
    package_dir = make_csproj(
        repo,
        "Foundation",
        "Tw.Core",
        f'<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><EnableDefaultCompileItems>false</EnableDefaultCompileItems></PropertyGroup><ItemGroup><Compile Include="{external_source.as_posix()}" /></ItemGroup></Project>',
    )

    with pytest.raises(ValueError, match="unsafe Compile"):
        collect_package_api(repo, discover_packages(repo)[0])


def test_collect_package_api_rejects_source_symlink_that_escapes_package(repo: Path) -> None:
    external_source = write_text(
        repo.parent / f"{repo.name}-external/External.cs",
        "namespace Tw.External;\n\npublic sealed class ExternalType {}\n",
    )
    package_dir = make_csproj(
        repo,
        "Foundation",
        "Tw.Core",
        '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><EnableDefaultCompileItems>false</EnableDefaultCompileItems></PropertyGroup><ItemGroup><Compile Include="Linked.cs" /></ItemGroup></Project>',
    )
    (package_dir / "Linked.cs").symlink_to(external_source)

    with pytest.raises(ValueError, match="escapes package project directory"):
        collect_package_api(repo, discover_packages(repo)[0])


def test_collect_package_api_ignores_default_compile_items_in_case_variant_build_dirs(repo: Path) -> None:
    package_dir = make_csproj(repo, "Foundation", "Tw.Core")
    write_text(
        package_dir / "Source.cs",
        "namespace Tw.Core;\n\npublic sealed class SourceType {}\n",
    )
    write_text(
        package_dir / "OBJ/Generated.cs",
        "namespace Tw.Core;\n\npublic sealed class GeneratedType {}\n",
    )

    api = collect_package_api(repo, discover_packages(repo)[0])

    assert [public_type.name for public_type in api.public_types] == ["SourceType"]


def test_collect_package_api_rejects_external_package_readme_symlink(repo: Path) -> None:
    make_csproj(repo, "Foundation", "Tw.Core")
    external_readme = write_text(
        repo.parent / f"{repo.name}-external-docs/README.md",
        "# External documentation\n",
    )
    docs_root = repo / "docs/shared-packages/dotnet/Tw.Core"
    docs_root.mkdir(parents=True)
    (docs_root / "README.md").symlink_to(external_readme)

    with pytest.raises(PackageApiError, match="package documentation"):
        collect_package_api(repo, discover_packages(repo)[0])


def test_collect_package_api_rejects_external_package_docs_directory(repo: Path) -> None:
    make_csproj(repo, "Foundation", "Tw.Core")
    external_docs = repo.parent / f"{repo.name}-external-docs"
    write_text(external_docs / "README.md", "# External documentation\n")
    docs_parent = repo / "docs/shared-packages/dotnet"
    docs_parent.mkdir(parents=True)
    (docs_parent / "Tw.Core").symlink_to(external_docs, target_is_directory=True)

    with pytest.raises(PackageApiError, match="package documentation"):
        collect_package_api(repo, discover_packages(repo)[0])
