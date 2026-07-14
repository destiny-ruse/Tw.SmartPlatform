using System.Text.Json.Nodes;

namespace Tw.Architecture.Tests;

/// <summary>
/// 读取 NuGet 锁文件全部目标框架的依赖身份
/// </summary>
internal static class NuGetLockFileDependencies
{
    /// <summary>
    /// 读取每个目标框架下的直接、传递与集中传递依赖
    /// </summary>
    /// <param name="lockFile">需要检查的 NuGet 锁文件</param>
    /// <returns>包含目标框架与包身份的依赖集合</returns>
    /// <exception cref="InvalidOperationException">锁文件无法解析或 dependencies 图结构无效时抛出</exception>
    internal static IReadOnlyList<string> ReadPackageIdentities(string lockFile)
    {
        var lockRoot = JsonNode.Parse(File.ReadAllText(lockFile))?.AsObject()
            ?? throw new InvalidOperationException($"无法解析 NuGet 锁文件：{lockFile}");
        var targetFrameworks = lockRoot["dependencies"]?.AsObject()
            ?? throw new InvalidOperationException($"NuGet 锁文件缺少 dependencies：{lockFile}");
        var dependencyIdentities = new List<string>();

        foreach (var targetFramework in targetFrameworks)
        {
            if (targetFramework.Value is not JsonObject dependencies)
            {
                throw new InvalidOperationException(
                    $"NuGet 锁文件目标框架依赖图无效：{lockFile}；{targetFramework.Key}");
            }

            dependencyIdentities.AddRange(
                dependencies.Select(package => $"{targetFramework.Key}: {package.Key}"));
        }

        return dependencyIdentities;
    }
}
