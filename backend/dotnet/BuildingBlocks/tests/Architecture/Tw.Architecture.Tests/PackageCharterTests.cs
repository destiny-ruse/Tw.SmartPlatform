using AwesomeAssertions;
using Xunit;

namespace Tw.Architecture.Tests;

public sealed class PackageCharterTests
{
    private static readonly string[] RequiredFields =
    [
        "schema_version:",
        "package:",
        "owner:",
        "responsibility:",
        "in_scope:",
        "out_of_scope:",
        "public_capabilities:",
        "dependency_rules:"
    ];

    [Fact]
    public void EveryRuntimeProject_HasPackageCharterWithCanonicalPackageName()
    {
        var projects = Directory.GetFiles(RepositoryLayout.BuildingBlocksSrc, "*.csproj", SearchOption.AllDirectories);

        foreach (var project in projects)
        {
            var projectName = Path.GetFileNameWithoutExtension(project);
            var charter = Path.Combine(Path.GetDirectoryName(project)!, "package-charter.yaml");

            File.Exists(charter).Should().BeTrue($"{projectName} must declare package-charter.yaml");

            var text = File.ReadAllText(charter);
            text.Should().Contain($"package: {projectName}");
            foreach (var field in RequiredFields)
            {
                text.Should().Contain(field, $"{projectName} charter must use the formal schema");
            }
        }
    }

    [Fact]
    public void EveryRuntimeProject_UsesChineseNaturalLanguageCharterContent()
    {
        var charters = Directory.GetFiles(RepositoryLayout.BuildingBlocksSrc, "package-charter.yaml", SearchOption.AllDirectories);
        var violations = charters
            .Where(path => !ContainsChineseValue(File.ReadAllLines(path), "responsibility")
                || !ContainsChineseListValue(File.ReadAllLines(path), "in_scope")
                || !ContainsChineseListValue(File.ReadAllLines(path), "out_of_scope"))
            .Select(path => Path.GetRelativePath(RepositoryLayout.Root, path).Replace('\\', '/'))
            .ToArray();

        violations.Should().BeEmpty("charter responsibility, in_scope and out_of_scope must be written in Simplified Chinese");
    }

    private static bool ContainsChineseValue(string[] lines, string key)
    {
        var start = Array.FindIndex(lines, line => line.StartsWith($"{key}:", StringComparison.Ordinal));
        if (start < 0)
        {
            return false;
        }

        if (ContainsCjk(lines[start]))
        {
            return true;
        }

        return lines.Skip(start + 1)
            .TakeWhile(line => string.IsNullOrWhiteSpace(line) || line.StartsWith(" ", StringComparison.Ordinal))
            .Any(ContainsCjk);
    }

    private static bool ContainsChineseListValue(string[] lines, string key)
    {
        var start = Array.FindIndex(lines, line => line.StartsWith($"{key}:", StringComparison.Ordinal));
        if (start < 0)
        {
            return false;
        }

        return lines.Skip(start + 1)
            .TakeWhile(line => line.StartsWith("  - ", StringComparison.Ordinal))
            .Any(ContainsCjk);
    }

    private static bool ContainsCjk(string text)
    {
        return text.Any(ch => ch >= '\u4e00' && ch <= '\u9fff');
    }
}
