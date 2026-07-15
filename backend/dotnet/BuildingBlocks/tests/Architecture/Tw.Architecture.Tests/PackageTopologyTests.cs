using AwesomeAssertions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Xml.Linq;
using Xunit;

namespace Tw.Architecture.Tests;

/// <summary>
/// 覆盖PackageTopology的核心行为和边界条件
/// </summary>
public sealed class PackageTopologyTests
{
    /// <summary>
    /// 验证运行时项目集合与合并后的批准拓扑完全一致
    /// </summary>
    [Fact]
    public void RuntimeProjectSet_MatchesApprovedConsolidatedTopology()
    {
        AssertProjectSetMatchesTopology(
            RepositoryLayout.BuildingBlocksSrc,
            RepositoryLayout.Topology.RuntimeProjects.Select(project => project.Path),
            expectedCount: 57);
    }

    /// <summary>
    /// 验证测试项目集合与合并后的批准拓扑完全一致
    /// </summary>
    [Fact]
    public void TestProjectSet_MatchesApprovedConsolidatedTopology()
    {
        AssertProjectSetMatchesTopology(
            RepositoryLayout.BuildingBlocksTests,
            RepositoryLayout.Topology.TestProjects.Select(project => project.Path),
            expectedCount: 50);
    }

    /// <summary>
    /// 验证测试项目均能映射到现存运行时包
    /// </summary>
    [Fact]
    public void TestProjects_TargetExistingRuntimePackages()
    {
        var runtimePackages = RepositoryLayout.Topology.RuntimeProjects
            .Select(project => Path.GetFileNameWithoutExtension(project.Path))
            .ToHashSet(StringComparer.Ordinal);
        var violations = RepositoryLayout.Topology.TestProjects
            .Select(project => Path.GetFileNameWithoutExtension(project.Path))
            .Where(project => !string.Equals(project, "Tw.Architecture.Tests", StringComparison.Ordinal))
            .Select(project => new
            {
                TestProject = project,
                RuntimePackage = RepositoryLayout.RuntimePackageNameForTestProject(project)
            })
            .Where(project => !runtimePackages.Contains(project.RuntimePackage))
            .Select(project => $"{project.TestProject} -> {project.RuntimePackage}")
            .ToArray();

        violations.Should().BeEmpty("every test project must target a retained runtime package");
    }

    /// <summary>
    /// 验证运行时项目的目录、包、程序集和根命名空间身份保持一致
    /// </summary>
    [Fact]
    public void RuntimeProjectIdentity_MatchesDirectoryAndPackageMetadata()
    {
        var violations = new List<string>();
        foreach (var project in RepositoryLayout.Topology.RuntimeProjects)
        {
            var projectPath = Path.Combine(
                RepositoryLayout.BuildingBlocksSrc,
                project.Path.Replace('/', Path.DirectorySeparatorChar));
            var projectStem = Path.GetFileNameWithoutExtension(projectPath);
            var projectDirectory = Path.GetFileName(Path.GetDirectoryName(projectPath));
            var document = XDocument.Load(projectPath);
            var packageId = EffectiveProjectProperty(document, "PackageId", projectStem);
            var assemblyName = EffectiveProjectProperty(document, "AssemblyName", projectStem);
            var rootNamespace = EffectiveProjectProperty(document, "RootNamespace", projectStem);
            var expectedRootNamespace = string.Equals(projectStem, "Tw.Core", StringComparison.Ordinal)
                ? "Tw"
                : projectStem;

            if (!string.Equals(projectDirectory, projectStem, StringComparison.Ordinal)
                || !string.Equals(packageId, projectStem, StringComparison.Ordinal)
                || !string.Equals(assemblyName, projectStem, StringComparison.Ordinal)
                || !string.Equals(rootNamespace, expectedRootNamespace, StringComparison.Ordinal)
                || !string.Equals(rootNamespace, project.RootNamespace, StringComparison.Ordinal)
                || !packageId.StartsWith("Tw.", StringComparison.Ordinal))
            {
                violations.Add(
                    $"{project.Path}: directory={projectDirectory}, package={packageId}, assembly={assemblyName}, root={rootNamespace}");
            }
        }

        violations.Should().BeEmpty("runtime project identities must match their canonical project stems and approved roots");
    }

    /// <summary>
    /// 验证自有类型名称不使用含义模糊的角色后缀
    /// </summary>
    [Fact]
    public void OwnedTypeNames_DoNotUseAmbiguousRoleSuffixes()
    {
        var forbiddenSuffixes = new[] { "Manager", "Helper", "Util" };
        var violations = Directory.GetFiles(RepositoryLayout.BuildingBlocksSrc, "*.cs", SearchOption.AllDirectories)
            .Where(path => !IsBuildOutput(path))
            .SelectMany(path => CSharpSyntaxTree.ParseText(File.ReadAllText(path), path: path)
                .GetRoot()
                .DescendantNodes()
                .Select(declaration => declaration switch
                {
                    BaseTypeDeclarationSyntax type => type.Identifier.ValueText,
                    DelegateDeclarationSyntax @delegate => @delegate.Identifier.ValueText,
                    _ => null
                })
                .Where(name => name is not null)
                .Select(name => new { Path = path, Name = name! }))
            .Where(declaration => forbiddenSuffixes.Any(suffix => declaration.Name.EndsWith(suffix, StringComparison.Ordinal)))
            .Select(declaration => $"{RepositoryLayout.RepositoryRelativePath(declaration.Path)}: {declaration.Name}")
            .ToArray();

        violations.Should().BeEmpty("owned declarations must use capability-specific role names");
    }

