using System.Text.RegularExpressions;
using AwesomeAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace Tw.Architecture.Tests;

/// <summary>
/// 验证人工维护的 C# 类型和成员具备有意义的 XML 文档注释
/// </summary>
public sealed class XmlDocumentationTests
{
    /// <summary>
    /// 匹配写成单行格式的 XML summary 摘要
    /// </summary>
    private static readonly Regex SingleLineSummaryPattern = new(
        @"^\s*///\s*<summary>.*</summary>\s*$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>
    /// 识别复述标识符、语法动作或兜底模板的文档注释句式
    /// </summary>
    private static readonly Regex[] TemplateDocumentationPatterns =
    [
        new(@"执行\s+\S+\s+操作", RegexOptions.Compiled | RegexOptions.CultureInvariant),
        new(@"验证\s+\S+\s+(场景|相关行为)", RegexOptions.Compiled | RegexOptions.CultureInvariant),
        new(@"<param\s+name=""(?<name>[^""]+)"">\k<name>\s+参数</param>", RegexOptions.Compiled | RegexOptions.CultureInvariant),
        new(@"<returns>[^<]+ 的执行结果</returns>", RegexOptions.Compiled | RegexOptions.CultureInvariant),
        new(@"表示\s+\S+\s+(声明|字段|属性|常量|类型)", RegexOptions.Compiled | RegexOptions.CultureInvariant),
        new(@"定义\s+\S+\s+契约", RegexOptions.Compiled | RegexOptions.CultureInvariant),
        new(@"根据当前契约完成\S+处理流程", RegexOptions.Compiled | RegexOptions.CultureInvariant),
        new(@"当前调用传入的\S+值", RegexOptions.Compiled | RegexOptions.CultureInvariant),
        new(@"当前对象暴露的\S+配置值", RegexOptions.Compiled | RegexOptions.CultureInvariant),
        new(@"\S+在当前模型中的语义", RegexOptions.Compiled | RegexOptions.CultureInvariant),
        new(@"处理\S+(对应的业务逻辑|主体逻辑)", RegexOptions.Compiled | RegexOptions.CultureInvariant),
        new(@"用于指定\S+", RegexOptions.Compiled | RegexOptions.CultureInvariant),
        new(@"解析或生成后的文本值", RegexOptions.Compiled | RegexOptions.CultureInvariant),
        new(@"当前流程完成后产生的返回值", RegexOptions.Compiled | RegexOptions.CultureInvariant),
        new(@"当前成员", RegexOptions.Compiled | RegexOptions.CultureInvariant)
    ];

    /// <summary>
    /// 验证受治理源码中的声明具备 XML 文档注释
    /// </summary>
    [Fact]
    public void MaintainedCSharpMembers_HaveXmlDocumentation()
    {
        var violations = DocumentationFilePaths()
            .SelectMany(FindUndocumentedDeclarations)
            .ToArray();

        violations.Should().BeEmpty("all maintained C# declarations must explain their contract in Simplified Chinese XML documentation");
    }

    /// <summary>
    /// 验证受治理源码中的 XML 文档注释使用多行摘要并避免模板化句式
    /// </summary>
    [Fact]
    public void MaintainedXmlDocumentation_UsesMeaningfulMultilineSummaries()
    {
        var violations = DocumentationFilePaths()
            .SelectMany(FindDocumentationStyleViolations)
            .ToArray();

        violations.Should().BeEmpty("XML documentation must describe concrete contracts instead of repeating syntax or identifiers");
    }

    /// <summary>
    /// 返回需要参与 XML 文档注释治理的 C# 源码文件路径
    /// </summary>
    /// <returns>受治理的 C# 源码文件路径集合</returns>
    private static IEnumerable<string> DocumentationFilePaths()
    {
        var roots = new[]
        {
            Path.Combine(RepositoryLayout.DotnetRoot, "BuildingBlocks", "src"),
            Path.Combine(RepositoryLayout.DotnetRoot, "BuildingBlocks", "tests"),
            Path.Combine(RepositoryLayout.DotnetRoot, "tools", "src"),
            Path.Combine(RepositoryLayout.DotnetRoot, "tools", "tests")
        };

        return roots
            .Where(Directory.Exists)
            .SelectMany(root => Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories))
            .Where(IsMaintainedSourceFile);
    }

    /// <summary>
    /// 判断源码文件是否由仓库人工维护并需要参与文档注释治理
    /// </summary>
    /// <param name="path">待判断的源码文件绝对路径</param>
    /// <returns>文件属于人工维护源码时返回 true</returns>
    private static bool IsMaintainedSourceFile(string path)
    {
        var relative = Path.GetRelativePath(RepositoryLayout.DotnetRoot, path).Replace('\\', '/');
        return !relative.Contains("/bin/", StringComparison.OrdinalIgnoreCase)
            && !relative.Contains("/obj/", StringComparison.OrdinalIgnoreCase)
            && !relative.EndsWith(".g.cs", StringComparison.Ordinal)
            && !relative.EndsWith(".Designer.cs", StringComparison.Ordinal)
            && !relative.EndsWith("GlobalUsings.cs", StringComparison.Ordinal);
    }

