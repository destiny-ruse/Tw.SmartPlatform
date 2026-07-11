namespace Tw.Architecture.Tests;

using System.Text.Json;

/// <summary>
/// 提供架构测试访问仓库目录和项目映射的统一入口
/// </summary>
internal static class RepositoryLayout
{
    /// <summary>
    /// 读取拓扑清单时使用的 JSON 属性匹配规则
    /// </summary>
    private static readonly JsonSerializerOptions TopologyJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// 仓库根目录
    /// </summary>
    public static string Root { get; } = FindRepositoryRoot();

    /// <summary>
    /// .NET 工作区根目录
    /// </summary>
    public static string DotnetRoot => Path.Combine(Root, "backend", "dotnet");

    /// <summary>
    /// .NET 解决方案文件
    /// </summary>
    public static string SolutionFile => Path.Combine(DotnetRoot, "Tw.SmartPlatform.slnx");

    /// <summary>
    /// BuildingBlocks 生产源码根目录
    /// </summary>
    public static string BuildingBlocksSrc => Path.Combine(DotnetRoot, "BuildingBlocks", "src");

    /// <summary>
    /// BuildingBlocks 测试根目录
    /// </summary>
    public static string BuildingBlocksTests => Path.Combine(DotnetRoot, "BuildingBlocks", "tests");

    /// <summary>
    /// BuildingBlocks 目标拓扑清单文件
    /// </summary>
    public static string BuildingBlocksTopologyFile => Path.Combine(DotnetRoot, "BuildingBlocks", "building-blocks-topology.json");

    /// <summary>
    /// .NET tools 根目录
    /// </summary>
    public static string ToolsRoot => Path.Combine(DotnetRoot, "tools");

    /// <summary>
    /// Build 配置根目录
    /// </summary>
    public static string BuildRoot => Path.Combine(DotnetRoot, "Build");

    /// <summary>
    /// 已验证的 BuildingBlocks 目标拓扑
    /// </summary>
    internal static BuildingBlocksTopology Topology { get; } = LoadTopology(BuildingBlocksTopologyFile);

    /// <summary>
    /// 将路径分隔符规范化为仓库约定的正斜杠
    /// </summary>
    /// <param name="path">需要规范化的路径</param>
    /// <returns>使用正斜杠表示的路径</returns>
    public static string NormalizePath(string path)
    {
        return path.Replace('\\', '/');
    }

    /// <summary>
    /// 计算指定路径相对于仓库根目录的规范化路径
    /// </summary>
    /// <param name="path">绝对路径或相对于仓库根目录的路径</param>
    /// <returns>相对于仓库根目录的正斜杠路径</returns>
    public static string RepositoryRelativePath(string path)
    {
        return RelativePath(Root, path);
    }

    /// <summary>
    /// 计算指定路径相对于 .NET 工作区的规范化路径
    /// </summary>
    /// <param name="path">绝对路径或相对于 .NET 工作区的路径</param>
    /// <returns>相对于 .NET 工作区的正斜杠路径</returns>
    public static string DotnetRelativePath(string path)
    {
        return RelativePath(DotnetRoot, path);
    }

    /// <summary>
    /// 计算指定项目相对于解决方案文件所在目录的规范化路径
    /// </summary>
    /// <param name="path">绝对路径或相对于 .NET 工作区的路径</param>
    /// <returns>可直接写入 .slnx Project Path 的正斜杠路径</returns>
    public static string SolutionRelativePath(string path)
    {
        return DotnetRelativePath(path);
    }