    /// <summary>
    /// 验证BuildingBlocksRuntimeProjectsLiveUnderCapabilityFolders
    /// </summary>
    [Fact]
    public void BuildingBlocks_RuntimeProjects_LiveUnderCapabilityFolders()
    {
        var projectFiles = Directory.GetFiles(RepositoryLayout.BuildingBlocksSrc, "*.csproj", SearchOption.AllDirectories);

        projectFiles.Should().NotBeEmpty();
        projectFiles.Should().OnlyContain(
            path => Path.GetRelativePath(RepositoryLayout.BuildingBlocksSrc, path).Replace('\\', '/').Count(ch => ch == '/') == 2,
            "runtime projects must use src/<Capability>/<Package>/<Package>.csproj");
    }

    /// <summary>
    /// 验证BuildingBlocksTestProjectsLiveUnderCapabilityFolders
    /// </summary>
    [Fact]
    public void BuildingBlocks_TestProjects_LiveUnderCapabilityFolders()
    {
        var testProjects = Directory.GetFiles(RepositoryLayout.BuildingBlocksTests, "*.csproj", SearchOption.AllDirectories);

        testProjects.Should().NotBeEmpty();
        testProjects.Should().OnlyContain(
            path => Path.GetRelativePath(RepositoryLayout.BuildingBlocksTests, path).Replace('\\', '/').Count(ch => ch == '/') == 2,
            "test projects must use tests/<Capability>/<TestProject>/<TestProject>.csproj");
    }

    /// <summary>
    /// 验证BuildingBlocksTestProjectsMirrorRuntimeCapabilityFolders
    /// </summary>
    [Fact]
    public void BuildingBlocks_TestProjects_MirrorRuntimeCapabilityFolders()
    {
        var runtimeCapabilities = RepositoryLayout.RuntimeCapabilitiesByPackage();
        var violations = Directory.GetFiles(RepositoryLayout.BuildingBlocksTests, "*.csproj", SearchOption.AllDirectories)
            .Select(path => new
            {
                Path = path,
                ProjectName = Path.GetFileNameWithoutExtension(path),
                Capability = RepositoryLayout.TestCapability(path)
            })
            .Where(project => project.ProjectName != "Tw.Architecture.Tests")
            .Select(project => new
            {
                project.Path,
                project.ProjectName,
                project.Capability,
                RuntimePackage = RepositoryLayout.RuntimePackageNameForTestProject(project.ProjectName)
            })
            .Select(project => new
            {
                project.ProjectName,
                project.Capability,
                RuntimeCapability = runtimeCapabilities.GetValueOrDefault(project.RuntimePackage)
            })
            .Where(project => project.RuntimeCapability is null || project.RuntimeCapability != project.Capability)
            .Select(project => $"{project.ProjectName} expected {project.RuntimeCapability ?? "a matching runtime owner"} but was {project.Capability}")
            .ToArray();

        violations.Should().BeEmpty("test projects must stay beside the capability of the runtime package they validate");
    }

    /// <summary>
    /// 验证BuildingBlocks不ContainAbstractionsTestProjects
    /// </summary>
    [Fact]
    public void BuildingBlocks_DoesNotContainAbstractionsTestProjects()
    {
        var abstractionsTests = Directory.GetFiles(RepositoryLayout.BuildingBlocksTests, "*.csproj", SearchOption.AllDirectories)
            .Select(path => Path.GetFileNameWithoutExtension(path)!)
            .Where(projectName => projectName.EndsWith(".Abstractions.Tests", StringComparison.Ordinal))
            .ToArray();

        abstractionsTests.Should().BeEmpty("Abstractions packages define contracts and are validated through consuming packages");
    }

    /// <summary>
    /// 验证DotnetToolsProjectsLiveUnderSrcOr
    /// </summary>
    [Fact]
    public void DotnetTools_ProjectsLiveUnderSrcOrTests()
    {
        var toolProjects = Directory.GetFiles(RepositoryLayout.ToolsRoot, "*.csproj", SearchOption.AllDirectories)
            .Where(path => !Path.GetRelativePath(RepositoryLayout.ToolsRoot, path).Replace('\\', '/').Contains("/content/", StringComparison.Ordinal))
            .ToArray();

        toolProjects.Should().NotBeEmpty();
        toolProjects.Should().OnlyContain(path => IsToolProjectInSrcOrTests(path), "tools projects must use tools/src/<Project> or tools/tests/<Project>");
    }

