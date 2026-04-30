using FluentAssertions;
using System.Xml.Linq;
using Xunit;

namespace Tw.Core.Tests;

public class NuGetConfigTests
{
    [Fact]
    public void NuGetConfig_Maps_All_Configured_Sources_When_CentralPackageManagement_Uses_Multiple_Sources()
    {
        var dotnetRoot = FindDotNetRoot();
        var nugetConfig = XDocument.Load(Path.Combine(dotnetRoot.FullName, "NuGet.Config"));

        var sourceKeys = nugetConfig
            .Descendants("packageSources")
            .Elements("add")
            .Select(element => (string?)element.Attribute("key"))
            .Where(key => !string.IsNullOrWhiteSpace(key))
            .Cast<string>()
            .ToArray();

        if (sourceKeys.Length <= 1)
        {
            return;
        }

        var mappedSources = nugetConfig
            .Descendants("packageSourceMapping")
            .Elements("packageSource")
            .Select(element => new
            {
                Key = (string?)element.Attribute("key"),
                Patterns = element.Elements("package")
                    .Select(package => (string?)package.Attribute("pattern"))
                    .Where(pattern => !string.IsNullOrWhiteSpace(pattern))
                    .Cast<string>()
                    .ToArray()
            })
            .ToArray();

        mappedSources.Select(source => source.Key)
            .Should()
            .BeEquivalentTo(sourceKeys);

        mappedSources.Should().OnlyContain(source => source.Patterns.Contains("*"));
    }

    private static DirectoryInfo FindDotNetRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);

        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "NuGet.Config"))
                && File.Exists(Path.Combine(current.FullName, "Directory.Packages.props")))
            {
                return current;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("未找到 backend/dotnet 目录。");
    }
}