    /// <summary>
    /// 返回生产包名到能力目录名的映射
    /// </summary>
    public static IReadOnlyDictionary<string, string> RuntimeCapabilitiesByPackage()
    {
        return Directory.GetFiles(BuildingBlocksSrc, "*.csproj", SearchOption.AllDirectories)
            .ToDictionary(
                path => Path.GetFileNameWithoutExtension(path)!,
                SourceCapability,
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
        if (testProjectName == "Tw.DependencyInjection.Tests.Fixtures")
        {
            return "Tw.DependencyInjection";
        }

        if (testProjectName.EndsWith(".Tests", StringComparison.Ordinal))
        {
            return testProjectName[..^".Tests".Length];
        }

        throw new InvalidOperationException($"测试项目名称不符合约定: {testProjectName}");
    }

    /// <summary>
    /// 从生产项目文件路径解析能力目录名称
    /// </summary>
    /// <param name="runtimeProjectPath">生产项目文件路径</param>
    /// <returns>生产项目所在的能力目录名称</returns>
    public static string SourceCapability(string runtimeProjectPath)
    {
        return CapabilityForProjectPath(BuildingBlocksSrc, runtimeProjectPath);
    }

    /// <summary>
    /// 判断生产项目路径是否属于测试基础包能力目录
    /// </summary>
    /// <param name="projectPath">生产项目文件路径</param>
    /// <returns>项目位于 TestBase 能力目录时返回 true</returns>
    public static bool IsTestBaseRuntimeProject(string projectPath)
    {
        return SourceCapability(projectPath) == "TestBase";
    }

    /// <summary>
    /// 从测试项目文件路径解析测试能力目录名
    /// </summary>
    /// <param name="testProjectPath">测试项目文件路径</param>
    /// <returns>测试项目所在能力目录名</returns>
    public static string TestCapability(string testProjectPath)
    {
        return CapabilityForProjectPath(BuildingBlocksTests, testProjectPath);
    }

    /// <summary>
    /// 从能力目录中的项目路径提取第一层能力名称
    /// </summary>
    /// <param name="projectsRoot">生产或测试项目根目录</param>
    /// <param name="projectPath">待解析的项目文件路径</param>
    /// <returns>项目所属能力目录名称</returns>
    /// <exception cref="InvalidOperationException">路径不符合三层能力目录结构时抛出</exception>
    private static string CapabilityForProjectPath(string projectsRoot, string projectPath)
    {
        var relative = RelativePath(projectsRoot, projectPath);
        var segments = relative.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length != 3 || !segments[2].EndsWith(".csproj", StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"项目路径不符合能力目录结构: {relative}");
        }

        return segments[0];
    }

    /// <summary>
    /// 将绝对路径或相对路径转换为指定根目录下的规范化相对路径
    /// </summary>
    /// <param name="basePath">相对路径的计算基准目录</param>
    /// <param name="path">绝对路径或相对于基准目录的路径</param>
    /// <returns>相对于基准目录的正斜杠路径</returns>
    private static string RelativePath(string basePath, string path)
    {
        var absolutePath = Path.IsPathRooted(path)
            ? Path.GetFullPath(path)
            : Path.GetFullPath(path, basePath);
        return NormalizePath(Path.GetRelativePath(basePath, absolutePath));
    }

    /// <summary>
    /// 从指定 JSON 文件加载并校验 BuildingBlocks 的目标拓扑
    /// </summary>
    /// <param name="topologyFile">需要读取的拓扑清单文件</param>
    /// <returns>满足清单约束的目标拓扑</returns>
    /// <exception cref="InvalidOperationException">清单缺失、无法解析或违反结构约束时抛出</exception>
    internal static BuildingBlocksTopology LoadTopology(string topologyFile)
    {
        if (!File.Exists(topologyFile))
        {
            throw new InvalidOperationException($"未找到 BuildingBlocks 拓扑清单: {RepositoryRelativePath(topologyFile)}");
        }

        var topology = JsonSerializer.Deserialize<BuildingBlocksTopology>(
            File.ReadAllText(topologyFile),
            TopologyJsonOptions)
            ?? throw new InvalidOperationException("BuildingBlocks 拓扑清单为空或无法解析");

        ValidateTopology(topology);
        return topology;
    }

