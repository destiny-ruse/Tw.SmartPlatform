using AwesomeAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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
    /// 生产声明中受治理的框架品牌分段
    /// </summary>
    private const string FrameworkBrandSegment = "Tw";

    /// <summary>
    /// 唯一允许保留品牌分段的生产类型声明路径
    /// </summary>
    private const string ApprovedExceptionTypePath =
        "backend/dotnet/BuildingBlocks/src/Foundation/Tw.Core/Exceptions/TwException.cs";

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
    /// BuildingBlocks 与工具生产源码的声明标识符只能为批准异常类型保留品牌分段
    /// </summary>
    [Fact]
    public void ProductionDeclarations_OnlyApprovedExceptionTypeUsesBrandIdentifierSegment()
    {
        var scan = ScanBrandIdentifierDeclarations(ProductionSourceFiles());

        scan.ApprovedExceptionTypeCount.Should().Be(
            1,
            "Tw.Exceptions.TwException 必须是生产源码中唯一批准的品牌标识符声明");
        scan.Violations.Should().BeEmpty(
            "生产声明应当使用职责语义名称，测试负例和历史文本不属于生产源码扫描范围");
    }

    /// <summary>
    /// 文件扫描能够识别旧批准方法名称，并允许不含品牌分段的语义名称
    /// </summary>
    [Fact]
    public void BrandIdentifierDeclarationScanner_DetectsLegacyApprovedMethodName()
    {
        using var directory = new TemporaryTestDirectory();
        var legacySource = directory.WriteFile(
            "LegacyAnalyzer.cs",
            "internal static bool IsApprovedTwException() => true;");
        var semanticSource = directory.WriteFile(
            "SemanticAnalyzer.cs",
            "internal static bool IsApprovedExceptionType() => true;");

        var legacyScan = ScanBrandIdentifierDeclarations([legacySource]);
        var semanticScan = ScanBrandIdentifierDeclarations([semanticSource]);

        legacyScan.Violations.Should().ContainSingle()
            .Which.Should().Contain("IsApprovedTwException");
        semanticScan.Violations.Should().BeEmpty();
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
    /// 枚举 BuildingBlocks 与工具目录下需要治理声明名称的生产 C# 源码
    /// </summary>
    /// <returns>排除构建输出后的生产源码绝对路径</returns>
    private static IEnumerable<string> ProductionSourceFiles()
    {
        var roots = new[]
        {
            RepositoryLayout.BuildingBlocksSrc,
            Path.Combine(RepositoryLayout.Root, "backend", "dotnet", "tools", "src")
        };

        return roots
            .SelectMany(root => Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories))
            .Where(file => !IsGeneratedOrHistoricalArtifact(file));
    }

    /// <summary>
    /// 使用 Roslyn 声明语法扫描源码中的框架品牌标识符分段
    /// </summary>
    /// <param name="sourceFiles">需要扫描的生产或临时 C# 源文件</param>
    /// <returns>违规诊断与批准异常类型声明数量</returns>
    private static BrandIdentifierScan ScanBrandIdentifierDeclarations(IEnumerable<string> sourceFiles)
    {
        var violations = new List<string>();
        var approvedExceptionTypeCount = 0;

        foreach (var sourceFile in sourceFiles)
        {
            var root = CSharpSyntaxTree.ParseText(File.ReadAllText(sourceFile)).GetRoot();
            foreach (var identifier in DeclarationIdentifiers(root))
            {
                if (!ContainsFrameworkBrandSegment(identifier.ValueText))
                {
                    continue;
                }

                if (IsApprovedExceptionTypeDeclaration(identifier, sourceFile))
                {
                    approvedExceptionTypeCount++;
                    continue;
                }

                var line = identifier.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                violations.Add(
                    $"{RepositoryLayout.RepositoryRelativePath(sourceFile)}:{line}：{identifier.ValueText}");
            }
        }

        return new BrandIdentifierScan(violations, approvedExceptionTypeCount);
    }

    /// <summary>
    /// 枚举 Roslyn 语法树中具有自有名称的声明标识符
    /// </summary>
    /// <param name="root">待扫描 C# 文件的语法树根节点</param>
    /// <returns>类型、成员、参数、变量及补充声明的标识符</returns>
    private static IEnumerable<SyntaxToken> DeclarationIdentifiers(SyntaxNode root)
    {
        foreach (var node in root.DescendantNodes())
        {
            var identifier = node switch
            {
                BaseTypeDeclarationSyntax declaration => declaration.Identifier,
                DelegateDeclarationSyntax declaration => declaration.Identifier,
                MethodDeclarationSyntax declaration => declaration.Identifier,
                LocalFunctionStatementSyntax declaration => declaration.Identifier,
                PropertyDeclarationSyntax declaration => declaration.Identifier,
                EventDeclarationSyntax declaration => declaration.Identifier,
                VariableDeclaratorSyntax declaration => declaration.Identifier,
                ParameterSyntax declaration => declaration.Identifier,
                TypeParameterSyntax declaration => declaration.Identifier,
                EnumMemberDeclarationSyntax declaration => declaration.Identifier,
                ForEachStatementSyntax declaration => declaration.Identifier,
                CatchDeclarationSyntax declaration => declaration.Identifier,
                SingleVariableDesignationSyntax declaration => declaration.Identifier,
                LabeledStatementSyntax declaration => declaration.Identifier,
                FromClauseSyntax declaration => declaration.Identifier,
                LetClauseSyntax declaration => declaration.Identifier,
                JoinClauseSyntax declaration => declaration.Identifier,
                JoinIntoClauseSyntax declaration => declaration.Identifier,
                QueryContinuationSyntax declaration => declaration.Identifier,
                UsingDirectiveSyntax { Alias: not null } declaration => declaration.Alias.Name.Identifier,
                _ => default
            };

            if (!identifier.IsKind(SyntaxKind.None))
            {
                yield return identifier;
            }
        }
    }

    /// <summary>
    /// 判断声明标识符是否包含独立的框架品牌语义分段
    /// </summary>
    /// <param name="identifier">待按大小写与下划线边界切分的声明标识符</param>
    /// <returns>存在受治理品牌分段时返回 <see langword="true"/></returns>
    private static bool ContainsFrameworkBrandSegment(string identifier)
    {
        var tokenStart = 0;

        for (var index = 0; index <= identifier.Length; index++)
        {
            var isEnd = index == identifier.Length;
            var isUnderscore = !isEnd && identifier[index] == '_';
            var startsNewToken = !isEnd && !isUnderscore && StartsNewIdentifierToken(identifier, index);
            if (!isEnd && !isUnderscore && !startsNewToken)
            {
                continue;
            }

            if (index - tokenStart == FrameworkBrandSegment.Length &&
                string.Compare(
                    identifier,
                    tokenStart,
                    FrameworkBrandSegment,
                    0,
                    FrameworkBrandSegment.Length,
                    StringComparison.OrdinalIgnoreCase) == 0)
            {
                return true;
            }

            tokenStart = isUnderscore ? index + 1 : index;
        }

        return false;
    }

    /// <summary>
    /// 判断标识符当前位置是否形成新的大小写语义分段
    /// </summary>
    /// <param name="identifier">待切分的声明标识符</param>
    /// <param name="index">当前字符索引</param>
    /// <returns>当前位置开始新的语义分段时返回 <see langword="true"/></returns>
    private static bool StartsNewIdentifierToken(string identifier, int index)
    {
        if (index == 0 || identifier[index - 1] == '_')
        {
            return false;
        }

        var previous = identifier[index - 1];
        var current = identifier[index];

        return (char.IsLower(previous) && char.IsUpper(current)) ||
            (char.IsUpper(previous) &&
             char.IsUpper(current) &&
             index + 1 < identifier.Length &&
             char.IsLower(identifier[index + 1]));
    }

    /// <summary>
    /// 判断声明是否为唯一批准保留品牌分段的异常类型
    /// </summary>
    /// <param name="identifier">包含待判断类型名称的声明标识符</param>
    /// <param name="sourceFile">声明所在 C# 源文件</param>
    /// <returns>声明路径、命名空间与类型名称全部匹配时返回 <see langword="true"/></returns>
    private static bool IsApprovedExceptionTypeDeclaration(SyntaxToken identifier, string sourceFile)
    {
        return identifier.Parent is ClassDeclarationSyntax declaration &&
            identifier.ValueText.Equals("TwException", StringComparison.Ordinal) &&
            RepositoryLayout.RepositoryRelativePath(sourceFile).Equals(
                ApprovedExceptionTypePath,
                StringComparison.Ordinal) &&
            DeclaredNamespace(declaration).Equals("Tw.Exceptions", StringComparison.Ordinal);
    }

    /// <summary>
    /// 组合类型声明的祖先命名空间并返回规范点分名称
    /// </summary>
    /// <param name="declaration">需要解析完整命名空间的类型声明</param>
    /// <returns>由标识符组成的完整命名空间</returns>
    private static string DeclaredNamespace(BaseTypeDeclarationSyntax declaration)
    {
        return string.Join(
            ".",
            declaration
                .Ancestors()
                .OfType<BaseNamespaceDeclarationSyntax>()
                .Reverse()
                .SelectMany(item => item.Name.DescendantTokens())
                .Where(token => token.IsKind(SyntaxKind.IdentifierToken))
                .Select(token => token.ValueText));
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
    /// 保存品牌声明扫描产生的违规诊断与批准例外数量
    /// </summary>
    /// <param name="Violations">包含文件、行号和声明标识符的违规诊断</param>
    /// <param name="ApprovedExceptionTypeCount">扫描范围内批准异常类型声明的数量</param>
    private sealed record BrandIdentifierScan(
        IReadOnlyList<string> Violations,
        int ApprovedExceptionTypeCount);
}
