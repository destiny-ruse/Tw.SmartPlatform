using AwesomeAssertions;
using Xunit;

namespace Tw.Architecture.Tests;

/// <summary>
/// 固定多租户与分片包的零依赖边界
/// </summary>
public sealed partial class PackageConsolidationTests
{
    /// <summary>
    /// 能够向生产程序集引入编译或运行依赖的 MSBuild 项目项
    /// </summary>
    private static readonly string[] DependencyItemNames =
    [
        "PackageReference",
        "ProjectReference",
        "FrameworkReference",
        "Reference"
    ];

    /// <summary>
    /// 第十一项任务合并后必须保持零依赖的两个包
    /// </summary>
    private static readonly (string Capability, string PackageId)[] ZeroDependencyPackages =
    [
        ("MultiTenancy", "Tw.MultiTenancy"),
        ("Sharding", "Tw.Sharding")
    ];

    /// <summary>
    /// 项目文件、所有目标框架锁图与包章程必须共同声明零依赖
    /// </summary>
    [Fact]
    public void TenancyAndShardingPackages_HaveZeroDependencyBoundaries()
    {
        var violations = ZeroDependencyPackages
            .SelectMany(FindZeroDependencyViolations)
            .ToArray();

        violations.Should().BeEmpty("多租户与分片核心包必须保持提供方无关的零依赖边界");
    }

    /// <summary>
    /// 检查单个核心包的项目、锁文件和章程依赖声明
    /// </summary>
    /// <param name="package">能力目录与包标识</param>
    /// <returns>破坏零依赖边界的诊断信息</returns>
    private static IEnumerable<string> FindZeroDependencyViolations(
        (string Capability, string PackageId) package)
    {
        var packageRoot = Path.Combine(
            RepositoryLayout.BuildingBlocksSrc,
            package.Capability,
            package.PackageId);
        var projectPath = Path.Combine(packageRoot, $"{package.PackageId}.csproj");
        var lockPath = Path.Combine(packageRoot, "packages.lock.json");
        var charterPath = Path.Combine(packageRoot, "package-charter.yaml");

        foreach (var dependencyItem in MsBuildProjectItems.Read(projectPath, DependencyItemNames))
        {
            yield return $"{package.PackageId} 项目依赖项：{dependencyItem.ItemName} {dependencyItem.Include}";
        }

        foreach (var lockDependency in NuGetLockFileDependencies.ReadPackageIdentities(lockPath))
        {
            yield return $"{package.PackageId} 锁文件依赖：{lockDependency}";
        }

        foreach (var allowedDependency in PackageCharterDependencyRules.ReadAllowedDependencies(charterPath))
        {
            yield return $"{package.PackageId} 章程允许依赖：{allowedDependency}";
        }
    }
}
