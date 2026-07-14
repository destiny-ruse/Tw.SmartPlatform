using AwesomeAssertions;
using Xunit;

namespace Tw.Architecture.Tests;

/// <summary>
/// 固定 ASP.NET Core 契约合并和语义名称迁移后的边界
/// </summary>
public sealed partial class PackageConsolidationTests
{
    /// <summary>
    /// 第十二项任务删除的运行时项目路径
    /// </summary>
    private static readonly string RetiredAspNetCoreProjectPath =
        "Web/Tw.AspNetCore." + "Abstractions/Tw.AspNetCore." + "Abstractions.csproj";

    /// <summary>
    /// 第十二项任务删除的包标识与公开 API 名称
    /// </summary>
    private static readonly string[] RetiredAspNetCoreIdentifiers =
    [
        "Tw.AspNetCore." + "Abstractions",
        "Map" + "TwHealthEndpoints",
        "Tw" + "StringLocalizer",
        "Tw" + "LocalizationOptions"
    ];

    /// <summary>
    /// 网关项目模板的源码、项目文件和依赖锁根目录
    /// </summary>
    private static readonly string GatewayTemplateRoot = Path.Combine(
        RepositoryLayout.Root,
        "backend",
        "dotnet",
        "tools",
        "src",
        "Tw.Templates",
        "content",
        "gateway");

    /// <summary>
    /// 已删除的 ASP.NET Core 抽象项目文件和目录不能重新出现
    /// </summary>
    [Fact]
    public void RetiredAspNetCoreProjectFileAndDirectory_DoNotExist()
    {
        var violations = FindExistingAspNetCoreBoundary().ToArray();

        violations.Should().BeEmpty(
            "第十二项任务已完成项目退休，不得继续使用阶段性退休项目白名单");
    }

    /// <summary>
    /// 活动源码、测试、锁文件、解决方案与共享包文档不能恢复历史标识
    /// </summary>
    [Fact]
    public void ActiveAspNetCoreArtifacts_DoNotContainRetiredIdentifiers()
    {
        var violations = AspNetCoreArtifactFiles()
            .SelectMany(FindRetiredAspNetCoreIdentifiers)
            .ToArray();

        violations.Should().BeEmpty("第十二项任务移除的包边界与 API 名称不得重新进入活动制品");
    }

    /// <summary>
    /// 历史标识扫描以不区分大小写方式识别 NuGet 锁文件的小写包键
    /// </summary>
    [Fact]
    public void RetiredIdentifierScanner_DetectsLowercaseLockKey()
    {
        using var directory = new TemporaryTestDirectory();
        var lockPath = directory.WriteFile(
            "packages.lock.json",
            """
            {
              "dependencies": {
                "net10.0": {
                  "tw.aspnetcore.abstractions": {
                    "type": "Project"
                  }
                }
              }
            }
            """);

        var violations = FindRetiredAspNetCoreIdentifiers(lockPath).ToArray();

        violations.Should().ContainSingle();
    }

    /// <summary>
    /// 网关模板的全部源码、项目文件与锁文件都进入历史标识扫描
    /// </summary>
    [Fact]
    public void AspNetCoreArtifactFiles_IncludeGatewayTemplateSourcesProjectsAndLocks()
    {
        var expectedFiles = Directory
            .GetFiles(GatewayTemplateRoot, "*", SearchOption.AllDirectories)
            .Where(file => !file.Contains(
                $"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}",
                StringComparison.OrdinalIgnoreCase))
            .Where(file => !file.Contains(
                $"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}",
                StringComparison.OrdinalIgnoreCase))
            .Where(file => Path.GetExtension(file).Equals(
                    ".cs",
                    StringComparison.OrdinalIgnoreCase)
                || Path.GetExtension(file).Equals(
                    ".csproj",
                    StringComparison.OrdinalIgnoreCase)
                || Path.GetFileName(file).Equals(
                    "packages.lock.json",
                    StringComparison.OrdinalIgnoreCase));
        var scannedFiles = AspNetCoreArtifactFiles()
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var missingFiles = expectedFiles
            .Except(scannedFiles, StringComparer.OrdinalIgnoreCase)
            .Select(RepositoryLayout.RepositoryRelativePath)
            .ToArray();

        missingFiles.Should().BeEmpty(
            "网关模板中的源码、项目定义和依赖锁都可能恢复退休运行时边界");
    }

