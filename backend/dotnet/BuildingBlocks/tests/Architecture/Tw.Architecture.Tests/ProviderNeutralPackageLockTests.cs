using System.Text.Json.Nodes;
using AwesomeAssertions;
using Xunit;

namespace Tw.Architecture.Tests;

/// <summary>
/// 固定 provider-neutral 应用包的 NuGet 锁定依赖边界
/// </summary>
public sealed partial class PackageConsolidationTests
{
    /// <summary>
    /// provider-neutral 应用包的有效锁文件依赖图不包含 Tw.Core 身份
    /// </summary>
    [Fact]
    public void ProviderNeutralApplicationPackageLocks_DoNotContainCorePackage()
    {
        var lockPaths = new[]
        {
            "Application/Tw.Application.Contracts/packages.lock.json",
            "Application/Tw.Domain/packages.lock.json"
        };
        var violations = lockPaths
            .Select(path => Path.Combine(RepositoryLayout.BuildingBlocksSrc, path.Replace('/', Path.DirectorySeparatorChar)))
            .Where(LockFileContainsCorePackage)
            .Select(RepositoryLayout.RepositoryRelativePath)
            .ToArray();

        violations.Should().BeEmpty("provider-neutral package lock graphs must not restore Tw.Core transitively");
    }

    /// <summary>
    /// 判断 NuGet 锁文件任一目标框架依赖图是否包含 Tw.Core
    /// </summary>
    /// <param name="lockPath">待读取的 NuGet 锁文件</param>
    /// <returns>依赖图包含 Tw.Core 时返回 <see langword="true"/></returns>
    private static bool LockFileContainsCorePackage(string lockPath)
    {
        var document = JsonNode.Parse(File.ReadAllText(lockPath))
            ?? throw new InvalidDataException($"NuGet 锁文件为空：{lockPath}");
        var dependencies = document["dependencies"]?.AsObject()
            ?? throw new InvalidDataException($"NuGet 锁文件缺少 dependencies：{lockPath}");

        return dependencies
            .SelectMany(framework => framework.Value?.AsObject() ?? [])
            .Any(dependency => string.Equals(dependency.Key, "Tw.Core", StringComparison.OrdinalIgnoreCase));
    }
}
