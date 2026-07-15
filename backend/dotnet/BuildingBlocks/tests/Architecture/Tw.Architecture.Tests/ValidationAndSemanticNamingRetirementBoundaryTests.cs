using System.Collections.Immutable;
using System.Xml.Linq;
using AwesomeAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Tw.Analyzers.Rules;
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
    /// 运行真实分析器所需的平台元数据引用
    /// </summary>
    private static readonly ImmutableArray<MetadataReference> PlatformMetadataReferences =
        CreatePlatformMetadataReferences();

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
    /// BuildingBlocks 与工具生产项目必须通过正式品牌标识符分析器
    /// </summary>
    /// <returns>异步分析全部生产项目的任务</returns>
    [Fact]
    public async Task ProductionProjects_PassForbiddenBrandIdentifierAnalyzer()
    {
        var violations = new List<string>();

        foreach (var project in ProductionProjects())
        {
            var diagnostics = await AnalyzeProjectAsync(project);
            violations.AddRange(diagnostics.Select(FormatDiagnostic));
        }

        violations.Should().BeEmpty(
            "生产声明必须使用职责语义名称，品牌规则只能由 ForbiddenBrandIdentifierAnalyzer 定义");
    }

    /// <summary>
    /// 正式分析器必须识别旧批准方法名并允许不含品牌分段的语义名称
    /// </summary>
    /// <returns>异步分析差异样例的任务</returns>
    [Fact]
    public async Task ForbiddenBrandIdentifierAnalyzer_DetectsLegacyApprovedMethodName()
    {
        const string source = """
        internal static class AnalyzerHelpers
        {
            internal static bool IsApprovedTwException() => true;
            internal static bool IsApprovedExceptionType() => true;
        }
        """;

        var diagnostics = await AnalyzeSourceAsync(source, "Tw.Analyzers");

        diagnostics.Should().ContainSingle();
        DiagnosticText(source, diagnostics[0]).Should().Be("IsApprovedTwException");
    }

    /// <summary>
    /// 正式分析器必须治理三类品牌分段且不得误报包含相同字母的普通单词
    /// </summary>
    /// <returns>异步分析正反例矩阵的任务</returns>
    [Fact]
    public async Task ForbiddenBrandIdentifierAnalyzer_ReportsBrandMatrixWithoutSubstringFalsePositives()
    {
        const string source = """
        internal sealed class TwOrderService { }
        internal sealed class AbpModule { }
        internal sealed class FurionService { }
        internal sealed class Twin
        {
            internal string Twice { get; init; } = string.Empty;
            internal string Between { get; init; } = string.Empty;
        }
        """;

        var diagnostics = await AnalyzeSourceAsync(source, "SemanticNames");

        diagnostics
            .Select(diagnostic => DiagnosticText(source, diagnostic))
            .Should()
            .BeEquivalentTo(["TwOrderService", "AbpModule", "FurionService"]);
    }

    /// <summary>
    /// 仅允许Tw.Core中顶层非泛型且继承Exception的Tw.Exceptions.TwException
    /// </summary>
    /// <returns>异步分析完整例外边界的任务</returns>
    [Fact]
    public async Task ForbiddenBrandIdentifierAnalyzer_EnforcesCompleteTwExceptionExceptionBoundary()
    {
        const string approvedSource = """
        namespace Tw.Exceptions;
        public sealed class TwException : System.Exception { }
        """;
        var approvedDiagnostics = await AnalyzeSourceAsync(approvedSource, "Tw.Core");
        approvedDiagnostics.Should().BeEmpty();

        var invalidScenarios = new (string AssemblyName, string Source)[]
        {
            ("Other.Core", approvedSource),
            ("Tw.Core", "namespace Other; public sealed class TwException : System.Exception { }"),
            ("Tw.Core", "namespace Tw.Exceptions; public sealed class TwException<T> : System.Exception { }"),
            ("Tw.Core", "namespace Tw.Exceptions; public sealed class Container { public sealed class TwException : System.Exception { } }"),
            ("Tw.Core", "namespace Tw.Exceptions; public sealed class TwException { }")
        };

        foreach (var scenario in invalidScenarios)
        {
            var diagnostics = await AnalyzeSourceAsync(scenario.Source, scenario.AssemblyName);
            diagnostics.Should().ContainSingle();
            DiagnosticText(scenario.Source, diagnostics[0]).Should().Be("TwException");
        }
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
    /// 枚举BuildingBlocks与工具目录中的生产项目及其编译源文件
    /// </summary>
    /// <returns>具有至少一个编译源文件的生产项目</returns>
    private static IEnumerable<ProductionProject> ProductionProjects()
    {
        var roots = new[]
        {
            RepositoryLayout.BuildingBlocksSrc,
            Path.Combine(RepositoryLayout.Root, "backend", "dotnet", "tools", "src")
        };

        return roots
            .SelectMany(root => Directory.GetFiles(root, "*.csproj", SearchOption.AllDirectories))
            .Where(IsProductionProject)
            .Select(CreateProductionProject)
            .Where(project => project.SourceFiles.Count > 0)
            .OrderBy(project => project.ProjectFile, StringComparer.Ordinal);
    }

    /// <summary>
    /// 判断项目是否属于生产项目而非模板测试项目
    /// </summary>
    /// <param name="projectFile">待判断的项目文件</param>
    /// <returns>项目路径不包含tests目录时返回 <see langword="true"/></returns>
    private static bool IsProductionProject(string projectFile)
    {
        return !RepositoryLayout.RepositoryRelativePath(projectFile)
            .Split('/', StringSplitOptions.RemoveEmptyEntries)
            .Any(segment => segment.Equals("tests", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// 从项目文件解析程序集名和编译源文件
    /// </summary>
    /// <param name="projectFile">生产项目文件</param>
    /// <returns>生产项目分析输入</returns>
    private static ProductionProject CreateProductionProject(string projectFile)
    {
        var document = XDocument.Load(projectFile);
        var projectDirectory = Path.GetDirectoryName(projectFile)
            ?? throw new InvalidOperationException($"无法解析项目目录: {projectFile}");
        var assemblyName = document
            .Descendants()
            .FirstOrDefault(element => element.Name.LocalName == "AssemblyName")?
            .Value
            .Trim();
        if (string.IsNullOrWhiteSpace(assemblyName))
        {
            assemblyName = Path.GetFileNameWithoutExtension(projectFile);
        }

        var defaultCompileEnabled = !document
            .Descendants()
            .Where(element => element.Name.LocalName is "EnableDefaultItems" or "EnableDefaultCompileItems")
            .Any(element => element.Value.Trim().Equals("false", StringComparison.OrdinalIgnoreCase));
        var sourceFiles = defaultCompileEnabled
            ? Directory.GetFiles(projectDirectory, "*.cs", SearchOption.AllDirectories)
                .Where(file => !IsBuildOutput(file))
                .ToArray()
            : ExplicitCompileFiles(document, projectDirectory).ToArray();

        return new ProductionProject(projectFile, assemblyName, sourceFiles);
    }

    /// <summary>
    /// 枚举关闭默认编译项项目中的显式源文件
    /// </summary>
    /// <param name="document">已加载的项目文档</param>
    /// <param name="projectDirectory">项目目录</param>
    /// <returns>存在于磁盘的显式编译源文件</returns>
    private static IEnumerable<string> ExplicitCompileFiles(XDocument document, string projectDirectory)
    {
        return document
            .Descendants()
            .Where(element => element.Name.LocalName == "Compile")
            .Select(element => element.Attribute("Include")?.Value)
            .Where(include => !string.IsNullOrWhiteSpace(include))
            .Select(include => Path.GetFullPath(include!, projectDirectory))
            .Where(File.Exists)
            .Where(file => !IsBuildOutput(file));
    }

    /// <summary>
    /// 使用项目程序集名和全部源文件运行正式品牌分析器
    /// </summary>
    /// <param name="project">生产项目分析输入</param>
    /// <returns>正式分析器报告的全部诊断</returns>
    private static async Task<ImmutableArray<Diagnostic>> AnalyzeProjectAsync(ProductionProject project)
    {
        var syntaxTrees = project.SourceFiles
            .Select(file => CSharpSyntaxTree.ParseText(
                File.ReadAllText(file),
                CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview),
                file,
                cancellationToken: TestContext.Current.CancellationToken))
            .Append(CSharpSyntaxTree.ParseText(
                "global using System;",
                CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview),
                Path.Combine(Path.GetDirectoryName(project.ProjectFile)!, "obj", "Architecture.GlobalUsings.g.cs"),
                cancellationToken: TestContext.Current.CancellationToken));
        var compilation = CSharpCompilation.Create(
            project.AssemblyName,
            syntaxTrees,
            PlatformMetadataReferences,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        return await RunForbiddenBrandAnalyzerAsync(compilation);
    }

    /// <summary>
    /// 使用指定程序集名编译临时源码并运行正式品牌分析器
    /// </summary>
    /// <param name="source">待分析的C#源码</param>
    /// <param name="assemblyName">编译单元程序集名</param>
    /// <returns>正式分析器报告的全部诊断</returns>
    private static Task<ImmutableArray<Diagnostic>> AnalyzeSourceAsync(string source, string assemblyName)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(
            source,
            CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview),
            cancellationToken: TestContext.Current.CancellationToken);
        var compilation = CSharpCompilation.Create(
            assemblyName,
            [syntaxTree],
            PlatformMetadataReferences,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        return RunForbiddenBrandAnalyzerAsync(compilation);
    }

    /// <summary>
    /// 在给定编译单元上运行生产使用的品牌标识符分析器
    /// </summary>
    /// <param name="compilation">待分析的编译单元</param>
    /// <returns>分析器报告的全部诊断</returns>
    private static async Task<ImmutableArray<Diagnostic>> RunForbiddenBrandAnalyzerAsync(Compilation compilation)
    {
        return await compilation
            .WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(new ForbiddenBrandIdentifierAnalyzer()))
            .GetAnalyzerDiagnosticsAsync(TestContext.Current.CancellationToken);
    }

    /// <summary>
    /// 创建当前运行时可信平台程序集的元数据引用
    /// </summary>
    /// <returns>供真实项目与差异样例使用的元数据引用</returns>
    private static ImmutableArray<MetadataReference> CreatePlatformMetadataReferences()
    {
        var trustedAssemblies = AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string
            ?? throw new InvalidOperationException("运行时未提供可信平台程序集列表");
        return trustedAssemblies
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Select(path => MetadataReference.CreateFromFile(path))
            .ToImmutableArray<MetadataReference>();
    }

    /// <summary>
    /// 格式化生产项目分析器诊断以便定位源码
    /// </summary>
    /// <param name="diagnostic">待格式化的分析器诊断</param>
    /// <returns>包含仓库路径、行号和消息的诊断文本</returns>
    private static string FormatDiagnostic(Diagnostic diagnostic)
    {
        var lineSpan = diagnostic.Location.GetLineSpan();
        var path = string.IsNullOrWhiteSpace(lineSpan.Path)
            ? "<unknown>"
            : RepositoryLayout.RepositoryRelativePath(lineSpan.Path);
        return $"{path}:{lineSpan.StartLinePosition.Line + 1} {diagnostic.Id} {diagnostic.GetMessage()}";
    }

    /// <summary>
    /// 从差异样例源码提取诊断命中的声明文本
    /// </summary>
    /// <param name="source">差异样例源码</param>
    /// <param name="diagnostic">分析器诊断</param>
    /// <returns>诊断命中的原始文本</returns>
    private static string DiagnosticText(string source, Diagnostic diagnostic)
    {
        return source.Substring(
            diagnostic.Location.SourceSpan.Start,
            diagnostic.Location.SourceSpan.Length);
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

    /// <summary>
    /// 判断生产源码路径是否属于SDK构建输出
    /// </summary>
    /// <param name="filePath">需要判定的绝对文件路径</param>
    /// <returns>文件位于bin或obj目录时返回 <see langword="true"/></returns>
    private static bool IsBuildOutput(string filePath)
    {
        var outputSegments = new[]
        {
            $"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}",
            $"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"
        };

        return outputSegments.Any(segment => filePath.Contains(
            segment,
            StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// 保存生产项目文件、程序集名和编译源文件
    /// </summary>
    /// <param name="ProjectFile">项目文件绝对路径</param>
    /// <param name="AssemblyName">项目编译使用的程序集名</param>
    /// <param name="SourceFiles">项目编译包含的源文件</param>
    private sealed record ProductionProject(
        string ProjectFile,
        string AssemblyName,
        IReadOnlyList<string> SourceFiles);
}
