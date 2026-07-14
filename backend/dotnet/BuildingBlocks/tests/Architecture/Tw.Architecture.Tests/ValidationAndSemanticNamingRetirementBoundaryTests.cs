using AwesomeAssertions;
using Xunit;

namespace Tw.Architecture.Tests;

/// <summary>
/// 固定验证契约合并与提供方语义入口清理后的边界
/// </summary>
public sealed partial class PackageConsolidationTests
{
    /// <summary>
    /// 第十三项任务删除的验证包标识
    /// </summary>
    private static readonly string RetiredValidationPackageId =
        "Tw.Validation." + "Abstractions";

    /// <summary>
    /// 第十三项任务删除或重命名的生产扩展入口
    /// </summary>
    private static readonly string[] RetiredProviderEntryPoints =
    [
        "Add" + "TwYarpGateway",
        "Add" + "TwOpenTelemetry",
        "Add" + "TwHttpResilience",
        "EnrichWith" + "TwRedaction"
    ];

    /// <summary>
    /// 验证契约历史项目、引用、依赖锁与活动文档不能重新出现
    /// </summary>
    [Fact]
    public void RetiredValidationPackage_DoesNotExistInActiveArtifacts()
    {
        var projectDirectory = Path.Combine(
            RepositoryLayout.BuildingBlocksSrc,
            "Foundation",
            RetiredValidationPackageId);
        var violations = ActiveValidationBoundaryFiles()
            .Where(file => File.ReadAllText(file).Contains(
                RetiredValidationPackageId,
                StringComparison.OrdinalIgnoreCase))
            .Select(RepositoryLayout.RepositoryRelativePath)
            .ToList();

        if (Directory.Exists(projectDirectory))
        {
            violations.Add(RepositoryLayout.RepositoryRelativePath(projectDirectory));
        }

        violations.Should().BeEmpty(
            "验证错误契约已由 Tw.ExceptionHandling 承接，历史包目录、引用和锁文件不得恢复");
    }

    /// <summary>
    /// 活动生产源码与共享包文档不能恢复无行为入口或旧脱敏名称
    /// </summary>
    [Fact]
    public void ActiveProviderArtifacts_DoNotContainRetiredEntryPoints()
    {
        var violations = ActiveProviderBoundaryFiles()
            .SelectMany(file => RetiredProviderEntryPoints
                .Where(identifier => File.ReadAllText(file).Contains(identifier, StringComparison.Ordinal))
                .Select(identifier => $"{RepositoryLayout.RepositoryRelativePath(file)}：{identifier}"))
            .ToArray();

        violations.Should().BeEmpty(
            "没有真实宿主装配行为的提供方入口必须删除，Serilog 脱敏入口必须使用行为语义名称");
    }

    /// <summary>
    /// 枚举可能持有历史验证包引用的活动工程制品
    /// </summary>
    /// <returns>需要扫描的绝对文件路径</returns>
    private static IEnumerable<string> ActiveValidationBoundaryFiles()
    {
        var extensions = new HashSet<string>(
            [".cs", ".csproj", ".json", ".yaml", ".md", ".props", ".targets", ".slnx"],
            StringComparer.OrdinalIgnoreCase);
        var roots = new[]
        {
            RepositoryLayout.BuildingBlocksSrc,
            RepositoryLayout.BuildingBlocksTests,
            Path.Combine(RepositoryLayout.Root, "backend", "dotnet", "Build"),
            Path.Combine(RepositoryLayout.Root, "backend", "dotnet", "tools"),
            Path.Combine(RepositoryLayout.Root, "docs", "shared-packages")
        };

        return roots
            .SelectMany(root => Directory.GetFiles(root, "*", SearchOption.AllDirectories))
            .Where(file => extensions.Contains(Path.GetExtension(file)))
            .Where(file => !IsGeneratedOrHistoricalArtifact(file))
            .Append(Path.Combine(RepositoryLayout.Root, "backend", "dotnet", "Tw.SmartPlatform.slnx"));
    }

    /// <summary>
    /// 枚举需要锁定提供方扩展入口名称的生产源码与活动文档
    /// </summary>
    /// <returns>需要扫描的绝对文件路径</returns>
    private static IEnumerable<string> ActiveProviderBoundaryFiles()
    {
        var sourceFiles = Directory.GetFiles(
            RepositoryLayout.BuildingBlocksSrc,
            "*.cs",
            SearchOption.AllDirectories);
        var documentationFiles = Directory.GetFiles(
            Path.Combine(RepositoryLayout.Root, "docs", "shared-packages"),
            "*.md",
            SearchOption.AllDirectories);
        var templateRoot = Path.Combine(
            RepositoryLayout.Root,
            "backend",
            "dotnet",
            "tools",
            "src",
            "Tw.Templates",
            "content");
        var templateSourceFiles = Directory.GetFiles(
            templateRoot,
            "*.cs",
            SearchOption.AllDirectories);

        return sourceFiles
            .Concat(templateSourceFiles)
            .Concat(documentationFiles)
            .Where(file => !IsGeneratedOrHistoricalArtifact(file));
    }

    /// <summary>
    /// 判断文件是否属于构建输出、架构门禁自身或历史迁移记录
    /// </summary>
    /// <param name="filePath">需要判定的绝对文件路径</param>
    /// <returns>文件不属于活动制品时返回 <see langword="true"/></returns>
    private static bool IsGeneratedOrHistoricalArtifact(string filePath)
    {
        var excludedSegments = new[]
        {
            $"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}",
            $"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}",
            $"{Path.DirectorySeparatorChar}Architecture{Path.DirectorySeparatorChar}",
            $"{Path.DirectorySeparatorChar}migrations{Path.DirectorySeparatorChar}"
        };

        return excludedSegments.Any(segment => filePath.Contains(
            segment,
            StringComparison.OrdinalIgnoreCase));
    }
}
