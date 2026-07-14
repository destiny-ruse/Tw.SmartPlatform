using AwesomeAssertions;
using Xunit;

namespace Tw.Architecture.Tests;

/// <summary>
/// 验证 Task 10 已淘汰的 HTTP 项目无法通过阶段性白名单重新出现
/// </summary>
public sealed class HttpRetirementBoundaryTests
{
    /// <summary>
    /// Task 10 已删除的两个运行时项目和一个测试项目前身
    /// </summary>
    private static readonly string[] RetiredHttpProjectPaths =
    [
        "backend/dotnet/BuildingBlocks/src/Http/Tw.Http.Abstractions/Tw.Http.Abstractions.csproj",
        "backend/dotnet/BuildingBlocks/src/Http/Tw.Http.Client/Tw.Http.Client.csproj",
        "backend/dotnet/BuildingBlocks/tests/Http/Tw.Http.Client.Tests/Tw.Http.Client.Tests.csproj"
    ];

    /// <summary>
    /// 已淘汰 HTTP 项目的目录和项目文件必须持续不存在
    /// </summary>
    [Fact]
    public void RetiredHttpProjectFilesAndDirectories_DoNotExist()
    {
        var violations = FindReintroducedHttpBoundaries(RepositoryLayout.Root);

        violations.Should().BeEmpty(
            "Task 10 retirement is complete and must not use the phased retired-project allowance");
    }

    /// <summary>
    /// 查找指定仓库根目录下重新出现的 HTTP 项目文件或项目目录
    /// </summary>
    /// <param name="repositoryRoot">需要检查的仓库根目录</param>
    /// <returns>重新出现的仓库相对路径</returns>
    /// <exception cref="InvalidOperationException">无法从已淘汰项目路径解析项目目录时抛出</exception>
    private static IEnumerable<string> FindReintroducedHttpBoundaries(string repositoryRoot)
    {
        foreach (var relativeProjectPath in RetiredHttpProjectPaths)
        {
            var projectPath = Path.Combine(
                repositoryRoot,
                relativeProjectPath.Replace('/', Path.DirectorySeparatorChar));
            var projectDirectory = Path.GetDirectoryName(projectPath)
                ?? throw new InvalidOperationException("无法解析已淘汰 HTTP 项目目录");

            if (Directory.Exists(projectDirectory))
            {
                yield return RepositoryLayout.NormalizePath(
                    Path.GetRelativePath(repositoryRoot, projectDirectory));
            }

            if (File.Exists(projectPath))
            {
                yield return RepositoryLayout.NormalizePath(relativeProjectPath);
            }
        }
    }
}
