using AwesomeAssertions;
using Xunit;

namespace Tw.Architecture.Tests;

/// <summary>验证 PackageTopologyTests 相关行为</summary>
public sealed class PackageTopologyTests
{
    /// <summary>验证 BuildingBlocks_RuntimeProjects_LiveUnderCapabilityFolders 场景</summary>
    [Fact]
    public void BuildingBlocks_RuntimeProjects_LiveUnderCapabilityFolders()
    {
        var projectFiles = Directory.GetFiles(RepositoryLayout.BuildingBlocksSrc, "*.csproj", SearchOption.AllDirectories);

        projectFiles.Should().NotBeEmpty();
        projectFiles.Should().OnlyContain(
            path => Path.GetRelativePath(RepositoryLayout.BuildingBlocksSrc, path).Replace('\\', '/').Count(ch => ch == '/') == 2,
            "runtime projects must use src/<Capability>/<Package>/<Package>.csproj");
    }

    /// <summary>验证 BuildingBlocks_TestProjects_LiveUnderCapabilityFolders 场景</summary>
    [Fact]
    public void BuildingBlocks_TestProjects_LiveUnderCapabilityFolders()
    {
        var testProjects = Directory.GetFiles(RepositoryLayout.BuildingBlocksTests, "*.csproj", SearchOption.AllDirectories);

        testProjects.Should().NotBeEmpty();
        testProjects.Should().OnlyContain(
            path => Path.GetRelativePath(RepositoryLayout.BuildingBlocksTests, path).Replace('\\', '/').Count(ch => ch == '/') == 2,
            "test projects must use tests/<Capability>/<TestProject>/<TestProject>.csproj");
    }

    /// <summary>验证 BuildingBlocks_TestProjects_MirrorRuntimeCapabilityFolders 场景</summary>
    [Fact]
    public void BuildingBlocks_TestProjects_MirrorRuntimeCapabilityFolders()
    {
        var runtimeCapabilities = RepositoryLayout.RuntimeCapabilitiesByPackage();
        var violations = Directory.GetFiles(RepositoryLayout.BuildingBlocksTests, "*.csproj", SearchOption.AllDirectories)
            .Select(path => new
            {
                Path = path,
                ProjectName = Path.GetFileNameWithoutExtension(path),
                Capability = RepositoryLayout.TestCapability(path)
            })
            .Where(project => project.ProjectName != "Tw.Architecture.Tests")
            .Where(project => !RepositoryLayout.IsAbstractionsTestProject(project.ProjectName))
            .Select(project => new
            {
                project.Path,
                project.ProjectName,
                project.Capability,
                RuntimePackage = RepositoryLayout.RuntimePackageNameForTestProject(project.ProjectName)
            })
            .Where(project => runtimeCapabilities.TryGetValue(project.RuntimePackage, out var capability) && capability != project.Capability)
            .Select(project => $"{project.ProjectName} expected {runtimeCapabilities[project.RuntimePackage]} but was {project.Capability}")
            .ToArray();

        violations.Should().BeEmpty("test projects must stay beside the capability of the runtime package they validate");
    }

    /// <summary>验证 BuildingBlocks_DoesNotContainAbstractionsTestProjects 场景</summary>
    [Fact]
    public void BuildingBlocks_DoesNotContainAbstractionsTestProjects()
    {
        var abstractionsTests = Directory.GetFiles(RepositoryLayout.BuildingBlocksTests, "*.csproj", SearchOption.AllDirectories)
            .Select(path => Path.GetFileNameWithoutExtension(path)!)
            .Where(RepositoryLayout.IsAbstractionsTestProject)
            .ToArray();

        abstractionsTests.Should().BeEmpty("Abstractions packages define contracts and are validated through consuming packages");
    }

    /// <summary>验证 DotnetTools_ProjectsLiveUnderSrcOrTests 场景</summary>
    [Fact]
    public void DotnetTools_ProjectsLiveUnderSrcOrTests()
    {
        var toolProjects = Directory.GetFiles(RepositoryLayout.ToolsRoot, "*.csproj", SearchOption.AllDirectories)
            .Where(path => !Path.GetRelativePath(RepositoryLayout.ToolsRoot, path).Replace('\\', '/').Contains("/content/", StringComparison.Ordinal))
            .ToArray();

        toolProjects.Should().NotBeEmpty();
        toolProjects.Should().OnlyContain(path => IsToolProjectInSrcOrTests(path), "tools projects must use tools/src/<Project> or tools/tests/<Project>");
    }

    /// <summary>验证 ForbiddenPackages_DoNotExist 场景</summary>
    [Fact]
    public void ForbiddenPackages_DoNotExist()
    {
        var forbiddenPackages = new[]
        {
            "Tw.Infrastructure",
            "Tw.Context",
            "Tw.ExecutionPipeline",
            "Tw.Swagger",
            "Tw.ApiVersioning",
            "Tw.Validation",
            "Tw.RateLimiting",
            "Tw.HealthChecks",
            "Tw.ObjectStorage",
            "Tw.Serialization",
            "Tw.Bff",
            "Tw.DynamicApi",
            "Tw.AspNetCore.DynamicApi",
            "Tw.ApplicationConfiguration",
            "Tw.Snowflake",
            "Tw.DistributedLock",
            "Tw.Autofac",
            "Tw.Localization.AspNetCore",
            "Tw.Grpc.AspNetCore",
            "Tw.Cqrs",
            "Tw.UnitOfWork",
            "Tw.Data.Abstractions",
            "Tw.Testing"
        };

        var actualPackages = Directory.GetFiles(RepositoryLayout.BuildingBlocksSrc, "*.csproj", SearchOption.AllDirectories)
            .Select(Path.GetFileNameWithoutExtension)
            .ToHashSet(StringComparer.Ordinal);

        actualPackages.Should().NotIntersectWith(forbiddenPackages);
    }

    /// <summary>验证 IsToolProjectInSrcOrTests 场景</summary>
    /// <param name="path">path 参数</param>
    /// <returns>IsToolProjectInSrcOrTests 的执行结果</returns>
    private static bool IsToolProjectInSrcOrTests(string path)
    {
        var relative = Path.GetRelativePath(RepositoryLayout.ToolsRoot, path).Replace('\\', '/');
        var parts = relative.Split('/');
        return parts.Length == 3 && (parts[0] == "src" || parts[0] == "tests");
    }
}