    /// <summary>
    /// 验证清单中的路径、映射和去重约束，避免测试依据损坏的库存运行
    /// </summary>
    /// <param name="topology">从 JSON 文件加载的目标拓扑</param>
    /// <exception cref="InvalidOperationException">清单不满足拓扑契约时抛出</exception>
    private static void ValidateTopology(BuildingBlocksTopology topology)
    {
        if (topology.SchemaVersion != 1)
        {
            throw new InvalidOperationException($"不支持的 BuildingBlocks 拓扑清单版本: {topology.SchemaVersion}");
        }

        if (topology.RuntimeProjects.Count == 0 || topology.TestProjects.Count == 0)
        {
            throw new InvalidOperationException("BuildingBlocks 拓扑清单必须包含运行时和测试目标项目");
        }

        foreach (var project in topology.RuntimeProjects)
        {
            ValidateCapabilityRelativeProjectPath(project.Path, "runtimeProjects");
            if (string.IsNullOrWhiteSpace(project.RootNamespace))
            {
                throw new InvalidOperationException($"运行时目标缺少根命名空间: {project.Path}");
            }
        }

        foreach (var project in topology.TestProjects)
        {
            ValidateCapabilityRelativeProjectPath(project.Path, "testProjects");
        }

        EnsureDistinct(topology.RuntimeProjects.Select(project => project.Path), "runtimeProjects 路径");
        EnsureDistinct(topology.TestProjects.Select(project => project.Path), "testProjects 路径");
        EnsureDistinct(topology.ToolProjects, "toolProjects 路径");
        EnsureDistinct(topology.IndependentContractPackages, "independentContractPackages 包名");
        EnsureDistinct(topology.RetiredPackages.Select(package => package.PackageId), "retiredPackages 包名");

        foreach (var toolProject in topology.ToolProjects)
        {
            ValidateToolProjectPath(toolProject);
        }

        var targetRuntimePackageIds = topology.RuntimeProjects
            .Select(project => Path.GetFileNameWithoutExtension(project.Path))
            .ToHashSet(StringComparer.Ordinal);
        if (topology.IndependentContractPackages.Any(package => !targetRuntimePackageIds.Contains(package)))
        {
            throw new InvalidOperationException("独立契约包必须指向运行时目标项目");
        }

        var invalidReplacementPackageIds = topology.RetiredPackages
            .Select(package => package.ReplacementPackageId)
            .Where(packageId => packageId is not null)
            .Cast<string>()
            .Where(packageId => !targetRuntimePackageIds.Contains(packageId))
            .ToArray();
        if (invalidReplacementPackageIds.Length > 0)
        {
            throw new InvalidOperationException($"替代运行时包必须是运行时目标项目: {string.Join(", ", invalidReplacementPackageIds)}");
        }

        var targetTestPaths = topology.TestProjects
            .Select(project => project.Path)
            .ToHashSet(StringComparer.Ordinal);
        foreach (var retiredPackage in topology.RetiredPackages)
        {
            ValidateRetiredPackage(retiredPackage, targetTestPaths);
        }

        var retiredRuntimePaths = topology.RetiredPackages
            .Select(package => package.RuntimeProjectPath)
            .Where(path => path is not null)
            .Cast<string>()
            .ToArray();
        var retiredTestPaths = topology.RetiredPackages
            .Select(package => package.TestProjectPath)
            .Where(path => path is not null)
            .Cast<string>()
            .ToArray();
        EnsureDistinct(retiredRuntimePaths, "retiredPackages 运行时项目路径");
        EnsureDistinct(retiredTestPaths, "retiredPackages 测试项目路径");

        if (retiredRuntimePaths.Intersect(topology.RuntimeProjects.Select(project => project.Path), StringComparer.Ordinal).Any()
            || retiredTestPaths.Intersect(topology.TestProjects.Select(project => project.Path), StringComparer.Ordinal).Any())
        {
            throw new InvalidOperationException("目标项目和淘汰项目不得使用相同路径");
        }
    }

