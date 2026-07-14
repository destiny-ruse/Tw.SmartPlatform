using AwesomeAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace Tw.Architecture.Tests;

/// <summary>
/// 固定多租户与分片契约合并后的项目和命名空间边界
/// </summary>
public sealed partial class PackageConsolidationTests
{
    /// <summary>
    /// 第十一项任务删除的两个运行时包标识
    /// </summary>
    private static readonly string[] RetiredPackageIds =
    [
        "Tw.MultiTenancy.Abstractions",
        "Tw.Sharding.Abstractions"
    ];

    /// <summary>
    /// 合并后必须保持唯一项目库存的能力目录
    /// </summary>
    private static readonly (string Capability, string RuntimeProject, string TestProject)[] RetainedCapabilities =
    [
        ("MultiTenancy", "Tw.MultiTenancy.csproj", "Tw.MultiTenancy.Tests.csproj"),
        ("Sharding", "Tw.Sharding.csproj", "Tw.Sharding.Tests.csproj")
    ];

    /// <summary>
    /// 已删除的契约项目文件和目录不能通过阶段性清单重新出现
    /// </summary>
    [Fact]
    public void RetiredRuntimeProjectFilesAndDirectories_DoNotExist()
    {
        var violations = RetiredContractPackages()
            .Select(package => package.RuntimeProjectPath
                ?? throw new InvalidOperationException($"退休包缺少运行时项目路径：{package.PackageId}"))
            .SelectMany(FindExistingProjectBoundary)
            .ToArray();

        violations.Should().BeEmpty(
            "第十一项任务已完成项目退休，不得继续使用阶段性退休项目白名单");
    }

    /// <summary>
    /// 多租户和分片能力各自只有一个运行时项目与一个测试项目
    /// </summary>
    [Fact]
    public void RetainedCapabilities_HaveExactlyOneRuntimeAndOneTestProject()
    {
        var violations = RetainedCapabilities
            .SelectMany(FindInventoryViolations)
            .ToArray();

        violations.Should().BeEmpty(
            "每项合并能力必须只保留一个运行时项目和一个测试项目");
    }

    /// <summary>
    /// 已退休命名空间不能继续由活动 C# 源文件声明
    /// </summary>
    [Fact]
    public void ActiveSources_DoNotDeclareRetiredContractNamespaces()
    {
        var retiredNamespaces = RetiredContractPackages()
            .SelectMany(package => package.RetiredNamespaces)
            .ToHashSet(StringComparer.Ordinal);
        var sourceFiles = Directory.GetFiles(
            RepositoryLayout.BuildingBlocksSrc,
            "*.cs",
            SearchOption.AllDirectories);
        var violations = FindRetiredNamespaceDeclarations(sourceFiles, retiredNamespaces)
            .Select(RepositoryLayout.RepositoryRelativePath)
            .ToArray();

        violations.Should().BeEmpty("退休契约命名空间不得继续声明活动源码类型");
    }

    /// <summary>
    /// 退休命名空间扫描覆盖文件作用域、块作用域与包含空白的声明
    /// </summary>
    /// <param name="source">包含退休命名空间声明的 C# 源码</param>
    [Theory]
    [InlineData("namespace Tw.MultiTenancy.Abstractions;")]
    [InlineData("namespace Tw.MultiTenancy.Abstractions { }")]
    [InlineData("namespace Tw . MultiTenancy . Abstractions ;")]
    public void RetiredNamespaceScanner_DetectsSupportedNamespaceSyntax(string source)
    {
        using var directory = new TemporaryTestDirectory();
        var sourcePath = directory.WriteFile("RetiredNamespace.cs", source);

        var violations = FindRetiredNamespaceDeclarations(
            [sourcePath],
            new HashSet<string>(["Tw.MultiTenancy.Abstractions"], StringComparer.Ordinal));

        violations.Should().ContainSingle()
            .Which.Should().Be(sourcePath);
    }

    /// <summary>
    /// 从拓扑清单读取第十一项任务退休的两个契约包
    /// </summary>
    /// <returns>按拓扑清单顺序返回的退休包映射</returns>
    /// <exception cref="InvalidOperationException">拓扑清单缺少任一目标退休包时抛出</exception>
    private static IReadOnlyList<RetiredPackageTopology> RetiredContractPackages()
    {
        var retiredPackageIds = RetiredPackageIds.ToHashSet(StringComparer.Ordinal);
        var retiredPackages = RepositoryLayout.Topology.RetiredPackages
            .Where(package => retiredPackageIds.Contains(package.PackageId))
            .ToArray();
        var missingPackageIds = retiredPackageIds
            .Except(retiredPackages.Select(package => package.PackageId), StringComparer.Ordinal)
            .ToArray();

        if (missingPackageIds.Length > 0)
        {
            throw new InvalidOperationException(
                $"拓扑清单缺少第十一项任务退休包：{string.Join("，", missingPackageIds)}");
        }

        return retiredPackages;
    }