    /// <summary>
    /// 验证DotnetTest项目Classification不TreatTest基类包作为Executable
    /// </summary>
    [Fact]
    public void DotnetTestProjectClassification_DoesNotTreatTestBasePackagesAsExecutableTests()
    {
        var directoryBuildProps = File.ReadAllText(Path.Combine(RepositoryLayout.DotnetRoot, "Directory.Build.props"));

        directoryBuildProps.Should().NotContain(
            "EndsWith('.TestBase')",
            "TestBase source packages provide reusable fixtures and must not be executed as VSTest projects by solution-level dotnet test");
    }

    /// <summary>
    /// 验证禁止包Do不Exist
    /// </summary>
    [Fact]
    public void ForbiddenPackages_DoNotExist()
    {
        var forbiddenPackages = new[]
        {
            "Tw.Infrastructure",
            "Tw.Context",
            "Tw.ExecutionPipeline",
            "Tw.Swagger",
            "Tw.ApiVersioning",
            "Tw.Validation",
            "Tw.RateLimiting",
            "Tw.HealthChecks",
            "Tw.ObjectStorage",
            "Tw.Serialization",
            "Tw.Bff",
            "Tw.DynamicApi",
            "Tw.AspNetCore.DynamicApi",
            "Tw.ApplicationConfiguration",
            "Tw.Snowflake",
            "Tw.DistributedLock",
            "Tw.Autofac",
            "Tw.Localization.AspNetCore",
            "Tw.Grpc.AspNetCore",
            "Tw.Cqrs",
            "Tw.UnitOfWork",
            "Tw.Data.Abstractions",
            "Tw.Testing"
        };

        var actualPackages = Directory.GetFiles(RepositoryLayout.BuildingBlocksSrc, "*.csproj", SearchOption.AllDirectories)
            .Select(Path.GetFileNameWithoutExtension)
            .ToHashSet(StringComparer.Ordinal);

        actualPackages.Should().NotIntersectWith(forbiddenPackages);
    }

    /// <summary>
    /// 判断工具项目InSrcOr是否满足条件
    /// </summary>
    /// <param name="path">待处理文件或目录的路径</param>
    /// <returns>条件满足时返回 <see langword="true"/></returns>
    private static bool IsToolProjectInSrcOrTests(string path)
    {
        var relative = Path.GetRelativePath(RepositoryLayout.ToolsRoot, path).Replace('\\', '/');
        var parts = relative.Split('/');
        return parts.Length == 3 && (parts[0] == "src" || parts[0] == "tests");
    }

    /// <summary>
    /// 验证物理项目路径集合与拓扑清单中的能力相对路径完全相同
    /// </summary>
    /// <param name="projectsRoot">运行时或测试项目根目录</param>
    /// <param name="approvedPaths">拓扑清单批准的能力相对项目路径</param>
    /// <param name="expectedCount">合并计划锁定的项目数量</param>
    private static void AssertProjectSetMatchesTopology(
        string projectsRoot,
        IEnumerable<string> approvedPaths,
        int expectedCount)
    {
        var actualPaths = Directory.GetFiles(projectsRoot, "*.csproj", SearchOption.AllDirectories)
            .Select(path => RepositoryLayout.NormalizePath(Path.GetRelativePath(projectsRoot, path)))
            .Order(StringComparer.Ordinal)
            .ToArray();
        var expectedPaths = approvedPaths.Order(StringComparer.Ordinal).ToArray();

        expectedPaths.Should().HaveCount(expectedCount, "the approved consolidated inventory has a fixed cardinality");
        actualPaths.Should().Equal(expectedPaths, "the complete capability-relative project inventory is governed by the topology manifest");
    }

    /// <summary>
    /// 读取 SDK 项目的有效属性值，未显式配置时返回 MSBuild 默认值
    /// </summary>
    /// <param name="document">项目 XML 文档</param>
    /// <param name="propertyName">需要读取的 MSBuild 属性名称</param>
    /// <param name="defaultValue">属性省略时采用的 SDK 默认值</param>
    /// <returns>项目最终使用的属性值</returns>
    private static string EffectiveProjectProperty(XDocument document, string propertyName, string defaultValue)
    {
        return document.Descendants(propertyName)
            .Select(element => element.Value.Trim())
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))
            ?? defaultValue;
    }

    /// <summary>
    /// 判断源码文件是否位于编译输出目录
    /// </summary>
    /// <param name="path">待检查的源码文件路径</param>
    /// <returns>路径位于 bin 或 obj 目录时返回 <see langword="true"/></returns>
    private static bool IsBuildOutput(string path)
    {
        var normalized = RepositoryLayout.NormalizePath(path);
        return normalized.Contains("/bin/", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("/obj/", StringComparison.OrdinalIgnoreCase);
    }
}
