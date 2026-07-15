using System.Xml.Linq;
using System.Text.Json.Nodes;
using AwesomeAssertions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace Tw.Architecture.Tests;

/// <summary>
/// 锁定 BuildingBlocks 目标库存，并允许清单中明确的项目逐步退出
/// </summary>
public sealed partial class PackageConsolidationTests
{
    /// <summary>
    /// 验证项目引用不会指向拓扑清单中的淘汰包
    /// </summary>
    [Fact]
    public void ProjectReferences_DoNotTargetRetiredPackages()
    {
        var retiredPackageIds = RepositoryLayout.Topology.RetiredPackages
            .Select(package => package.PackageId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var violations = Directory.GetFiles(RepositoryLayout.DotnetRoot, "*.csproj", SearchOption.AllDirectories)
            .Where(path => !IsBuildOutput(path))
            .SelectMany(projectPath => MsBuildProjectItems.Read(projectPath, "ProjectReference")
                .SelectMany(reference => reference.ItemSpecs)
                .Select(reference => new
                {
                    ProjectPath = projectPath,
                    Reference = reference,
                    PackageId = Path.GetFileNameWithoutExtension(reference)
                }))
            .Where(reference => retiredPackageIds.Contains(reference.PackageId))
            .Select(reference => $"{RepositoryLayout.RepositoryRelativePath(reference.ProjectPath)} -> {reference.Reference}")
            .ToArray();

        violations.Should().BeEmpty("ProjectReference entries must target retained packages");
    }

    /// <summary>
    /// 验证所有保留运行时项目存在且使用批准的有效根命名空间
    /// </summary>
    [Fact]
    public void TargetRuntimeProjects_ExistAndUseApprovedRootNamespaces()
    {
        var topology = RepositoryLayout.Topology;

        topology.RuntimeProjects.Count.Should().Be(57, "the topology manifest defines the complete retained runtime inventory");

        foreach (var project in topology.RuntimeProjects)
        {
            var projectPath = ProjectPath(RepositoryLayout.BuildingBlocksSrc, project.Path);
            File.Exists(projectPath).Should().BeTrue($"retained runtime project {project.Path} must not be deleted");

            EffectiveRootNamespace(projectPath).Should().Be(
                project.RootNamespace,
                $"{project.Path} must keep its approved root namespace");
        }
    }

    /// <summary>
    /// 验证当前运行时项目只能属于目标库存或清单明确的淘汰库存
    /// </summary>
    [Fact]
    public void CurrentRuntimeProjects_AreTargetOrRetired()
    {
        var expectedPaths = RepositoryLayout.Topology.RuntimeProjects
            .Select(project => project.Path)
            .Concat(RepositoryLayout.Topology.RetiredPackages
                .Select(package => package.RuntimeProjectPath)
                .Where(path => path is not null)
                .Cast<string>())
            .ToHashSet(StringComparer.Ordinal);
        var unexpectedPaths = CurrentProjectPaths(RepositoryLayout.BuildingBlocksSrc)
            .Where(path => !expectedPaths.Contains(path))
            .ToArray();

        unexpectedPaths.Should().BeEmpty("new runtime packages must be added to the topology manifest before they enter BuildingBlocks");
    }

    /// <summary>
    /// 验证当前测试项目只能属于目标库存或具有明确迁移记录的历史测试
    /// </summary>
    [Fact]
    public void CurrentTestProjects_AreTargetOrMigrating()
    {
        var expectedPaths = RepositoryLayout.Topology.TestProjects
            .Select(project => project.Path)
            .Concat(RepositoryLayout.Topology.RetiredPackages
                .Select(package => package.TestProjectPath)
                .Where(path => path is not null)
                .Cast<string>())
            .ToHashSet(StringComparer.Ordinal);
        var unexpectedPaths = CurrentProjectPaths(RepositoryLayout.BuildingBlocksTests)
            .Where(path => !expectedPaths.Contains(path))
            .ToArray();

        unexpectedPaths.Should().BeEmpty("new test projects must be target projects or have an explicit migration record");
    }

    /// <summary>
    /// 验证目标测试在迁移前由现存前身承接，迁移后不会被静默删除
    /// </summary>
    [Fact]
    public void TargetTestProjects_ExistUnlessAnActivePredecessorIsMigrating()
    {
        var topology = RepositoryLayout.Topology;
        topology.TestProjects.Count.Should().Be(50, "the topology manifest defines the complete retained test inventory");

        var missingTargets = topology.TestProjects
            .Where(project => !File.Exists(ProjectPath(RepositoryLayout.BuildingBlocksTests, project.Path)))
            .Where(project => !HasActivePredecessor(project.Path))
            .Select(project => project.Path)
            .ToArray();

        missingTargets.Should().BeEmpty("a target test project may be absent only while its recorded predecessor still exists");
    }

    /// <summary>
    /// 验证清单中的工具项目均存在于固定工具目录
    /// </summary>
    [Fact]
    public void ToolProjects_ExistAtApprovedPaths()
    {
        var topology = RepositoryLayout.Topology;
        topology.ToolProjects.Count.Should().Be(3, "the topology manifest has three governed .NET tool projects");

        foreach (var projectPath in topology.ToolProjects)
        {
            File.Exists(ProjectPath(RepositoryLayout.Root, projectPath)).Should().BeTrue(
                $"tool project {projectPath} must remain available to the solution");
        }
    }

    /// <summary>
    /// 验证独立契约包全部对应保留的运行时项目
    /// </summary>
    [Fact]
    public void IndependentContractPackages_AreRetainedRuntimePackages()
    {
        var topology = RepositoryLayout.Topology;
        var expectedIndependentContractPackages = new[]
        {
            "Tw.Application.Contracts",
            "Tw.Auditing.Contracts",
            "Tw.BackgroundJobs.Abstractions",
            "Tw.DependencyInjection.Abstractions",
            "Tw.Json.Abstractions"
        };

        topology.IndependentContractPackages.Should().BeEquivalentTo(
            expectedIndependentContractPackages,
            "only the five explicitly approved contract projects may be independently packaged");

        var retainedPackageIds = topology.RuntimeProjects
            .Select(project => Path.GetFileNameWithoutExtension(project.Path))
            .ToHashSet(StringComparer.Ordinal);
        topology.IndependentContractPackages.Should().OnlyContain(
            package => retainedPackageIds.Contains(package),
            "independent contracts must be part of the retained runtime inventory");
    }

    /// <summary>
    /// 验证淘汰映射覆盖现存历史项目、迁移测试和保留的拦截器身份
    /// </summary>
    [Fact]
    public void RetiredPackageMappings_CoverCurrentProjectsAndReservedIdentity()
    {
        var retiredPackages = RepositoryLayout.Topology.RetiredPackages;

        retiredPackages.Count.Should().Be(17, "sixteen current retired projects and the reserved Tw.Interception identity are governed");
        retiredPackages.Count(package => package.RuntimeProjectPath is not null).Should().Be(16);
        retiredPackages.Count(package => package.TestProjectPath is not null).Should().Be(8);
        retiredPackages.Should().ContainSingle(package => package.PackageId == "Tw.Interception"
            && package.RuntimeProjectPath == null
            && package.TestProjectPath == null);
        retiredPackages.Should().OnlyContain(package => package.RetiredNamespaces.Count > 0);
    }

    /// <summary>
    /// 验证淘汰包的替代包必须属于清单中的运行时目标项目
    /// </summary>
    [Fact]
    public void TopologyManifest_RejectsReplacementPackageOutsideRuntimeTargets()
    {
        var temporaryTopologyFile = Path.GetTempFileName();
        try
        {
            var manifest = JsonNode.Parse(File.ReadAllText(RepositoryLayout.BuildingBlocksTopologyFile))
                ?? throw new InvalidOperationException("无法解析用于拓扑校验的测试清单");
            var retiredPackage = manifest["retiredPackages"]?
                .AsArray()
                .Select(node => node?.AsObject())
                .FirstOrDefault(package => package?["replacementPackageId"] is not null)
                ?? throw new InvalidOperationException("测试清单缺少具有替代包的淘汰映射");
            retiredPackage["replacementPackageId"] = "Tw.Unknown";
            File.WriteAllText(temporaryTopologyFile, manifest.ToJsonString());

            var loadTopology = () => RepositoryLayout.LoadTopology(temporaryTopologyFile);

            loadTopology.Should().Throw<InvalidOperationException>()
                .WithMessage("*替代运行时包必须是运行时目标项目*");
        }
        finally
        {
            File.Delete(temporaryTopologyFile);
        }
    }

    /// <summary>
    /// 验证非 HTTP 的淘汰测试前身不能放行缺失的目标测试项目
    /// </summary>
    [Fact]
    public void NonHttpPredecessor_CannotTemporarilyCoverMissingTargetTest()
    {
        HasActivePredecessor("Application/Tw.Domain.Tests/Tw.Domain.Tests.csproj").Should().BeFalse(
            "only the manifest's Tw.Http.Client.Tests predecessor may temporarily cover a missing target test");
    }

    /// <summary>
    /// 验证 HTTP 目标测试迁移完成后不再依赖已淘汰的测试前身
    /// </summary>
    [Fact]
    public void HttpTargetTest_DoesNotDependOnRetiredPredecessorAfterMigration()
    {
        const string targetTestPath = "Http/Tw.Http.Tests/Tw.Http.Tests.csproj";

        File.Exists(ProjectPath(RepositoryLayout.BuildingBlocksTests, targetTestPath)).Should().BeTrue();
        HasActivePredecessor(targetTestPath).Should().BeFalse();
    }

    /// <summary>
    /// 验证淘汰项目删除后不会遗留原有的 runtime 或 test 项目目录
    /// </summary>
    [Fact]
    public void RetiredPackageDirectories_DoNotExist()
    {
        var staleDirectories = RepositoryLayout.Topology.RetiredPackages
            .SelectMany(RetiredProjectFiles)
            .Select(Path.GetDirectoryName)
            .Where(directory => directory is not null && Directory.Exists(directory))
            .Select(directory => RepositoryLayout.RepositoryRelativePath(directory!))
            .ToArray();

        staleDirectories.Should().BeEmpty("retiring a project requires deleting its complete runtime or test project directory");
    }

    /// <summary>
    /// 验证生产源码不再声明淘汰命名空间，同时保留 Configuration.Json 功能子命名空间
    /// </summary>
    [Fact]
    public void RetiredNamespaces_DoNotRemainInSource()
    {
        var retiredNamespaces = RepositoryLayout.Topology.RetiredPackages
            .SelectMany(package => package.RetiredNamespaces)
            .Where(namespaceName => !string.Equals(namespaceName, "Tw.Configuration.Json", StringComparison.Ordinal))
            .ToHashSet(StringComparer.Ordinal);
        var violations = Directory.GetFiles(RepositoryLayout.BuildingBlocksSrc, "*.cs", SearchOption.AllDirectories)
            .Where(path => !IsBuildOutput(path))
            .SelectMany(path => CSharpSyntaxTree.ParseText(File.ReadAllText(path), path: path)
                .GetRoot()
                .DescendantNodes()
                .OfType<BaseNamespaceDeclarationSyntax>()
                .Select(declaration => new
                {
                    Path = path,
                    Namespace = FullNamespaceName(declaration)
                }))
            .Where(declaration => retiredNamespaces.Any(retired =>
                string.Equals(declaration.Namespace, retired, StringComparison.Ordinal)
                || declaration.Namespace.StartsWith($"{retired}.", StringComparison.Ordinal)))
            .Select(declaration => $"{RepositoryLayout.RepositoryRelativePath(declaration.Path)}: {declaration.Namespace}")
            .ToArray();

        violations.Should().BeEmpty("retired namespace boundaries must be removed from owned source declarations");
    }

    /// <summary>
    /// 验证 BuildingBlocks 中每条项目引用均指向现存项目文件
    /// </summary>
    [Fact]
    public void ProjectReferences_ResolveToExistingProjects()
    {
        var violations = Directory.GetFiles(RepositoryLayout.DotnetRoot, "*.csproj", SearchOption.AllDirectories)
            .Where(path => !IsBuildOutput(path))
            .SelectMany(FindUnresolvedProjectReferences)
            .ToArray();

        violations.Should().BeEmpty("retired project directories cannot be removed while ProjectReference entries still resolve to them");
    }

    /// <summary>
    /// 验证 provider-neutral 的应用契约与领域形状包不保留未使用的 Tw.Core 引用
    /// </summary>
    [Fact]
    public void ProviderNeutralApplicationPackages_DoNotReferenceTwCore()
    {
        var projectPaths = new[]
        {
            "Application/Tw.Application.Contracts/Tw.Application.Contracts.csproj",
            "Application/Tw.Domain/Tw.Domain.csproj"
        };
        var violations = projectPaths
            .Select(path => ProjectPath(RepositoryLayout.BuildingBlocksSrc, path))
            .Where(path => ReferencesTwCore(path)
                || CharterAllowsTwCore(Path.Combine(Path.GetDirectoryName(path)!, "package-charter.yaml")))
            .Select(RepositoryLayout.RepositoryRelativePath)
            .ToArray();

        violations.Should().BeEmpty(
            "provider-neutral contracts with no Tw.Core source usage must not retain a transitive framework dependency");
    }

    /// <summary>
    /// 判断项目是否通过源码项目或 NuGet 包身份引用 Tw.Core
    /// </summary>
    /// <param name="projectPath">待检查的项目文件路径</param>
    /// <returns>存在 Tw.Core 引用时返回 <see langword="true"/></returns>
    private static bool ReferencesTwCore(string projectPath)
    {
        return MsBuildProjectItems
            .Read(projectPath, "ProjectReference", "PackageReference")
            .SelectMany(reference => reference.ItemSpecs.Select(itemSpec => (reference.ItemName, ItemSpec: itemSpec)))
            .Any(reference =>
            {
                var dependencyName = string.Equals(
                    reference.ItemName,
                    "ProjectReference",
                    StringComparison.OrdinalIgnoreCase)
                    ? Path.GetFileNameWithoutExtension(reference.ItemSpec)
                    : reference.ItemSpec;

                return string.Equals(dependencyName, "Tw.Core", StringComparison.OrdinalIgnoreCase);
            });
    }

    /// <summary>
    /// 判断包章程的依赖白名单是否允许 Tw.Core
    /// </summary>
    /// <param name="charterPath">待检查的包章程路径</param>
    /// <returns>依赖白名单包含 Tw.Core 时返回 <see langword="true"/></returns>
    private static bool CharterAllowsTwCore(string charterPath)
    {
        return PackageCharterDependencyRules.AllowsDependency(charterPath, "Tw.Core");
    }

    /// <summary>
    /// 判断缺失的目标测试是否仍由清单记录的前身测试项目承接
    /// </summary>
    /// <param name="targetTestPath">相对于 BuildingBlocks/tests 的目标测试路径</param>
    /// <returns>存在现存前身测试项目时返回 true</returns>
    private static bool HasActivePredecessor(string targetTestPath)
    {
        if (!string.Equals(
                targetTestPath,
                "Http/Tw.Http.Tests/Tw.Http.Tests.csproj",
                StringComparison.Ordinal))
        {
            return false;
        }

        return RepositoryLayout.Topology.RetiredPackages
            .Where(package => string.Equals(package.PackageId, "Tw.Http.Client", StringComparison.Ordinal))
            .Where(package => string.Equals(
                package.TestProjectPath,
                "Http/Tw.Http.Client.Tests/Tw.Http.Client.Tests.csproj",
                StringComparison.Ordinal))
            .Where(package => string.Equals(package.ReplacementTestProjectPath, targetTestPath, StringComparison.Ordinal))
            .Select(package => package.TestProjectPath)
            .Where(path => path is not null)
            .Cast<string>()
            .Any(path => File.Exists(ProjectPath(RepositoryLayout.BuildingBlocksTests, path)));
    }

    /// <summary>
    /// 返回清单中每个淘汰包仍可能存在的 runtime 和测试项目文件路径
    /// </summary>
    /// <param name="retiredPackage">需要检查目录清理状态的淘汰包映射</param>
    /// <returns>相对于文件系统的历史项目文件路径</returns>
    private static IEnumerable<string> RetiredProjectFiles(RetiredPackageTopology retiredPackage)
    {
        if (retiredPackage.RuntimeProjectPath is not null)
        {
            yield return ProjectPath(RepositoryLayout.BuildingBlocksSrc, retiredPackage.RuntimeProjectPath);
        }

        if (retiredPackage.TestProjectPath is not null)
        {
            yield return ProjectPath(RepositoryLayout.BuildingBlocksTests, retiredPackage.TestProjectPath);
        }
    }

    /// <summary>
    /// 获取当前目录下全部项目文件的能力相对路径
    /// </summary>
    /// <param name="projectsRoot">BuildingBlocks 的生产或测试项目根目录</param>
    /// <returns>使用正斜杠表示的项目相对路径集合</returns>
    private static IEnumerable<string> CurrentProjectPaths(string projectsRoot)
    {
        return Directory.GetFiles(projectsRoot, "*.csproj", SearchOption.AllDirectories)
            .Select(projectPath => RepositoryLayout.NormalizePath(Path.GetRelativePath(projectsRoot, projectPath)));
    }

    /// <summary>
    /// 读取项目的有效根命名空间，未显式设置时使用 SDK 的项目名默认值
    /// </summary>
    /// <param name="projectPath">需要读取的项目文件</param>
    /// <returns>项目编译时使用的根命名空间</returns>
    private static string EffectiveRootNamespace(string projectPath)
    {
        var document = XDocument.Load(projectPath);
        return document.Descendants("RootNamespace")
            .Select(element => element.Value)
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))
            ?? Path.GetFileNameWithoutExtension(projectPath);
    }

    /// <summary>
    /// 查找指定项目中引用缺失或无法作为文件路径解析的 ProjectReference
    /// </summary>
    /// <param name="projectPath">需要检查的项目文件</param>
    /// <returns>每条无法解析引用的可读诊断信息</returns>
    private static IEnumerable<string> FindUnresolvedProjectReferences(string projectPath)
    {
        var projectDirectory = Path.GetDirectoryName(projectPath)!;
        foreach (var reference in MsBuildProjectItems.Read(projectPath, "ProjectReference"))
        {
            if (string.IsNullOrWhiteSpace(reference.Include) || reference.ItemSpecs.Count == 0)
            {
                yield return $"{RepositoryLayout.RepositoryRelativePath(projectPath)} has a ProjectReference without Include";
                continue;
            }

            foreach (var itemSpec in reference.ItemSpecs)
            {
                if (MsBuildProjectItems.ContainsExpression(itemSpec))
                {
                    yield return $"{RepositoryLayout.RepositoryRelativePath(projectPath)} uses an unresolved ProjectReference expression: {itemSpec}";
                    continue;
                }

                var referencedPath = Path.GetFullPath(Path.Combine(projectDirectory, itemSpec));
                if (!File.Exists(referencedPath))
                {
                    yield return $"{RepositoryLayout.RepositoryRelativePath(projectPath)} references missing project {RepositoryLayout.NormalizePath(itemSpec)}";
                }
            }
        }
    }

    /// <summary>
    /// 根据根目录和清单相对路径生成本机文件系统路径
    /// </summary>
    /// <param name="root">相对路径的根目录</param>
    /// <param name="relativePath">使用正斜杠表示的清单路径</param>
    /// <returns>可用于文件系统访问的绝对路径</returns>
    private static string ProjectPath(string root, string relativePath)
    {
        return Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar));
    }

    /// <summary>
    /// 组合嵌套命名空间声明并返回完整命名空间名称
    /// </summary>
    /// <param name="declaration">待解析的命名空间声明</param>
    /// <returns>包含外层命名空间的完整名称</returns>
    private static string FullNamespaceName(BaseNamespaceDeclarationSyntax declaration)
    {
        return string.Join(
            ".",
            declaration.AncestorsAndSelf()
                .OfType<BaseNamespaceDeclarationSyntax>()
                .Reverse()
                .Select(namespaceDeclaration => namespaceDeclaration.Name.ToString()));
    }

}
