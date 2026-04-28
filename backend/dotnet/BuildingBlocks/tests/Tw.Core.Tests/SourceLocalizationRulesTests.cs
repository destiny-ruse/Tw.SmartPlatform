using FluentAssertions;
using System.Text.RegularExpressions;
using Xunit;

namespace Tw.Core.Tests;

public class SourceLocalizationRulesTests
{
    private static readonly string[] SourceRootPaths =
    [
        "backend/dotnet/BuildingBlocks/src/Tw.Core",
        "backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests",
        "backend/dotnet/BuildingBlocks/tests/Tw.TestBase"
    ];

    private static readonly Regex NaturalEnglishWordRegex = new(
        @"\b(the|when|returns?|gets?|provides|computes|verifies|encrypts|decrypts|signs|hashes|uses|creates|finds|checks|converts|normalizes|selects|invokes|disposes|cancels|supplied|configured|current|required|expected|matching|random|password|private|public|lower|upper|inclusive|exclusive|range|source|value|input|output|string|bytes|collection|stream|file|type|assembly|cache|metadata|attributes|helpers|extensions|empty|null|true|false|cannot|must|should|with|without|using|otherwise|available|valid|invalid|supports?|boundary|operation|operation)\b",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    private static readonly Regex XmlTagRegex = new(
        "<[^>]+>",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex ThrowExpressionRegex = new(
        @"throw\s+new\s+(?<type>[A-Za-z0-9_.]+Exception)\s*\((?<args>.*?)\)\s*;",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Singleline);

    private static readonly Regex ThrowSwitchArmRegex = new(
        @"throw\s+new\s+(?<type>[A-Za-z0-9_.]+Exception)\s*\((?<args>[^(\r\n]*(?:\([^)]*\)[^(\r\n]*)*)\)\s*,",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex ThrowIfRegex = new(
        @"\b[A-Za-z0-9_.]+Exception\.ThrowIf[A-Za-z]+\s*\(",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex StringLiteralRegex = new(
        @"\$?""(?<value>(?:\\.|[^""\\])*)""",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Singleline);

    private static readonly Regex IdentifierLiteralRegex = new(
        "^[A-Za-z_][A-Za-z0-9_]*$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex PropertyDeclarationRegex = new(
        @"^\s*(?:(?:public|protected|internal|private)\s+)?(?:static\s+|virtual\s+|override\s+|sealed\s+|abstract\s+|required\s+|new\s+|readonly\s+)*[A-Za-z0-9_<>,.?[\]\s]+\s+[A-Za-z_][A-Za-z0-9_]*\s*\{",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex ChineseTextRegex = new(
        @"\p{IsCJKUnifiedIdeographs}",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly string[] CommentEndingPeriods = ["。", "."];

    private static readonly string[] PropertyCommentBannedPhrases = ["获取或设置", "获取", "设置"];

    private static readonly string[] LocalizedMessageExpressions =
    [
        "profile.KeyLengthMessage",
        "profile.IvLengthMessage"
    ];

    [Fact]
    public void Source_Comments_Use_Simplified_Chinese()
    {
        var violations = EnumerateSourceFiles()
            .SelectMany(file => EnumerateCommentLines(file)
                .Where(comment => NaturalEnglishWordRegex.IsMatch(NormalizeCommentText(comment.Text)))
                .Select(comment => $"{RelativePath(file)}:{comment.LineNumber} {comment.Text.Trim()}"))
            .ToArray();

        violations.Should().BeEmpty("注释中的自然语言说明应使用简体中文");
    }

    [Fact]
    public void Source_Comments_Do_Not_End_With_Period()
    {
        var violations = EnumerateSourceFiles()
            .SelectMany(file => EnumerateCommentLines(file)
                .Where(comment => CommentTextEndsWithPeriod(comment.Text))
                .Select(comment => $"{RelativePath(file)}:{comment.LineNumber} {comment.Text.Trim()}"))
            .ToArray();

        violations.Should().BeEmpty("注释末尾不应使用中英文句号");
    }

    [Fact]
    public void Line_Comments_Keep_Space_After_Marker()
    {
        var violations = EnumerateSourceFiles()
            .SelectMany(FindLineCommentSpacingViolations)
            .ToArray();

        violations.Should().BeEmpty("单行注释标记 // 与注释文本之间应保留一个空格");
    }

    [Fact]
    public void Property_Comments_Describe_Meaning_Directly()
    {
        var violations = EnumerateSourceFiles()
            .SelectMany(FindPropertyCommentPhraseViolations)
            .ToArray();

        violations.Should().BeEmpty("属性注释应直接描述属性含义，不使用获取、设置或获取或设置");
    }

    [Fact]
    public void Explicit_Thrown_Exception_Messages_Use_Simplified_Chinese()
    {
        var violations = EnumerateSourceFiles()
            .SelectMany(FindExceptionMessageViolations)
            .ToArray();

        violations.Should().BeEmpty("显式抛出的异常应提供简体中文消息，参数名字符串除外");
    }

    private static IEnumerable<FileInfo> EnumerateSourceFiles()
    {
        var repositoryRoot = FindRepositoryRoot();

        return SourceRootPaths
            .Select(path => new DirectoryInfo(Path.Combine(repositoryRoot.FullName, path)))
            .Where(directory => directory.Exists)
            .SelectMany(directory => directory.EnumerateFiles("*.cs", SearchOption.AllDirectories))
            .Where(file => !file.FullName.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .Where(file => !file.FullName.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase));
    }

    private static IEnumerable<(int LineNumber, string Text)> EnumerateCommentLines(FileInfo file)
    {
        var lineNumber = 0;
        var inBlockComment = false;

        foreach (var line in File.ReadLines(file.FullName))
        {
            lineNumber++;
            var trimmed = line.TrimStart();

            if (inBlockComment)
            {
                var text = trimmed;
                if (text.Contains("*/", StringComparison.Ordinal))
                {
                    text = text[..text.IndexOf("*/", StringComparison.Ordinal)];
                    inBlockComment = false;
                }

                yield return (lineNumber, text.TrimStart('*', ' '));
                continue;
            }

            if (trimmed.StartsWith("///", StringComparison.Ordinal))
            {
                yield return (lineNumber, trimmed[3..]);
                continue;
            }

            if (trimmed.StartsWith("//", StringComparison.Ordinal))
            {
                yield return (lineNumber, trimmed[2..]);
                continue;
            }

            if (trimmed.StartsWith("/*", StringComparison.Ordinal))
            {
                var text = trimmed[2..];
                if (text.Contains("*/", StringComparison.Ordinal))
                {
                    text = text[..text.IndexOf("*/", StringComparison.Ordinal)];
                }
                else
                {
                    inBlockComment = true;
                }

                yield return (lineNumber, text);
            }
        }
    }

    private static IEnumerable<string> FindLineCommentSpacingViolations(FileInfo file)
    {
        var lineNumber = 0;

        foreach (var line in File.ReadLines(file.FullName))
        {
            lineNumber++;
            var trimmed = line.TrimStart();

            if (!trimmed.StartsWith("//", StringComparison.Ordinal) ||
                trimmed.StartsWith("///", StringComparison.Ordinal) ||
                trimmed.Length <= 2)
            {
                continue;
            }

            if (trimmed[2] != ' ')
            {
                yield return $"{RelativePath(file)}:{lineNumber} {trimmed}";
            }
        }
    }

    private static IEnumerable<string> FindPropertyCommentPhraseViolations(FileInfo file)
    {
        var pendingXmlComments = new List<(int LineNumber, string Text)>();
        var lines = File.ReadAllLines(file.FullName);

        for (var index = 0; index < lines.Length; index++)
        {
            var trimmed = lines[index].TrimStart();

            if (trimmed.StartsWith("///", StringComparison.Ordinal))
            {
                pendingXmlComments.Add((index + 1, trimmed[3..]));
                continue;
            }

            if (pendingXmlComments.Count == 0)
            {
                continue;
            }

            if (trimmed.Length == 0)
            {
                pendingXmlComments.Clear();
                continue;
            }

            if (trimmed.StartsWith("[", StringComparison.Ordinal))
            {
                continue;
            }

            if (PropertyDeclarationRegex.IsMatch(trimmed))
            {
                var commentText = NormalizeCommentText(string.Join(' ', pendingXmlComments.Select(comment => comment.Text)));

                foreach (var bannedPhrase in PropertyCommentBannedPhrases)
                {
                    if (commentText.Contains(bannedPhrase, StringComparison.Ordinal))
                    {
                        yield return $"{RelativePath(file)}:{pendingXmlComments[0].LineNumber} 属性注释包含“{bannedPhrase}”";
                    }
                }
            }

            pendingXmlComments.Clear();
        }
    }

    private static IEnumerable<string> FindExceptionMessageViolations(FileInfo file)
    {
        var source = File.ReadAllText(file.FullName);

        foreach (Match match in ThrowIfRegex.Matches(source))
        {
            yield return $"{RelativePath(file)}:{GetLineNumber(source, match.Index)} 禁止使用默认英文消息的 ThrowIf... 守卫";
        }

        foreach (Match match in ThrowExpressionRegex.Matches(source).Concat(ThrowSwitchArmRegex.Matches(source)))
        {
            var args = match.Groups["args"].Value.Trim();
            var lineNumber = GetLineNumber(source, match.Index);

            if (args.Length == 0)
            {
                yield return $"{RelativePath(file)}:{lineNumber} 异常缺少显式中文消息";
                continue;
            }

            var stringValues = StringLiteralRegex.Matches(args)
                .Select(valueMatch => valueMatch.Groups["value"].Value)
                .Where(value => !IdentifierLiteralRegex.IsMatch(value))
                .ToArray();

            if (stringValues.Length == 0)
            {
                if (!LocalizedMessageExpressions.Any(expression => args.Contains(expression, StringComparison.Ordinal)))
                {
                    yield return $"{RelativePath(file)}:{lineNumber} 异常缺少可识别的中文消息";
                }

                continue;
            }

            foreach (var stringValue in stringValues)
            {
                if (!ChineseTextRegex.IsMatch(stringValue))
                {
                    yield return $"{RelativePath(file)}:{lineNumber} 异常消息不是简体中文：{stringValue}";
                }
            }
        }
    }

    private static string NormalizeCommentText(string text)
    {
        var withoutXml = XmlTagRegex.Replace(text, " ");
        return withoutXml.Replace("&lt;", " ", StringComparison.Ordinal)
            .Replace("&gt;", " ", StringComparison.Ordinal)
            .Replace("&amp;", " ", StringComparison.Ordinal)
            .Replace("langword", " ", StringComparison.OrdinalIgnoreCase)
            .Replace("paramref", " ", StringComparison.OrdinalIgnoreCase)
            .Replace("typeparamref", " ", StringComparison.OrdinalIgnoreCase)
            .Replace("cref", " ", StringComparison.OrdinalIgnoreCase);
    }

    private static bool CommentTextEndsWithPeriod(string text)
    {
        var normalized = NormalizeCommentText(text).Trim();
        return normalized.Length > 0 &&
            CommentEndingPeriods.Any(ending => normalized.EndsWith(ending, StringComparison.Ordinal));
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);

        while (current is not null)
        {
            var marker = Path.Combine(current.FullName, "backend", "dotnet", "BuildingBlocks", "src", "Tw.Core", "Check.cs");
            if (File.Exists(marker))
            {
                return current;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException("无法定位仓库根目录。");
    }

    private static string RelativePath(FileInfo file)
    {
        return Path.GetRelativePath(FindRepositoryRoot().FullName, file.FullName)
            .Replace(Path.DirectorySeparatorChar, '/');
    }

    private static int GetLineNumber(string source, int index)
    {
        return source[..index].Count(character => character == '\n') + 1;
    }
}
