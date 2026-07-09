using AwesomeAssertions;
using Xunit;

namespace Tw.Architecture.Tests;

/// <summary>
/// 验证 Build 目录只承载中央包版本与构建级 MSBuild 配置
/// </summary>
public sealed class BuildLayoutTests
{
    /// <summary>
    /// 验证 Build 目录中的文件类型只包含 props 和锁定文件
    /// </summary>
    [Fact]
    public void BuildDirectory_ContainsOnlyPropsAndLockFile()
    {
        var files = Directory.GetFiles(RepositoryLayout.BuildRoot, "*", SearchOption.AllDirectories)
            .Where(path => !Path.GetRelativePath(RepositoryLayout.BuildRoot, path).StartsWith("obj", StringComparison.OrdinalIgnoreCase))
            .Select(path => Path.GetRelativePath(RepositoryLayout.BuildRoot, path).Replace('\\', '/'))
            .ToArray();

        files.Should().OnlyContain(
            path => path.EndsWith(".props", StringComparison.Ordinal) || path == "packages.lock.json",
            "Build is reserved for central MSBuild props and its lock file");
    }

    /// <summary>
    /// 验证 Build 目录不包含占位 runner 和 QualityGates 脚本目录
    /// </summary>
    [Fact]
    public void BuildDirectory_DoesNotContainQualityGatesOrRunnerProject()
    {
        Directory.Exists(Path.Combine(RepositoryLayout.BuildRoot, "QualityGates")).Should().BeFalse();
        File.Exists(Path.Combine(RepositoryLayout.BuildRoot, "Build.cs")).Should().BeFalse();
        File.Exists(Path.Combine(RepositoryLayout.BuildRoot, "Build.csproj")).Should().BeFalse();
    }
}
