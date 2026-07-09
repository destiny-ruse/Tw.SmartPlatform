using AwesomeAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace Tw.Architecture.Tests;

/// <summary>
/// 验证人工维护的 C# 类型和成员具备 XML 文档注释
/// </summary>
public sealed class XmlDocumentationTests
{
    /// <summary>
    /// 验证受治理源码中的声明具备 XML 文档注释
    /// </summary>
    [Fact]
    public void MaintainedCSharpMembers_HaveXmlDocumentation()
    {
        var roots = new[]
        {
            Path.Combine(RepositoryLayout.DotnetRoot, "BuildingBlocks", "src"),
            Path.Combine(RepositoryLayout.DotnetRoot, "BuildingBlocks", "tests"),
            Path.Combine(RepositoryLayout.DotnetRoot, "tools", "src"),
            Path.Combine(RepositoryLayout.DotnetRoot, "tools", "tests")
        };

        var violations = roots
            .Where(Directory.Exists)
            .SelectMany(root => Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories))
            .Where(IsMaintainedSourceFile)
            .SelectMany(FindUndocumentedDeclarations)
            .ToArray();

        violations.Should().BeEmpty("all maintained C# declarations must explain their contract in Simplified Chinese XML documentation");
    }

    private static bool IsMaintainedSourceFile(string path)
    {
        var relative = Path.GetRelativePath(RepositoryLayout.DotnetRoot, path).Replace('\\', '/');
        return !relative.Contains("/bin/", StringComparison.OrdinalIgnoreCase)
            && !relative.Contains("/obj/", StringComparison.OrdinalIgnoreCase)
            && !relative.EndsWith(".g.cs", StringComparison.Ordinal)
            && !relative.EndsWith(".Designer.cs", StringComparison.Ordinal)
            && !relative.EndsWith("GlobalUsings.cs", StringComparison.Ordinal);
    }

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

    private static bool HasXmlDocumentation(MemberDeclarationSyntax declaration)
    {
        return declaration.GetLeadingTrivia()
            .Any(trivia => trivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia)
                || trivia.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia));
    }

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