    /// <summary>
    /// 查找仍然存在的历史项目文件或目录
    /// </summary>
    /// <returns>仍然存在的仓库相对路径</returns>
    /// <exception cref="InvalidOperationException">项目路径无法解析目录时抛出</exception>
    private static IEnumerable<string> FindExistingAspNetCoreBoundary()
    {
        var projectPath = Path.Combine(
            RepositoryLayout.BuildingBlocksSrc,
            RetiredAspNetCoreProjectPath.Replace('/', Path.DirectorySeparatorChar));
        var projectDirectory = Path.GetDirectoryName(projectPath)
            ?? throw new InvalidOperationException("无法解析已退休 ASP.NET Core 项目目录");

        if (Directory.Exists(projectDirectory))
        {
            yield return RepositoryLayout.RepositoryRelativePath(projectDirectory);
        }

        if (File.Exists(projectPath))
        {
            yield return RepositoryLayout.RepositoryRelativePath(projectPath);
        }
    }

    /// <summary>
    /// 枚举需要锁定语义名称的活动源码、测试、依赖与文档文件
    /// </summary>
    /// <returns>需要扫描的绝对文件路径</returns>
    private static IEnumerable<string> AspNetCoreArtifactFiles()
    {
        var extensions = new HashSet<string>(
            [".cs", ".csproj", ".json", ".yaml", ".md", ".props", ".targets", ".slnx"],
            StringComparer.OrdinalIgnoreCase);
        var roots = new[]
        {
            RepositoryLayout.BuildingBlocksSrc,
            RepositoryLayout.BuildingBlocksTests,
            Path.Combine(RepositoryLayout.Root, "docs", "shared-packages"),
            GatewayTemplateRoot
        };

        var files = roots
            .SelectMany(root => Directory.GetFiles(root, "*", SearchOption.AllDirectories))
            .Where(file => extensions.Contains(Path.GetExtension(file)))
            .Where(file => !file.Contains(
                $"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}",
                StringComparison.OrdinalIgnoreCase))
            .Where(file => !file.Contains(
                $"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}",
                StringComparison.OrdinalIgnoreCase))
            .Where(file => !file.Contains(
                $"{Path.DirectorySeparatorChar}Architecture{Path.DirectorySeparatorChar}",
                StringComparison.OrdinalIgnoreCase))
            .Where(file => !file.Contains(
                $"{Path.DirectorySeparatorChar}migrations{Path.DirectorySeparatorChar}",
                StringComparison.OrdinalIgnoreCase));

        return files.Append(
            Path.Combine(RepositoryLayout.Root, "backend", "dotnet", "Tw.SmartPlatform.slnx"));
    }

    /// <summary>
    /// 查找单个文件中出现的历史包标识或 API 名称
    /// </summary>
    /// <param name="filePath">需要扫描的绝对文件路径</param>
    /// <returns>包含文件路径与历史标识的诊断信息</returns>
    private static IEnumerable<string> FindRetiredAspNetCoreIdentifiers(string filePath)
    {
        var content = File.ReadAllText(filePath);
        var relativePath = RepositoryLayout.RepositoryRelativePath(filePath);

        foreach (var retiredIdentifier in RetiredAspNetCoreIdentifiers)
        {
            if (relativePath.Contains(retiredIdentifier, StringComparison.OrdinalIgnoreCase)
                || content.Contains(retiredIdentifier, StringComparison.OrdinalIgnoreCase))
            {
                yield return $"{relativePath}：{retiredIdentifier}";
            }
        }
    }
}
