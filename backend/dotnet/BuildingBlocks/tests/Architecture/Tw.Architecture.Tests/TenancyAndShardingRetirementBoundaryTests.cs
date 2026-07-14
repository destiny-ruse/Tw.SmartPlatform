using AwesomeAssertions;
using Xunit;

namespace Tw.Architecture.Tests;

/// <summary>
/// 固定多租户与分片契约合并后的项目和命名空间边界
/// </summary>
public sealed class TenancyAndShardingRetirementBoundaryTests
{
    /// <summary>
    /// Task 11 删除的两个运行时项目路径
    /// </summary>
    private static readonly string[] RetiredRuntimeProjectPaths =
    [
        "backend/dotnet/BuildingBlocks/src/MultiTenancy/Tw.MultiTenancy.Abstractions/Tw.MultiTenancy.Abstractions.csproj",
        "backend/dotnet/BuildingBlocks/src/Sharding/Tw.Sharding.Abstractions/Tw.Sharding.Abstractions.csproj"
    ];

    /// <summary>
    /// 合并后必须保持唯一项目库存的能力目录
    /// </summary>
    private static readonly (string Capability, string RuntimeProject, string TestProject)[] RetainedCapabilities =
    [
        ("MultiTenancy", "Tw.MultiTenancy.csproj", "Tw.MultiTenancy.Tests.csproj"),
        ("Sharding", "Tw.Sharding.csproj", "Tw.Sharding.Tests.csproj")
    ];

    /// <summary>
    /// 已删除的契约项目文件和目录不能通过阶段性清单重新出现
    /// </summary>
    [Fact]
    public void RetiredRuntimeProjectFilesAndDirectories_DoNotExist()
    {
        var violations = RetiredRuntimeProjectPaths
            .SelectMany(FindExistingProjectBoundary)
            .ToArray();

        violations.Should().BeEmpty(
            "Task 11 retirement is complete and must not use the phased retired-project allowance");
    }

    /// <summary>
    /// 多租户和分片能力各自只有一个运行时项目与一个测试项目
    /// </summary>
    [Fact]
    public void RetainedCapabilities_HaveExactlyOneRuntimeAndOneTestProject()
    {
        var violations = RetainedCapabilities
            .SelectMany(FindInventoryViolations)
            .ToArray();

        violations.Should().BeEmpty(
            "each consolidated capability must keep one retained runtime project and one retained test project");
    }

    /// <summary>
    /// 已退休命名空间不能继续由活动 C# 源文件声明
    /// </summary>
    [Fact]
    public void ActiveSources_DoNotDeclareRetiredContractNamespaces()
    {
        var retiredNamespaceDeclarations = new[]
        {
            "namespace Tw.MultiTenancy.Abstractions",
            "namespace Tw.Sharding.Abstractions"
        };
        var violations = Directory.GetFiles(
                RepositoryLayout.BuildingBlocksSrc,
                "*.cs",
                SearchOption.AllDirectories)
            .Where(path => retiredNamespaceDeclarations.Any(
                declaration => File.ReadAllText(path).Contains(declaration, StringComparison.Ordinal)))
            .Select(RepositoryLayout.RepositoryRelativePath)
            .ToArray();

        violations.Should().BeEmpty("retired contract namespaces cannot contribute active source types");
    }

    /// <summary>
    /// 查找仍然存在的历史项目文件或项目目录
    /// </summary>
    /// <param name="relativeProjectPath">仓库相对项目文件路径</param>
    /// <returns>仍然存在的仓库相对路径</returns>
    /// <exception cref="InvalidOperationException">项目路径无法解析目录时抛出</exception>
    private static IEnumerable<string> FindExistingProjectBoundary(string relativeProjectPath)
    {
        var projectPath = Path.Combine(
            RepositoryLayout.Root,
            relativeProjectPath.Replace('/', Path.DirectorySeparatorChar));
        var projectDirectory = Path.GetDirectoryName(projectPath)
            ?? throw new InvalidOperationException("无法解析已退休契约项目目录");

        if (Directory.Exists(projectDirectory))
        {
            yield return RepositoryLayout.RepositoryRelativePath(projectDirectory);
        }

        if (File.Exists(projectPath))
        {
            yield return RepositoryLayout.NormalizePath(relativeProjectPath);
        }
    }

    /// <summary>
    /// 检查单项能力的运行时和测试项目库存
    /// </summary>
    /// <param name="capability">能力名称与目标项目文件</param>
    /// <returns>不符合唯一库存要求的诊断信息</returns>
    private static IEnumerable<string> FindInventoryViolations(
        (string Capability, string RuntimeProject, string TestProject) capability)
    {
        var runtimeRoot = Path.Combine(RepositoryLayout.BuildingBlocksSrc, capability.Capability);
        var testRoot = Path.Combine(RepositoryLayout.BuildingBlocksTests, capability.Capability);
        var runtimeProjects = Directory.GetFiles(runtimeRoot, "*.csproj", SearchOption.AllDirectories)
            .Select(Path.GetFileName)
            .Order(StringComparer.Ordinal)
            .ToArray();
        var testProjects = Directory.GetFiles(testRoot, "*.csproj", SearchOption.AllDirectories)
            .Select(Path.GetFileName)
            .Order(StringComparer.Ordinal)
            .ToArray();

        if (!runtimeProjects.SequenceEqual([capability.RuntimeProject], StringComparer.Ordinal))
        {
            yield return $"{capability.Capability} runtime: {string.Join(", ", runtimeProjects)}";
        }

        if (!testProjects.SequenceEqual([capability.TestProject], StringComparer.Ordinal))
        {
            yield return $"{capability.Capability} tests: {string.Join(", ", testProjects)}";
        }
    }
}
