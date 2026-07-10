using AwesomeAssertions;
using Xunit;

namespace Tw.Architecture.Tests;

/// <summary>
/// 覆盖PackageTopology的核心行为和边界条件
/// </summary>
public sealed class PackageTopologyTests
{
    /// <summary>
    /// 验证BuildingBlocksRuntimeProjectsLiveUnderCapabilityFolders
    /// </summary>
    [Fact]
    public void BuildingBlocks_RuntimeProjects_LiveUnderCapabilityFolders()
    {
        var projectFiles = Directory.GetFiles(RepositoryLayout.BuildingBlocksSrc, "*.csproj", SearchOption.AllDirectories);

        projectFiles.Should().NotBeEmpty();
        projectFiles.Should().OnlyContain(
            path => Path.GetRelativePath(RepositoryLayout.BuildingBlocksSrc, path).Replace('\\', '/').Count(ch => ch == '/') == 2,
            "runtime projects must use src/<Capability>/<Package>/<Package>.csproj");
    }

    /// <summary>
    /// 验证BuildingBlocksTestProjectsLiveUnderCapabilityFolders
    /// </summary>
    [Fact]
    public void BuildingBlocks_TestProjects_LiveUnderCapabilityFolders()
    {
        var testProjects = Directory.GetFiles(RepositoryLayout.BuildingBlocksTests, "*.csproj", SearchOption.AllDirectories);

        testProjects.Should().NotBeEmpty();
        testProjects.Should().OnlyContain(
            path => Path.GetRelativePath(RepositoryLayout.BuildingBlocksTests, path).Replace('\\', '/').Count(ch => ch == '/') == 2,
            "test projects must use tests/<Capability>/<TestProject>/<TestProject>.csproj");
    }

    /// <summary>
    /// 验证BuildingBlocksTestProjectsMirrorRuntimeCapabilityFolders
    /// </summary>
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

    /// <summary>
    /// 验证BuildingBlocks不ContainAbstractionsTestProjects
    /// </summary>
    [Fact]
    public void BuildingBlocks_DoesNotContainAbstractionsTestProjects()
    {
        var abstractionsTests = Directory.GetFiles(RepositoryLayout.BuildingBlocksTests, "*.csproj", SearchOption.AllDirectories)
            .Select(path => Path.GetFileNameWithoutExtension(path)!)
            .Where(RepositoryLayout.IsAbstractionsTestProject)
            .ToArray();

        abstractionsTests.Should().BeEmpty("Abstractions packages define contracts and are validated through consuming packages");
    }

    /// <summary>
    /// 验证DotnetToolsProjectsLiveUnderSrcOr
    /// </summary>
    [Fact]
    public void DotnetTools_ProjectsLiveUnderSrcOrTests()
    {
        var toolProjects = Directory.GetFiles(RepositoryLayout.ToolsRoot, "*.csproj", SearchOption.AllDirectories)
            .Where(path => !Path.GetRelativePath(RepositoryLayout.ToolsRoot, path).Replace('\\', '/').Contains("/content/", StringComparison.Ordinal))
            .ToArray();

        toolProjects.Should().NotBeEmpty();
        toolProjects.Should().OnlyContain(path => IsToolProjectInSrcOrTests(path), "tools projects must use tools/src/<Project> or tools/tests/<Project>");
    }

    /// <summary>
    /// 验证DotnetTest项目Classification不TreatTest基类包作为Executable
    /// </summary>
    [Fact]
    public void DotnetTestProjectClassification_DoesNotTreatTestBasePackagesAsExecutableTests()
    {
        var directoryBuildProps = File.ReadAllText(Path.Combine(RepositoryLayout.DotnetRoot, "Directory.Build.props"));

        directoryBuildProps.Should().NotContain(
            "EndsWith('.TestBase')",
            "TestBase source packages provide reusable fixtures and must not be executed as VSTest projects by solution-level dotnet test");
    }

    /// <summary>
    /// 验证禁止包Do不Exist
    /// </summary>
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

    /// <summary>
    /// 判断工具项目InSrcOr是否满足条件
    /// </summary>
    /// <param name="path">待处理文件或目录的路径</param>
    /// <returns>条件满足时返回 <see langword="true"/></returns>
    private static bool IsToolProjectInSrcOrTests(string path)
    {
        var relative = Path.GetRelativePath(RepositoryLayout.ToolsRoot, path).Replace('\\', '/');
        var parts = relative.Split('/');
        return parts.Length == 3 && (parts[0] == "src" || parts[0] == "tests");
    }
}
