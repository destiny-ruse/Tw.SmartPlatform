using System.Xml.Linq;

namespace Tw.Cli.Governance;

/// <summary>
/// 扫描项目依赖并返回治理结果
/// </summary>
public sealed class ProjectDependencyScanner
{
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

        foreach (var projectPath in Directory.GetFiles(repositoryPath, "*.csproj", SearchOption.AllDirectories))
        {
            if (projectPath.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase) ||
                projectPath.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var projectXml = File.ReadAllText(projectPath);
            var projectResult = ScanProjectText(projectPath, projectXml);
            foreach (var error in projectResult.Errors)
            {
                result.Errors.Add(error);
            }
        }

        return result;
    }

    /// <summary>
    /// 扫描项目文本并返回发现的治理问题
    /// </summary>
    /// <param name="projectPath">待分析项目文件的路径</param>
    /// <param name="projectXml">项目文件的 XML 文本内容</param>
    /// <returns>依赖扫描发现的治理违规结果</returns>
    public DependencyScanResult ScanProjectText(string projectPath, string projectXml)
    {
        var result = new DependencyScanResult();
        XDocument document;
        try
        {
            document = XDocument.Parse(projectXml, LoadOptions.PreserveWhitespace);
        }
        catch (Exception ex) when (ex is System.Xml.XmlException or InvalidOperationException)
        {
            result.Errors.Add(new DependencyScanError("TWGOV000", projectPath, "Project XML is invalid."));
            return result;
        }

        var isProductionProject = IsProductionProject(projectPath);
        foreach (var reference in ReadReferenceIncludes(document))
        {
            var name = Path.GetFileNameWithoutExtension(reference);
            if (name.EndsWith("TestBase", StringComparison.OrdinalIgnoreCase) ||
                reference.Contains("TestBase", StringComparison.OrdinalIgnoreCase))
            {
                if (isProductionProject)
                {
                    result.Errors.Add(new DependencyScanError(
                        "TWGOV003",
                        projectPath,
                        "Production projects must not reference test base packages."));
                }
            }

            if (ForbiddenPackageCatalog.Names.Contains(name) || ForbiddenPackageCatalog.Names.Contains(reference))
            {
                result.Errors.Add(new DependencyScanError(
                    "TWGOV002",
                    projectPath,
                    $"Forbidden package reference '{reference}'."));
            }
        }

        return result;
    }

    /// <summary>
    /// 读取项目文件中的 PackageReference 和 ProjectReference Include 值
    /// </summary>
    /// <param name="document">已经解析的项目 XML 文档</param>
    /// <returns>项目引用 Include 值集合</returns>
    private static IEnumerable<string> ReadReferenceIncludes(XDocument document)
    {
        return document
            .Descendants()
            .Where(element => element.Name.LocalName is "PackageReference" or "ProjectReference")
            .Select(element => element.Attribute("Include")?.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))!
            .Select(value => value!);
    }

    /// <summary>
    /// 判断生产项目是否满足条件
    /// </summary>
    /// <param name="projectPath">待分析项目文件的路径</param>
    /// <returns>条件满足时返回 <see langword="true"/></returns>
    private static bool IsProductionProject(string projectPath)
    {
        var normalized = projectPath.Replace('\\', '/');
        return normalized.Contains("/src/", StringComparison.OrdinalIgnoreCase) ||
            normalized.StartsWith("src/", StringComparison.OrdinalIgnoreCase);
    }
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
public sealed record DependencyScanError(string Code, string ProjectPath, string Message);
