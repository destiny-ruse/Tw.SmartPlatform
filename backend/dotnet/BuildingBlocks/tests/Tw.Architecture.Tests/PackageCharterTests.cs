using AwesomeAssertions;
using Xunit;

namespace Tw.Architecture.Tests;

public sealed class PackageCharterTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    [Fact]
    public void EveryRuntimeProject_HasPackageCharterWithCanonicalPackageName()
    {
        var srcRoot = Path.Combine(RepositoryRoot, "backend", "dotnet", "BuildingBlocks", "src");
        var projects = Directory.GetFiles(srcRoot, "*.csproj", SearchOption.AllDirectories);

        foreach (var project in projects)
        {
            var projectName = Path.GetFileNameWithoutExtension(project);
            var charter = Path.Combine(Path.GetDirectoryName(project)!, "package-charter.yaml");

            File.Exists(charter).Should().BeTrue($"{projectName} must declare package-charter.yaml");

            var text = File.ReadAllText(charter);
            text.Should().Contain($"package: {projectName}");
            text.Should().Contain("out_of_scope:");
            text.Should().Contain("public_capabilities:");
            text.Should().Contain("dependency_rules:");
        }
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "AGENTS.md")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("无法定位仓库根目录");
    }
}
