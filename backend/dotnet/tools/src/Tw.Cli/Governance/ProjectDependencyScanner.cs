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
                     .Where(path => !IsIgnoredProjectPath(path)))
        {
            var projectResult = ScanProjectFile(projectPath, repositoryPath, packageCatalog);
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

        ScanDocument(projectPath, document, packageCatalog, result);
        return result;
    }

    /// <summary>
    /// 扫描项目及适用的自动与显式导入文件
    /// </summary>
    /// <param name="projectPath">待分析项目文件路径</param>
    /// <param name="repositoryPath">限制导入读取范围的仓库根目录</param>
    /// <param name="packageCatalog">当前仓库淘汰包目录</param>
    /// <returns>项目及其导入文件产生的治理结果</returns>
    private static DependencyScanResult ScanProjectFile(
        string projectPath,
        string repositoryPath,
        ForbiddenPackageCatalog packageCatalog)
    {
        var result = new DependencyScanResult();
        var visitedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var repositoryRoot = Path.GetFullPath(repositoryPath);
        var automaticImportDirectories = AncestorDirectories(
            Path.GetDirectoryName(projectPath)!,
            repositoryRoot);

        foreach (var directory in automaticImportDirectories)
        {
            ScanImportedFile(
                Path.Combine(directory, "Directory.Build.props"),
                projectPath,
                repositoryRoot,
                packageCatalog,
                visitedFiles,
                result,
                required: false);
        }

        ScanImportedFile(
            projectPath,
            projectPath,
            repositoryRoot,
            packageCatalog,
            visitedFiles,
            result,
            required: true);

        foreach (var directory in automaticImportDirectories)
        {
            ScanImportedFile(
                Path.Combine(directory, "Directory.Build.targets"),
                projectPath,
                repositoryRoot,
                packageCatalog,
                visitedFiles,
                result,
                required: false);
        }

        return result;
    }

    /// <summary>
    /// 读取单个 MSBuild 文件、扫描引用并递归跟随静态显式导入
    /// </summary>
    /// <param name="filePath">项目、props 或 targets 文件路径</param>
    /// <param name="consumerProjectPath">导入内容最终应用的项目路径</param>
    /// <param name="repositoryRoot">允许读取的仓库根目录</param>
    /// <param name="packageCatalog">当前仓库淘汰包目录</param>
    /// <param name="visitedFiles">用于终止导入循环的已访问路径集合</param>
    /// <param name="result">累计治理错误的扫描结果</param>
    /// <param name="required">文件缺失时是否报告配置错误</param>
    private static void ScanImportedFile(
        string filePath,
        string consumerProjectPath,
        string repositoryRoot,
        ForbiddenPackageCatalog packageCatalog,
        ISet<string> visitedFiles,
        DependencyScanResult result,
        bool required)
    {
        var fullPath = Path.GetFullPath(filePath);
        if (!IsWithinRepository(fullPath, repositoryRoot))
        {
            result.Errors.Add(new DependencyScanError(
                "TWGOV000",
                consumerProjectPath,
                $"MSBuild import leaves the repository boundary: {filePath}"));
            return;
        }

        if (!File.Exists(fullPath))
        {
            if (required)
            {
                result.Errors.Add(new DependencyScanError(
                    "TWGOV000",
                    consumerProjectPath,
                    $"MSBuild import does not exist: {filePath}"));
            }

            return;
        }

        if (!visitedFiles.Add(fullPath))
        {
            return;
        }

        XDocument document;
        try
        {
            document = XDocument.Load(fullPath, LoadOptions.PreserveWhitespace);
        }
        catch (Exception exception) when (exception is System.Xml.XmlException or InvalidOperationException)
        {
            result.Errors.Add(new DependencyScanError(
                "TWGOV000",
                consumerProjectPath,
                $"MSBuild XML is invalid: {fullPath}"));
            return;
        }

        ScanDocument(consumerProjectPath, document, packageCatalog, result);
        foreach (var import in ReadImports(document))
        {
            if (ContainsExpression(import) || import.IndexOfAny(['*', '?']) >= 0)
            {
                result.Errors.Add(new DependencyScanError(
                    "TWGOV000",
                    consumerProjectPath,
                    $"MSBuild import cannot be evaluated statically: {import}"));
                continue;
            }

            var importedPath = Path.Combine(
                Path.GetDirectoryName(fullPath)!,
                MsBuildPath.NormalizeFileSystemPath(import, Path.DirectorySeparatorChar));
            ScanImportedFile(
                importedPath,
                consumerProjectPath,
                repositoryRoot,
                packageCatalog,
                visitedFiles,
                result,
                required: true);
        }
    }

    /// <summary>
    /// 对单个已解析 MSBuild 文档执行保守依赖治理
    /// </summary>
    /// <param name="projectPath">导入内容最终应用的项目路径</param>
    /// <param name="document">已经解析的项目或导入文档</param>
    /// <param name="packageCatalog">当前仓库淘汰包目录</param>
    /// <param name="result">累计治理错误的扫描结果</param>
    private static void ScanDocument(
        string projectPath,
        XDocument document,
        ForbiddenPackageCatalog packageCatalog,
        DependencyScanResult result)
    {
        var isProductionProject = IsProductionProject(projectPath);
        var projectName = Path.GetFileNameWithoutExtension(projectPath);
        foreach (var reference in ReadReferences(document))
        {
            if (ContainsExpression(reference.ItemSpec))
            {
                result.Errors.Add(new DependencyScanError(
                    "TWGOV000",
                    projectPath,
                    $"{reference.ReferenceType} {reference.ItemOperation} identity cannot be evaluated statically: {reference.ItemSpec}"));
                continue;
            }

            var packageId = ReferencePackageId(reference);
            if (packageId.EndsWith("TestBase", StringComparison.OrdinalIgnoreCase)
                || reference.ItemSpec.Contains("TestBase", StringComparison.OrdinalIgnoreCase))
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
                    $"Retired {reference.ReferenceType} {reference.ItemOperation} '{retiredPackage.PackageId}'; {replacement}."));
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
    }

    /// <summary>
    /// 读取项目文件中的 PackageReference 和 ProjectReference
    /// </summary>
    /// <param name="document">已经解析的项目 XML 文档</param>
    /// <returns>引用类型、Include 或 Update 操作及拆分后的 item-spec 集合</returns>
    private static IEnumerable<ProjectReferenceEntry> ReadReferences(XDocument document)
    {
        return document
            .Descendants()
            .Where(element => string.Equals(
                    element.Name.LocalName,
                    "PackageReference",
                    StringComparison.OrdinalIgnoreCase)
                || string.Equals(
                    element.Name.LocalName,
                    "ProjectReference",
                    StringComparison.OrdinalIgnoreCase))
            .Select(element => new
            {
                Element = element,
                Attribute = element.Attributes().FirstOrDefault(attribute =>
                    attribute.Name.Namespace == XNamespace.None
                    && (string.Equals(attribute.Name.LocalName, "Include", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(attribute.Name.LocalName, "Update", StringComparison.OrdinalIgnoreCase)))
            })
            .Where(entry => entry.Attribute is not null)
            .SelectMany(entry => entry.Attribute!.Value.Split(
                    ';',
                    StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(itemSpec => new ProjectReferenceEntry(
                    entry.Element.Name.LocalName,
                    entry.Attribute.Name.LocalName,
                    itemSpec)));
    }

    /// <summary>
    /// 读取静态显式 Import 项目路径并按分号拆分
    /// </summary>
    /// <param name="document">已经解析的项目、props 或 targets 文档</param>
    /// <returns>每个非空 Import Project item-spec</returns>
    private static IEnumerable<string> ReadImports(XDocument document)
    {
        return document.Descendants()
            .Where(element => string.Equals(element.Name.LocalName, "Import", StringComparison.OrdinalIgnoreCase))
            .SelectMany(element => element.Attributes()
                .Where(attribute => attribute.Name.Namespace == XNamespace.None
                    && string.Equals(attribute.Name.LocalName, "Project", StringComparison.OrdinalIgnoreCase))
                .Take(1)
                .SelectMany(attribute => attribute.Value.Split(
                    ';',
                    StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)));
    }

    /// <summary>
    /// 从不同 MSBuild 引用类型提取规范包标识
    /// </summary>
    /// <param name="reference">待解析的项目引用项</param>
    /// <returns>PackageReference 标识或 ProjectReference 项目文件名</returns>
    private static string ReferencePackageId(ProjectReferenceEntry reference)
    {
        return string.Equals(reference.ReferenceType, "ProjectReference", StringComparison.OrdinalIgnoreCase)
            ? Path.GetFileNameWithoutExtension(MsBuildPath.NormalizeFileSystemPath(
                reference.ItemSpec,
                Path.DirectorySeparatorChar))
            : reference.ItemSpec;
    }

    /// <summary>
    /// 判断 MSBuild item-spec 是否包含需要 evaluation 的动态表达式
    /// </summary>
    /// <param name="value">引用或导入 item-spec</param>
    /// <returns>包含属性、item 或 metadata 表达式时返回 <see langword="true"/></returns>
    private static bool ContainsExpression(string value)
    {
        return value.Contains("$(", StringComparison.Ordinal)
            || value.Contains("@(", StringComparison.Ordinal)
            || value.Contains("%(", StringComparison.Ordinal);
    }

    /// <summary>
    /// 返回仓库根到项目目录的祖先目录序列
    /// </summary>
    /// <param name="projectDirectory">项目所在目录</param>
    /// <param name="repositoryRoot">仓库根目录</param>
    /// <returns>从仓库根到项目目录排列的目录集合</returns>
    private static IReadOnlyList<string> AncestorDirectories(string projectDirectory, string repositoryRoot)
    {
        var directories = new List<string>();
        var current = new DirectoryInfo(Path.GetFullPath(projectDirectory));
        while (current is not null && IsWithinRepository(current.FullName, repositoryRoot))
        {
            directories.Add(current.FullName);
            if (PathEquals(current.FullName, repositoryRoot))
            {
                break;
            }

            current = current.Parent;
        }

        directories.Reverse();
        return directories;
    }

    /// <summary>
    /// 判断候选文件是否位于仓库根目录内部
    /// </summary>
    /// <param name="path">待检查绝对路径</param>
    /// <param name="repositoryRoot">仓库根目录绝对路径</param>
    /// <returns>候选路径等于或位于仓库根下时返回 <see langword="true"/></returns>
    private static bool IsWithinRepository(string path, string repositoryRoot)
    {
        var relativePath = Path.GetRelativePath(repositoryRoot, path);
        return !relativePath.Equals("..", StringComparison.Ordinal)
            && !relativePath.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
            && !Path.IsPathRooted(relativePath);
    }

    /// <summary>
    /// 按宿主路径规则判断两个绝对路径是否相同
    /// </summary>
    /// <param name="left">左侧绝对路径</param>
    /// <param name="right">右侧绝对路径</param>
    /// <returns>路径相同时返回 <see langword="true"/></returns>
    private static bool PathEquals(string left, string right)
    {
        return string.Equals(
            Path.TrimEndingDirectorySeparator(Path.GetFullPath(left)),
            Path.TrimEndingDirectorySeparator(Path.GetFullPath(right)),
            OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
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
    /// 判断项目路径是否位于构建输出或模板内容目录
    /// </summary>
    /// <param name="path">待检查的项目路径</param>
    /// <returns>路径位于 bin、obj 或模板内容目录时返回 <see langword="true"/></returns>
    private static bool IsIgnoredProjectPath(string path)
    {
        var normalized = path.Replace('\\', '/');
        return normalized.Contains("/bin/", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("/obj/", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("/templates/", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("/Tw.Templates/content/", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 描述项目文件中的依赖引用类型与原始 Include 值
    /// </summary>
    /// <param name="ReferenceType">PackageReference 或 ProjectReference</param>
    /// <param name="ItemOperation">Include 或 Update</param>
    /// <param name="ItemSpec">拆分后的单个 MSBuild item-spec</param>
    private sealed record ProjectReferenceEntry(string ReferenceType, string ItemOperation, string ItemSpec);
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

/// <summary>
/// 为静态 MSBuild item-spec 提供文件系统路径转换
/// </summary>
internal static class MsBuildPath
{
    /// <summary>
    /// 按指定宿主分隔符转换 MSBuild item-spec
    /// </summary>
    /// <param name="itemSpec">来自 Include 或 Update 的静态 item-spec</param>
    /// <param name="directorySeparator">目标文件系统目录分隔符</param>
    /// <returns>可交给 <see cref="Path"/> API 的路径文本</returns>
    internal static string NormalizeFileSystemPath(
        string itemSpec,
        char directorySeparator)
    {
        return itemSpec
            .Replace('\\', directorySeparator)
            .Replace('/', directorySeparator);
    }
}