    /// <summary>
    /// 校验淘汰项目映射的路径、替代测试和命名空间信息
    /// </summary>
    /// <param name="retiredPackage">需要校验的淘汰项目映射</param>
    /// <param name="targetTestPaths">清单中的目标测试项目路径集合</param>
    /// <exception cref="InvalidOperationException">淘汰项目映射缺失必要信息时抛出</exception>
    private static void ValidateRetiredPackage(RetiredPackageTopology retiredPackage, ISet<string> targetTestPaths)
    {
        if (string.IsNullOrWhiteSpace(retiredPackage.PackageId))
        {
            throw new InvalidOperationException("淘汰项目映射缺少 PackageId");
        }

        if (retiredPackage.RuntimeProjectPath is not null)
        {
            ValidateCapabilityRelativeProjectPath(retiredPackage.RuntimeProjectPath, "retiredPackages runtimeProjectPath");
            if (!string.Equals(
                    Path.GetFileNameWithoutExtension(retiredPackage.RuntimeProjectPath),
                    retiredPackage.PackageId,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"淘汰项目路径与 PackageId 不一致: {retiredPackage.PackageId}");
            }
        }

        if (retiredPackage.TestProjectPath is not null)
        {
            ValidateCapabilityRelativeProjectPath(retiredPackage.TestProjectPath, "retiredPackages testProjectPath");
        }
        else if (retiredPackage.ReplacementTestProjectPath is not null)
        {
            throw new InvalidOperationException($"没有迁移测试项目时不得设置替代测试路径: {retiredPackage.PackageId}");
        }

        if (retiredPackage.ReplacementTestProjectPath is not null
            && !targetTestPaths.Contains(retiredPackage.ReplacementTestProjectPath))
        {
            throw new InvalidOperationException($"替代测试项目必须是目标测试项目: {retiredPackage.ReplacementTestProjectPath}");
        }

        if (retiredPackage.RetiredNamespaces.Count == 0
            || retiredPackage.RetiredNamespaces.Any(string.IsNullOrWhiteSpace))
        {
            throw new InvalidOperationException($"淘汰项目必须列出命名空间: {retiredPackage.PackageId}");
        }

        EnsureDistinct(retiredPackage.RetiredNamespaces, $"{retiredPackage.PackageId} 的淘汰命名空间");
    }

    /// <summary>
    /// 校验能力相对项目路径保持 capability/project/project.csproj 结构
    /// </summary>
    /// <param name="projectPath">待校验的相对项目路径</param>
    /// <param name="collectionName">记录路径的清单集合名称</param>
    /// <exception cref="InvalidOperationException">路径不符合能力目录契约时抛出</exception>
    private static void ValidateCapabilityRelativeProjectPath(string projectPath, string collectionName)
    {
        if (string.IsNullOrWhiteSpace(projectPath))
        {
            throw new InvalidOperationException($"{collectionName} 不得包含空项目路径");
        }

        var segments = projectPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (Path.IsPathRooted(projectPath)
            || projectPath.Contains('\\')
            || segments.Length != 3
            || segments.Any(segment => segment is "." or "..")
            || !string.Equals(segments[2], $"{segments[1]}.csproj", StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"{collectionName} 包含无效能力项目路径: {projectPath}");
        }
    }

    /// <summary>
    /// 校验工具项目路径保持 tools/src/project/project.csproj 结构
    /// </summary>
    /// <param name="projectPath">待校验的仓库相对工具项目路径</param>
    /// <exception cref="InvalidOperationException">路径不符合工具项目目录契约时抛出</exception>
    private static void ValidateToolProjectPath(string projectPath)
    {
        if (string.IsNullOrWhiteSpace(projectPath))
        {
            throw new InvalidOperationException("toolProjects 不得包含空项目路径");
        }

        var segments = projectPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (Path.IsPathRooted(projectPath)
            || projectPath.Contains('\\')
            || segments.Length != 6
            || !segments.Take(4).SequenceEqual(["backend", "dotnet", "tools", "src"], StringComparer.Ordinal)
            || !string.Equals(segments[5], $"{segments[4]}.csproj", StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"toolProjects 包含无效工具项目路径: {projectPath}");
        }
    }

