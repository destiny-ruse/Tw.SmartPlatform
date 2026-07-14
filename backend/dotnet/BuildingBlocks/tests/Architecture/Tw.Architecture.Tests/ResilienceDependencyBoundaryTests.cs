using System.Text.Json.Nodes;
using System.Xml.Linq;
using AwesomeAssertions;
using Xunit;

namespace Tw.Architecture.Tests;

/// <summary>
/// 验证 Tw.Resilience 项目与锁文件保持 provider-neutral
/// </summary>
public sealed class ResilienceDependencyBoundaryTests
{
    /// <summary>
    /// 项目直接依赖与所有目标框架锁依赖均不得出现 HTTP 韧性 provider
    /// </summary>
    [Fact]
    public void ProjectAndLockDependencies_DoNotContainHttpProviderIdentities()
    {
        var resilienceRoot = Path.Combine(
            RepositoryLayout.BuildingBlocksSrc,
            "Resilience",
            "Tw.Resilience");
        var projectFile = Path.Combine(resilienceRoot, "Tw.Resilience.csproj");
        var lockFile = Path.Combine(resilienceRoot, "packages.lock.json");

        var violations = ReadProjectPackageIdentities(projectFile)
            .Select(identity => $"Tw.Resilience.csproj: {identity}")
            .Concat(ReadLockPackageIdentities(lockFile)
                .Select(identity => $"packages.lock.json: {identity}"))
            .Where(entry => IsForbiddenProviderIdentity(entry[(entry.IndexOf(':') + 1)..].Trim()))
            .ToArray();

        violations.Should().BeEmpty(
            "Tw.Resilience must not acquire HTTP, DI HTTP registration, or third-party resilience providers");
    }

    /// <summary>
    /// 读取项目文件中 Include 或 Update 声明的包标识
    /// </summary>
    /// <param name="projectFile">Tw.Resilience 项目文件路径</param>
    /// <returns>项目直接声明的包标识</returns>
    private static IEnumerable<string> ReadProjectPackageIdentities(string projectFile)
    {
        return XDocument.Load(projectFile)
            .Descendants("PackageReference")
            .Select(reference => reference.Attribute("Include")?.Value
                ?? reference.Attribute("Update")?.Value)
            .Where(identity => !string.IsNullOrWhiteSpace(identity))
            .Cast<string>();
    }

    /// <summary>
    /// 读取锁文件全部目标框架下的直接与传递依赖标识
    /// </summary>
    /// <param name="lockFile">Tw.Resilience 锁文件路径</param>
    /// <returns>所有目标框架记录的依赖标识</returns>
    private static IEnumerable<string> ReadLockPackageIdentities(string lockFile)
    {
        var lockRoot = JsonNode.Parse(File.ReadAllText(lockFile))?.AsObject()
            ?? throw new InvalidOperationException("无法解析 Tw.Resilience 锁文件");
        var targetFrameworks = lockRoot["dependencies"]?.AsObject()
            ?? throw new InvalidOperationException("Tw.Resilience 锁文件缺少 dependencies");

        return targetFrameworks
            .SelectMany(targetFramework => targetFramework.Value?.AsObject().Select(package => package.Key)
                ?? []);
    }

    /// <summary>
    /// 判断包标识是否属于禁止引入的 HTTP 或第三方韧性 provider
    /// </summary>
    /// <param name="identity">项目或锁文件中的包标识</param>
    /// <returns>包标识属于禁止边界时返回 <see langword="true"/></returns>
    private static bool IsForbiddenProviderIdentity(string identity)
    {
        return identity.StartsWith("Polly", StringComparison.OrdinalIgnoreCase)
            || identity.StartsWith("Microsoft.Extensions.Http", StringComparison.OrdinalIgnoreCase)
            || identity.StartsWith("System.Net.Http", StringComparison.OrdinalIgnoreCase);
    }
}
