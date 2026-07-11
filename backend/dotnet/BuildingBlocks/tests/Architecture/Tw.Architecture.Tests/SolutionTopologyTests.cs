using System.Xml.Linq;
using AwesomeAssertions;
using Xunit;

namespace Tw.Architecture.Tests;

/// <summary>
/// 验证解决方案中的 BuildingBlocks 项目清单与物理能力目录保持一致
/// </summary>
public sealed class SolutionTopologyTests
{
    /// <summary>
    /// 验证每个物理 BuildingBlocks 项目在解决方案中恰好列出一次
    /// </summary>
    [Fact]
    public void Solution_BuildingBlocksProjects_AreListedExactlyOnce()
    {
        var physicalProjectPaths = PhysicalBuildingBlocksProjects()
            .Select(project => project.SolutionPath)
            .ToHashSet(StringComparer.Ordinal);
        var solutionProjectPaths = XDocument.Load(RepositoryLayout.SolutionFile)
            .Descendants("Project")
            .Select(element => RepositoryLayout.NormalizePath(element.Attribute("Path")?.Value ?? string.Empty))
            .Where(IsBuildingBlocksProjectPath)
            .ToArray();
        var duplicates = solutionProjectPaths
            .GroupBy(path => path, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToArray();
        var missing = physicalProjectPaths.Except(solutionProjectPaths, StringComparer.Ordinal).ToArray();
        var unknown = solutionProjectPaths.Except(physicalProjectPaths, StringComparer.Ordinal).ToArray();

        duplicates.Should().BeEmpty("each BuildingBlocks project must have one solution entry");
        missing.Should().BeEmpty("every physical BuildingBlocks project must be listed by the solution");
        unknown.Should().BeEmpty("the solution must not retain removed or invented BuildingBlocks project paths");
    }

    /// <summary>
    /// 验证解决方案文件夹由每个物理项目所在的能力目录推导
    /// </summary>
    [Fact]
    public void Solution_BuildingBlocksProjectFolders_MirrorPhysicalCapabilityFolders()
    {
        var solution = XDocument.Load(RepositoryLayout.SolutionFile);
        var projectsByPath = solution.Descendants("Project")
            .Select(element => new
            {
                Element = element,
                Path = RepositoryLayout.NormalizePath(element.Attribute("Path")?.Value ?? string.Empty)
            })
            .Where(project => IsBuildingBlocksProjectPath(project.Path))
            .GroupBy(project => project.Path, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.Ordinal);
        var violations = new List<string>();

        foreach (var physicalProject in PhysicalBuildingBlocksProjects())
        {
            if (!projectsByPath.TryGetValue(physicalProject.SolutionPath, out var solutionProjects)
                || solutionProjects.Length != 1)
            {
                violations.Add($"{physicalProject.SolutionPath} must have exactly one solution entry before its folder can be checked");
                continue;
            }

            var expectedFolder = ExpectedCapabilityFolder(physicalProject.FilePath);
            var actualFolder = solutionProjects[0].Element
                .Ancestors("Folder")
                .FirstOrDefault()
                ?.Attribute("Name")
                ?.Value;
            if (!string.Equals(actualFolder, expectedFolder, StringComparison.Ordinal))
            {
                violations.Add($"{physicalProject.SolutionPath} expected {expectedFolder} but was {actualFolder ?? "without a containing folder"}");
            }
        }

        violations.Should().BeEmpty("solution folders must mirror BuildingBlocks/src/<Capability> and BuildingBlocks/tests/<Capability>");
    }

    /// <summary>
    /// 返回 BuildingBlocks 物理项目及其相对于解决方案目录的路径
    /// </summary>
    /// <returns>生产和测试项目的绝对路径与解决方案相对路径集合</returns>
    private static IEnumerable<(string FilePath, string SolutionPath)> PhysicalBuildingBlocksProjects()
    {
        return Directory.GetFiles(RepositoryLayout.BuildingBlocksSrc, "*.csproj", SearchOption.AllDirectories)
            .Concat(Directory.GetFiles(RepositoryLayout.BuildingBlocksTests, "*.csproj", SearchOption.AllDirectories))
            .Select(projectPath => (projectPath, RepositoryLayout.SolutionRelativePath(projectPath)));
    }

    /// <summary>
    /// 判断解决方案项目路径是否属于 BuildingBlocks 生产或测试目录
    /// </summary>
    /// <param name="solutionPath">相对于 .NET 工作区的解决方案项目路径</param>
    /// <returns>路径属于 BuildingBlocks 项目时返回 true</returns>
    private static bool IsBuildingBlocksProjectPath(string solutionPath)
    {
        return solutionPath.StartsWith("BuildingBlocks/src/", StringComparison.Ordinal)
            || solutionPath.StartsWith("BuildingBlocks/tests/", StringComparison.Ordinal);
    }

    /// <summary>
    /// 根据物理项目路径构造该项目必须所属的解决方案能力文件夹名称
    /// </summary>
    /// <param name="projectPath">生产或测试项目的绝对路径</param>
    /// <returns>以斜杠包围的解决方案能力文件夹名称</returns>
    /// <exception cref="InvalidOperationException">路径不属于 BuildingBlocks 生产或测试目录时抛出</exception>
    private static string ExpectedCapabilityFolder(string projectPath)
    {
        var solutionPath = RepositoryLayout.SolutionRelativePath(projectPath);
        if (solutionPath.StartsWith("BuildingBlocks/src/", StringComparison.Ordinal))
        {
            return $"/BuildingBlocks/src/{RepositoryLayout.SourceCapability(projectPath)}/";
        }

        if (solutionPath.StartsWith("BuildingBlocks/tests/", StringComparison.Ordinal))
        {
            return $"/BuildingBlocks/tests/{RepositoryLayout.TestCapability(projectPath)}/";
        }

        throw new InvalidOperationException($"项目路径不属于 BuildingBlocks: {RepositoryLayout.RepositoryRelativePath(projectPath)}");
    }
}
