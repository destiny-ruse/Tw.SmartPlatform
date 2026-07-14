using System.Collections.Frozen;
using System.Xml.Linq;
using AwesomeAssertions;
using Xunit;

namespace Tw.Architecture.Tests;

/// <summary>
/// 验证 Tw.Resilience 项目、锁文件与包章程保持零依赖边界
/// </summary>
public sealed class ResilienceDependencyBoundaryTests
{
    /// <summary>
    /// MSBuild 中能够向生产程序集引入编译或运行依赖的项目项
    /// </summary>
    private static readonly FrozenSet<string> DependencyItemNames = new[]
    {
        "PackageReference",
        "ProjectReference",
        "FrameworkReference",
        "Reference"
    }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// 项目依赖声明与章程允许列表必须同时保持为空
    /// </summary>
    [Fact]
    public void ProjectAndCharter_DeclareNoDependencies()
    {
        var resilienceRoot = ResilienceRoot();
        var projectDependencies = ReadProjectDependencyItems(
            Path.Combine(resilienceRoot, "Tw.Resilience.csproj"));
        var allowedDependencies = PackageCharterDependencyRules.ReadAllowedDependencies(
            Path.Combine(resilienceRoot, "package-charter.yaml"));

        projectDependencies.Should().BeEmpty(
            "Tw.Resilience 是提供方无关且自包含的策略包");
        allowedDependencies.Should().BeEmpty(
            "Tw.Resilience 包章程必须与项目零依赖边界一致");
        projectDependencies.Should().BeEquivalentTo(allowedDependencies);
    }

    /// <summary>
    /// 锁文件中每个目标框架的直接与传递依赖图必须为空
    /// </summary>
    [Fact]
    public void LockDependencyGraphs_AreEmptyForEveryTargetFramework()
    {
        var lockDependencies = NuGetLockFileDependencies.ReadPackageIdentities(
            Path.Combine(ResilienceRoot(), "packages.lock.json"));

        lockDependencies.Should().BeEmpty(
            "Tw.Resilience 的每个目标框架都不得包含直接或传递依赖");
    }

    /// <summary>
    /// 读取项目文件中全部 MSBuild 依赖项声明并展开分号分隔的身份
    /// </summary>
    /// <param name="projectFile">Tw.Resilience 项目文件路径</param>
    /// <returns>包含项目项类型与依赖身份的诊断集合</returns>
    private static IReadOnlyList<string> ReadProjectDependencyItems(string projectFile)
    {
        var dependencyItems = new List<string>();
        foreach (var item in XDocument.Load(projectFile)
                     .Descendants()
                     .Where(element => DependencyItemNames.Contains(element.Name.LocalName)))
        {
            var identityAttribute = item.Attributes().FirstOrDefault(attribute =>
                string.Equals(attribute.Name.LocalName, "Include", StringComparison.OrdinalIgnoreCase)
                || string.Equals(attribute.Name.LocalName, "Update", StringComparison.OrdinalIgnoreCase)
                || string.Equals(attribute.Name.LocalName, "Remove", StringComparison.OrdinalIgnoreCase));
            var identities = identityAttribute?.Value.Split(
                ';',
                StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries) ?? [];

            if (identities.Length == 0)
            {
                dependencyItems.Add($"{item.Name.LocalName}: <empty identity>");
                continue;
            }

            dependencyItems.AddRange(identities.Select(identity => $"{item.Name.LocalName}: {identity}"));
        }

        return dependencyItems;
    }

    /// <summary>
    /// 定位 Tw.Resilience 生产项目根目录
    /// </summary>
    /// <returns>包含项目文件、锁文件和包章程的绝对目录</returns>
    private static string ResilienceRoot()
    {
        return Path.Combine(
            RepositoryLayout.BuildingBlocksSrc,
            "Resilience",
            "Tw.Resilience");
    }
}
