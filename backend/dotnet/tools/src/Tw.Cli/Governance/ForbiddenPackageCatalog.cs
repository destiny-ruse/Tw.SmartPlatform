namespace Tw.Cli.Governance;

using System.Text.Json;

/// <summary>
/// 从 BuildingBlocks 拓扑清单提供淘汰包及其替代目标
/// </summary>
public sealed class ForbiddenPackageCatalog
{
    /// <summary>
    /// 拓扑清单相对于仓库根目录的固定路径
    /// </summary>
    private const string TopologyRelativePath = "backend/dotnet/BuildingBlocks/building-blocks-topology.json";

    /// <summary>
    /// 按包标识提供大小写无关的淘汰映射
    /// </summary>
    private readonly IReadOnlyDictionary<string, RetiredPackageRule> _retiredPackagesById;

    /// <summary>
    /// 使用已验证的淘汰映射初始化目录
    /// </summary>
    /// <param name="retiredPackages">拓扑清单中的淘汰包映射</param>
    private ForbiddenPackageCatalog(IReadOnlyList<RetiredPackageRule> retiredPackages)
    {
        RetiredPackages = retiredPackages;
        _retiredPackagesById = retiredPackages.ToDictionary(
            package => package.PackageId,
            StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 拓扑清单声明的淘汰包及替代目标
    /// </summary>
    public IReadOnlyList<RetiredPackageRule> RetiredPackages { get; }

    /// <summary>
    /// 从指定仓库的 BuildingBlocks 拓扑清单加载目录
    /// </summary>
    /// <param name="repositoryPath">包含 backend/dotnet 的仓库根目录</param>
    /// <returns>由仓库唯一拓扑清单构造的淘汰包目录</returns>
    /// <exception cref="GovernanceConfigurationException">仓库或拓扑清单缺失、损坏时抛出</exception>
    public static ForbiddenPackageCatalog Load(string repositoryPath)
    {
        if (!Directory.Exists(repositoryPath))
        {
            throw new GovernanceConfigurationException($"Repository path does not exist: {repositoryPath}");
        }

        var topologyPath = Path.Combine(
            repositoryPath,
            TopologyRelativePath.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(topologyPath))
        {
            throw new GovernanceConfigurationException($"BuildingBlocks topology manifest does not exist: {topologyPath}");
        }

        try
        {
            var topology = JsonSerializer.Deserialize<TopologyManifest>(
                File.ReadAllText(topologyPath),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (topology is null || topology.RetiredPackages.Count == 0)
            {
                throw new GovernanceConfigurationException("BuildingBlocks topology manifest contains no retired packages.");
            }

            if (topology.RetiredPackages.Any(package => string.IsNullOrWhiteSpace(package.PackageId)))
            {
                throw new GovernanceConfigurationException("BuildingBlocks topology manifest contains an empty retired PackageId.");
            }

            var duplicates = topology.RetiredPackages
                .GroupBy(package => package.PackageId, StringComparer.OrdinalIgnoreCase)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .ToArray();
            if (duplicates.Length > 0)
            {
                throw new GovernanceConfigurationException(
                    $"BuildingBlocks topology manifest contains duplicate retired PackageIds: {string.Join(", ", duplicates)}");
            }

            return new ForbiddenPackageCatalog(topology.RetiredPackages
                .Select(package => new RetiredPackageRule(package.PackageId, package.ReplacementPackageId))
                .ToArray());
        }
        catch (JsonException exception)
        {
            throw new GovernanceConfigurationException("BuildingBlocks topology manifest is invalid JSON.", exception);
        }
    }

    /// <summary>
    /// 查找指定包标识对应的淘汰映射
    /// </summary>
    /// <param name="packageId">从 PackageReference 或 ProjectReference 提取的包标识</param>
    /// <param name="rule">匹配时返回淘汰包及替代目标</param>
    /// <returns>包标识属于淘汰拓扑时返回 <see langword="true"/></returns>
    public bool TryGetRetiredPackage(string packageId, out RetiredPackageRule? rule)
    {
        return _retiredPackagesById.TryGetValue(packageId, out rule);
    }

    /// <summary>
    /// 描述拓扑清单中与目录加载相关的 JSON 字段
    /// </summary>
    private sealed class TopologyManifest
    {
        /// <summary>
        /// 淘汰包及替代目标集合
        /// </summary>
        public List<RetiredPackageManifestEntry> RetiredPackages { get; init; } = [];
    }

    /// <summary>
    /// 描述拓扑清单中的单条淘汰映射
    /// </summary>
    private sealed class RetiredPackageManifestEntry
    {
        /// <summary>
        /// 不再允许被引用的包标识
        /// </summary>
        public string PackageId { get; init; } = string.Empty;

        /// <summary>
        /// 调用方应迁移到的保留包标识
        /// </summary>
        public string? ReplacementPackageId { get; init; }
    }
}

/// <summary>
/// 描述淘汰包标识及其可选替代包
/// </summary>
/// <param name="PackageId">不再允许被引用的包标识</param>
/// <param name="ReplacementPackageId">调用方应迁移到的保留包标识</param>
public sealed record RetiredPackageRule(string PackageId, string? ReplacementPackageId);

/// <summary>
/// 表示依赖治理所需仓库配置缺失或损坏
/// </summary>
public sealed class GovernanceConfigurationException : Exception
{
    /// <summary>
    /// 使用可诊断消息初始化配置异常
    /// </summary>
    /// <param name="message">不含敏感数据的配置失败原因</param>
    public GovernanceConfigurationException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// 使用可诊断消息和原始异常初始化配置异常
    /// </summary>
    /// <param name="message">不含敏感数据的配置失败原因</param>
    /// <param name="innerException">触发配置失败的原始异常</param>
    public GovernanceConfigurationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