    /// <summary>
    /// 使用 Roslyn 语法树查找退休命名空间声明
    /// </summary>
    /// <param name="sourceFiles">需要扫描的 C# 源文件</param>
    /// <param name="retiredNamespaces">拓扑清单声明的退休命名空间</param>
    /// <returns>声明退休命名空间或其子命名空间的源文件</returns>
    private static IEnumerable<string> FindRetiredNamespaceDeclarations(
        IEnumerable<string> sourceFiles,
        IReadOnlySet<string> retiredNamespaces)
    {
        return sourceFiles.Where(sourceFile => CSharpSyntaxTree
            .ParseText(File.ReadAllText(sourceFile))
            .GetRoot()
            .DescendantNodes()
            .OfType<BaseNamespaceDeclarationSyntax>()
            .Select(NormalizeNamespaceName)
            .Any(declaredNamespace => retiredNamespaces.Any(retiredNamespace =>
                string.Equals(declaredNamespace, retiredNamespace, StringComparison.Ordinal)
                || declaredNamespace.StartsWith($"{retiredNamespace}.", StringComparison.Ordinal))));
    }

    /// <summary>
    /// 将命名空间语法节点归一为不含格式空白的点分名称
    /// </summary>
    /// <param name="namespaceDeclaration">文件作用域或块作用域命名空间声明</param>
    /// <returns>由标识符组成的点分命名空间</returns>
    private static string NormalizeNamespaceName(BaseNamespaceDeclarationSyntax namespaceDeclaration)
    {
        return string.Join(
            ".",
            namespaceDeclaration.Name
                .DescendantTokens()
                .Where(token => token.IsKind(SyntaxKind.IdentifierToken))
                .Select(token => token.ValueText));
    }

    /// <summary>
    /// 查找仍然存在的历史项目文件或项目目录
    /// </summary>
    /// <param name="relativeProjectPath">相对于 BuildingBlocks 源码目录的项目文件路径</param>
    /// <returns>仍然存在的仓库相对路径</returns>
    /// <exception cref="InvalidOperationException">项目路径无法解析目录时抛出</exception>
    private static IEnumerable<string> FindExistingProjectBoundary(string relativeProjectPath)
    {
        var projectPath = Path.Combine(
            RepositoryLayout.BuildingBlocksSrc,
            relativeProjectPath.Replace('/', Path.DirectorySeparatorChar));
        var projectDirectory = Path.GetDirectoryName(projectPath)
            ?? throw new InvalidOperationException("无法解析已退休契约项目目录");

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
    /// 检查单项能力的运行时和测试项目库存
    /// </summary>
    /// <param name="capability">能力名称与目标项目文件</param>
    /// <returns>不符合唯一库存要求的诊断信息</returns>
    private static IEnumerable<string> FindInventoryViolations(
        (string Capability, string RuntimeProject, string TestProject) capability)
    {
        var runtimeRoot = Path.Combine(RepositoryLayout.BuildingBlocksSrc, capability.Capability);
        var testRoot = Path.Combine(RepositoryLayout.BuildingBlocksTests, capability.Capability);
        var runtimeProjects = Directory.GetFiles(runtimeRoot, "*.csproj", SearchOption.AllDirectories)
            .Select(Path.GetFileName)
            .Order(StringComparer.Ordinal)
            .ToArray();
        var testProjects = Directory.GetFiles(testRoot, "*.csproj", SearchOption.AllDirectories)
            .Select(Path.GetFileName)
            .Order(StringComparer.Ordinal)
            .ToArray();

        if (!runtimeProjects.SequenceEqual([capability.RuntimeProject], StringComparer.Ordinal))
        {
            yield return $"{capability.Capability} 运行时项目：{string.Join("，", runtimeProjects)}";
        }

        if (!testProjects.SequenceEqual([capability.TestProject], StringComparer.Ordinal))
        {
            yield return $"{capability.Capability} 测试项目：{string.Join("，", testProjects)}";
        }
    }
}
