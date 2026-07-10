namespace Tw.Architecture.Tests;

/// <summary>
/// 提供架构测试访问仓库目录和项目映射的统一入口
/// </summary>
internal static class RepositoryLayout
{
    /// <summary>
    /// 仓库根目录
    /// </summary>
    public static string Root { get; } = FindRepositoryRoot();

    /// <summary>
    /// .NET 工作区根目录
    /// </summary>
    public static string DotnetRoot => Path.Combine(Root, "backend", "dotnet");

    /// <summary>
    /// BuildingBlocks 生产源码根目录
    /// </summary>
    public static string BuildingBlocksSrc => Path.Combine(DotnetRoot, "BuildingBlocks", "src");

    /// <summary>
    /// BuildingBlocks 测试根目录
    /// </summary>
    public static string BuildingBlocksTests => Path.Combine(DotnetRoot, "BuildingBlocks", "tests");

    /// <summary>
    /// .NET tools 根目录
    /// </summary>
    public static string ToolsRoot => Path.Combine(DotnetRoot, "tools");

    /// <summary>
    /// Build 配置根目录
    /// </summary>
    public static string BuildRoot => Path.Combine(DotnetRoot, "Build");

    /// <summary>
    /// 返回生产包名到能力目录名的映射
    /// </summary>
    public static IReadOnlyDictionary<string, string> RuntimeCapabilitiesByPackage()
    {
        return Directory.GetFiles(BuildingBlocksSrc, "*.csproj", SearchOption.AllDirectories)
            .ToDictionary(
                path => Path.GetFileNameWithoutExtension(path)!,
                path => new DirectoryInfo(Path.GetDirectoryName(path)!).Parent!.Name,
                StringComparer.Ordinal);
    }

    /// <summary>
    /// 返回测试项目对应的生产包名
    /// </summary>
    /// <param name="testProjectName">测试项目名称</param>
    /// <returns>被测试的生产包名称</returns>
    /// <exception cref="InvalidOperationException">测试项目名称不符合约定时抛出</exception>
    public static string RuntimePackageNameForTestProject(string testProjectName)
    {
        if (testProjectName.EndsWith(".Tests.Fixtures", StringComparison.Ordinal))
        {
            return testProjectName[..^".Tests.Fixtures".Length];
        }

        if (testProjectName.EndsWith(".Tests", StringComparison.Ordinal))
        {
            return testProjectName[..^".Tests".Length];
        }

        throw new InvalidOperationException($"测试项目名称不符合约定: {testProjectName}");
    }

    /// <summary>
    /// 判断测试项目是否属于 Abstractions 测试项目
    /// </summary>
    /// <param name="testProjectName">测试项目名称</param>
    /// <returns>属于 Abstractions 测试项目时返回 true</returns>
    public static bool IsAbstractionsTestProject(string testProjectName)
    {
        return testProjectName.EndsWith(".Abstractions.Tests", StringComparison.Ordinal);
    }

    /// <summary>
    /// 判断生产项目路径是否属于测试基础包能力目录
    /// </summary>
    /// <param name="projectPath">生产项目文件路径</param>
    /// <returns>项目位于 TestBase 能力目录时返回 true</returns>
    public static bool IsTestBaseRuntimeProject(string projectPath)
    {
        return Path.GetRelativePath(BuildingBlocksSrc, projectPath)
            .Replace('\\', '/')
            .StartsWith("TestBase/", StringComparison.Ordinal);
    }

    /// <summary>
    /// 从测试项目文件路径解析测试能力目录名
    /// </summary>
    /// <param name="testProjectPath">测试项目文件路径</param>
    /// <returns>测试项目所在能力目录名</returns>
    public static string TestCapability(string testProjectPath)
    {
        return Path.GetRelativePath(BuildingBlocksTests, testProjectPath)
            .Replace('\\', '/')
            .Split('/')[0];
    }

    /// <summary>
    /// 查找仓库根目录并返回匹配结果
    /// </summary>
    /// <returns>方法计算得到的文本值</returns>
    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "AGENTS.md")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("无法定位仓库根目录");
    }
}
