using System.Xml.Linq;

namespace Tw.Cli.Governance;

/// <summary>
/// 扫描项目依赖并返回治理结果
/// </summary>
public sealed class ProjectDependencyScanner
{
    /// <summary>
    /// 不得进入 Web、应用层或领域层的基础设施提供程序包前缀
    /// </summary>
    private static readonly string[] InfrastructureProviderPrefixes =
    [
        "Autofac",
        "Castle",
        "SqlSugar",
        "SqlSugarCore",
        "DotNetCore.CAP",
        "Quartz",
        "Yarp",
        "StackExchange.Redis",
        "DistributedLock.Redis",
        "Microsoft.Extensions.ServiceDiscovery.Yarp",
        "Tw.Data.SqlSugar",
        "Tw.EventBus.Cap",
        "Tw.BackgroundJobs.Quartz",
        "Tw.Gateway.Yarp",
        "Tw.DistributedLocking.Redis"
    ];

    /// <summary>
    /// 扫描仓库并返回发现的治理问题
    /// </summary>
    /// <param name="repositoryPath">待扫描仓库的根目录路径</param>
    /// <returns>依赖扫描发现的治理违规结果</returns>
    public DependencyScanResult ScanRepository(string repositoryPath)
    {
        var result = new DependencyScanResult();
        if (!Directory.Exists(repositoryPath))
        {
            result.Errors.Add(new DependencyScanError("TWGOV000", repositoryPath, "Repository path does not exist."));
            return result;
        }

        ForbiddenPackageCatalog packageCatalog;
        try
        {
            packageCatalog = ForbiddenPackageCatalog.Load(repositoryPath);
        }
        catch (GovernanceConfigurationException exception)
        {
            result.Errors.Add(new DependencyScanError("TWGOV000", repositoryPath, exception.Message));
            return result;
        }

        foreach (var projectPath in Directory.GetFiles(repositoryPath, "*.csproj", SearchOption.AllDirectories)
                     .Where(path => !IsBuildOutput(path)))
        {
            var projectResult = ScanProjectText(projectPath, File.ReadAllText(projectPath), packageCatalog);
            result.Errors.AddRange(projectResult.Errors);
        }

        return result;
    }

    /// <summary>
    /// 扫描项目文本并返回发现的治理问题
    /// </summary>
    /// <param name="projectPath">待分析项目文件的路径</param>
    /// <param name="projectXml">项目文件的 XML 文本内容</param>
    /// <param name="packageCatalog">从当前仓库拓扑清单加载的淘汰包目录</param>
    /// <returns>依赖扫描发现的治理违规结果</returns>
    public DependencyScanResult ScanProjectText(
        string projectPath,
        string projectXml,
        ForbiddenPackageCatalog packageCatalog)
    {
        ArgumentNullException.ThrowIfNull(packageCatalog);

        var result = new DependencyScanResult();
        XDocument document;
        try
        {
            document = XDocument.Parse(projectXml, LoadOptions.PreserveWhitespace);
        }
        catch (Exception exception) when (exception is System.Xml.XmlException or InvalidOperationException)
        {
            result.Errors.Add(new DependencyScanError("TWGOV000", projectPath, "Project XML is invalid."));
            return result;
        }

        var isProductionProject = IsProductionProject(projectPath);
        var projectName = Path.GetFileNameWithoutExtension(projectPath);
        foreach (var reference in ReadReferences(document))
        {
            var packageId = ReferencePackageId(reference);
            if (packageId.EndsWith("TestBase", StringComparison.OrdinalIgnoreCase)
                || reference.Include.Contains("TestBase", StringComparison.OrdinalIgnoreCase))
            {
                if (isProductionProject)
                {
                    result.Errors.Add(new DependencyScanError(
                        "TWGOV003",
                        projectPath,
                        "Production projects must not reference test base packages."));
                }
            }

            if (packageCatalog.TryGetRetiredPackage(packageId, out var retiredPackage))
            {
                var replacement = retiredPackage!.ReplacementPackageId is null
                    ? "no replacement package"
                    : $"use '{retiredPackage.ReplacementPackageId}'";
                result.Errors.Add(new DependencyScanError(
                    "TWGOV002",
                    projectPath,
                    $"Retired {reference.ReferenceType} '{retiredPackage.PackageId}'; {replacement}."));
            }

            if (!IsInfrastructureProvider(packageId))
            {
                continue;
            }

            if (string.Equals(projectName, "Tw.AspNetCore", StringComparison.OrdinalIgnoreCase))
            {
                result.Errors.Add(new DependencyScanError(
                    "TWGOV004",
                    projectPath,
                    $"Tw.AspNetCore must not reference infrastructure provider '{packageId}'."));
            }

            if (IsApplicationOrDomainProject(projectPath, projectName))
            {
                result.Errors.Add(new DependencyScanError(
                    "TWGOV005",
                    projectPath,
                    $"Application and Domain projects must not reference infrastructure provider '{packageId}'."));
            }
        }

        return result;
    }