    /// <summary>
    /// 校验清单集合中的字符串值非空且不存在重复项
    /// </summary>
    /// <param name="values">需要检查的字符串集合</param>
    /// <param name="collectionName">用于诊断信息的集合名称</param>
    /// <exception cref="InvalidOperationException">集合包含空值或重复值时抛出</exception>
    private static void EnsureDistinct(IEnumerable<string> values, string collectionName)
    {
        var entries = values.ToArray();
        if (entries.Any(string.IsNullOrWhiteSpace))
        {
            throw new InvalidOperationException($"{collectionName} 不得包含空值");
        }

        var duplicates = entries
            .GroupBy(value => value, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToArray();
        if (duplicates.Length > 0)
        {
            throw new InvalidOperationException($"{collectionName} 包含重复值: {string.Join(", ", duplicates)}");
        }
    }

    /// <summary>
    /// 查找仓库根目录并返回匹配结果
    /// </summary>
    /// <returns>包含 AGENTS.md 的仓库根目录绝对路径</returns>
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

/// <summary>
/// 描述 BuildingBlocks 目标项目、工具项目和淘汰映射的 JSON 清单模型
/// </summary>
internal sealed class BuildingBlocksTopology
{
    /// <summary>
    /// 清单结构的兼容版本
    /// </summary>
    public int SchemaVersion { get; init; }

    /// <summary>
    /// 必须长期保留的运行时项目及其根命名空间
    /// </summary>
    public List<RuntimeProjectTopology> RuntimeProjects { get; init; } = [];

    /// <summary>
    /// 目标测试项目路径
    /// </summary>
    public List<TestProjectTopology> TestProjects { get; init; } = [];

    /// <summary>
    /// 受解决方案治理的 .NET 工具项目路径
    /// </summary>
    public List<string> ToolProjects { get; init; } = [];

    /// <summary>
    /// 可以独立作为契约包存在的运行时包标识
    /// </summary>
    public List<string> IndependentContractPackages { get; init; } = [];

    /// <summary>
    /// 逐步删除期间允许存在的淘汰项目映射
    /// </summary>
    public List<RetiredPackageTopology> RetiredPackages { get; init; } = [];
}

/// <summary>
/// 声明一个必须保留的运行时项目及其批准根命名空间
/// </summary>
internal sealed class RuntimeProjectTopology
{
    /// <summary>
    /// 相对于 BuildingBlocks/src 的能力项目路径
    /// </summary>
    public string Path { get; init; } = string.Empty;

    /// <summary>
    /// 项目源文件应使用的根命名空间
    /// </summary>
    public string RootNamespace { get; init; } = string.Empty;
}

/// <summary>
/// 声明一个目标测试项目的能力路径
/// </summary>
internal sealed class TestProjectTopology
{
    /// <summary>
    /// 相对于 BuildingBlocks/tests 的能力项目路径
    /// </summary>
    public string Path { get; init; } = string.Empty;
}

/// <summary>
/// 描述淘汰包、替代包、迁移测试和需要清理的命名空间
/// </summary>
internal sealed class RetiredPackageTopology
{
    /// <summary>
    /// 不再允许新增引用的历史包标识
    /// </summary>
    public string PackageId { get; init; } = string.Empty;

    /// <summary>
    /// 承接保留能力的目标包标识，没有替代包时为 null
    /// </summary>
    public string? ReplacementPackageId { get; init; }

    /// <summary>
    /// 相对于 BuildingBlocks/src 的历史运行时项目路径，不存在物理项目时为 null
    /// </summary>
    public string? RuntimeProjectPath { get; init; }

    /// <summary>
    /// 相对于 BuildingBlocks/tests 的迁移中测试项目路径，没有测试项目时为 null
    /// </summary>
    public string? TestProjectPath { get; init; }

    /// <summary>
    /// 迁移完成后应承接测试职责的目标测试项目路径，没有对应测试时为 null
    /// </summary>
    public string? ReplacementTestProjectPath { get; init; }

    /// <summary>
    /// 删除历史项目后不再允许继续贡献类型的命名空间
    /// </summary>
    public List<string> RetiredNamespaces { get; init; } = [];
}