    /// <summary>
    /// 查找指定源码文件中缺少 XML 文档注释的声明
    /// </summary>
    /// <param name="path">待扫描的源码文件绝对路径</param>
    /// <returns>包含文件路径、行号和声明名称的违规描述</returns>
    private static IEnumerable<string> FindUndocumentedDeclarations(string path)
    {
        var tree = CSharpSyntaxTree.ParseText(File.ReadAllText(path));
        var root = tree.GetCompilationUnitRoot();
        foreach (var declaration in root.DescendantNodes().OfType<MemberDeclarationSyntax>())
        {
            if (!RequiresDocumentation(declaration) || HasXmlDocumentation(declaration))
            {
                continue;
            }

            var line = declaration.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
            yield return $"{Path.GetRelativePath(RepositoryLayout.Root, path).Replace('\\', '/')}:{line} {DeclarationName(declaration)}";
        }
    }

    /// <summary>
    /// 查找指定源码文件中格式或内容不合格的 XML 文档注释
    /// </summary>
    /// <param name="path">待扫描的源码文件绝对路径</param>
    /// <returns>包含文件路径、行号和命中内容的违规描述</returns>
    private static IEnumerable<string> FindDocumentationStyleViolations(string path)
    {
        var relative = Path.GetRelativePath(RepositoryLayout.Root, path).Replace('\\', '/');
        var lines = File.ReadAllLines(path);

        for (var index = 0; index < lines.Length; index++)
        {
            var line = lines[index];
            if (!line.TrimStart().StartsWith("///", StringComparison.Ordinal))
            {
                continue;
            }

            if (SingleLineSummaryPattern.IsMatch(line))
            {
                yield return $"{relative}:{index + 1} summary must use multiline XML documentation format";
            }

            foreach (var pattern in TemplateDocumentationPatterns)
            {
                if (pattern.IsMatch(line))
                {
                    yield return $"{relative}:{index + 1} template documentation: {line.Trim()}";
                }
            }
        }
    }

    /// <summary>
    /// 判断声明节点是否属于必须编写 XML 文档注释的 C# 成员
    /// </summary>
    /// <param name="declaration">待判断的 C# 成员声明节点</param>
    /// <returns>声明需要 XML 文档注释时返回 true</returns>
    private static bool RequiresDocumentation(MemberDeclarationSyntax declaration)
    {
        return declaration is BaseTypeDeclarationSyntax
            or DelegateDeclarationSyntax
            or EnumMemberDeclarationSyntax
            or BaseMethodDeclarationSyntax
            or PropertyDeclarationSyntax
            or FieldDeclarationSyntax
            or EventDeclarationSyntax
            or EventFieldDeclarationSyntax;
    }

    /// <summary>
    /// 判断声明节点前方是否已经存在 XML 文档注释
    /// </summary>
    /// <param name="declaration">待判断的 C# 成员声明节点</param>
    /// <returns>声明前方存在 XML 文档注释时返回 true</returns>
    private static bool HasXmlDocumentation(MemberDeclarationSyntax declaration)
    {
        return declaration.GetLeadingTrivia()
            .Any(trivia => trivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia)
                || trivia.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia));
    }

    /// <summary>
    /// 返回声明节点在违规信息中使用的可读名称
    /// </summary>
    /// <param name="declaration">待提取名称的 C# 成员声明节点</param>
    /// <returns>类型、方法、属性、字段或事件的名称</returns>
    private static string DeclarationName(MemberDeclarationSyntax declaration)
    {
        return declaration switch
        {
            BaseTypeDeclarationSyntax type => type.Identifier.Text,
            DelegateDeclarationSyntax @delegate => @delegate.Identifier.Text,
            EnumMemberDeclarationSyntax enumMember => enumMember.Identifier.Text,
            BaseMethodDeclarationSyntax method => method switch
            {
                ConstructorDeclarationSyntax constructor => constructor.Identifier.Text,
                MethodDeclarationSyntax namedMethod => namedMethod.Identifier.Text,
                ConversionOperatorDeclarationSyntax conversion => conversion.OperatorKeyword.Text,
                OperatorDeclarationSyntax @operator => @operator.OperatorToken.Text,
                _ => method.Kind().ToString()
            },
            PropertyDeclarationSyntax property => property.Identifier.Text,
            FieldDeclarationSyntax field => string.Join(", ", field.Declaration.Variables.Select(variable => variable.Identifier.Text)),
            EventDeclarationSyntax @event => @event.Identifier.Text,
            EventFieldDeclarationSyntax eventField => string.Join(", ", eventField.Declaration.Variables.Select(variable => variable.Identifier.Text)),
            _ => declaration.Kind().ToString()
        };
    }
}