    /// <summary>
    /// 读取项目文件中的 PackageReference 和 ProjectReference
    /// </summary>
    /// <param name="document">已经解析的项目 XML 文档</param>
    /// <returns>引用类型及 Include 值集合</returns>
    private static IEnumerable<ProjectReferenceEntry> ReadReferences(XDocument document)
    {
        return document
            .Descendants()
            .Where(element => element.Name.LocalName is "PackageReference" or "ProjectReference")
            .Select(element => new ProjectReferenceEntry(
                element.Name.LocalName,
                element.Attribute("Include")?.Value ?? string.Empty))
            .Where(reference => !string.IsNullOrWhiteSpace(reference.Include));
    }

    /// <summary>
    /// 从不同 MSBuild 引用类型提取规范包标识
    /// </summary>
    /// <param name="reference">待解析的项目引用项</param>
    /// <returns>PackageReference 标识或 ProjectReference 项目文件名</returns>
    private static string ReferencePackageId(ProjectReferenceEntry reference)
    {
        return string.Equals(reference.ReferenceType, "ProjectReference", StringComparison.OrdinalIgnoreCase)
            ? Path.GetFileNameWithoutExtension(reference.Include.Replace('\\', Path.DirectorySeparatorChar))
            : reference.Include;
    }

    /// <summary>
    /// 判断引用是否属于受限基础设施提供程序
    /// </summary>
    /// <param name="packageId">待检查的包标识</param>
    /// <returns>命中受限提供程序前缀时返回 <see langword="true"/></returns>
    private static bool IsInfrastructureProvider(string packageId)
    {
        return InfrastructureProviderPrefixes.Any(prefix =>
            string.Equals(packageId, prefix, StringComparison.OrdinalIgnoreCase)
            || packageId.StartsWith($"{prefix}.", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// 判断项目是否属于应用层或领域层
    /// </summary>
    /// <param name="projectPath">项目文件路径</param>
    /// <param name="projectName">项目文件名去扩展名</param>
    /// <returns>项目处于 Application 能力目录或名称表达 Application/Domain 层时返回 <see langword="true"/></returns>
    private static bool IsApplicationOrDomainProject(string projectPath, string projectName)
    {
        var normalizedPath = projectPath.Replace('\\', '/');
        return normalizedPath.Contains("/Application/", StringComparison.OrdinalIgnoreCase)
            || projectName.EndsWith(".Application", StringComparison.OrdinalIgnoreCase)
            || projectName.Contains(".Application.", StringComparison.OrdinalIgnoreCase)
            || projectName.EndsWith(".Domain", StringComparison.OrdinalIgnoreCase)
            || projectName.Contains(".Domain.", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 判断生产项目是否满足条件
    /// </summary>
    /// <param name="projectPath">待分析项目文件的路径</param>
    /// <returns>项目位于 src 目录时返回 <see langword="true"/></returns>
    private static bool IsProductionProject(string projectPath)
    {
        var normalized = projectPath.Replace('\\', '/');
        var isSourceProject = normalized.Contains("/src/", StringComparison.OrdinalIgnoreCase)
            || normalized.StartsWith("src/", StringComparison.OrdinalIgnoreCase);
        var isBuildingBlocksTestBase = normalized.Contains(
            "BuildingBlocks/src/TestBase/",
            StringComparison.OrdinalIgnoreCase);
        return isSourceProject && !isBuildingBlocksTestBase;
    }

    /// <summary>
    /// 判断路径是否位于编译输出目录
    /// </summary>
    /// <param name="path">待检查的项目路径</param>
    /// <returns>路径位于 bin 或 obj 时返回 <see langword="true"/></returns>
    private static bool IsBuildOutput(string path)
    {
        var normalized = path.Replace('\\', '/');
        return normalized.Contains("/bin/", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("/obj/", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 描述项目文件中的依赖引用类型与原始 Include 值
    /// </summary>
    /// <param name="ReferenceType">PackageReference 或 ProjectReference</param>
    /// <param name="Include">MSBuild Include 属性原始值</param>
    private sealed record ProjectReferenceEntry(string ReferenceType, string Include);
}

/// <summary>
/// 承载依赖扫描处理后的结果数据
/// </summary>
public sealed class DependencyScanResult
{
    /// <summary>
    /// 依赖扫描发现的治理违规列表
    /// </summary>
    public List<DependencyScanError> Errors { get; } = [];
}

/// <summary>
/// 描述依赖扫描过程中发现的错误项
/// </summary>
/// <param name="Code">稳定的治理错误码</param>
/// <param name="ProjectPath">触发治理错误的项目或仓库路径</param>
/// <param name="Message">不含敏感数据的可执行失败原因</param>
public sealed record DependencyScanError(string Code, string ProjectPath, string Message);
