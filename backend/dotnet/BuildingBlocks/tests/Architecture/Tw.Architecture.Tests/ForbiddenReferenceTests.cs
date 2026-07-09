using System.Xml.Linq;
using AwesomeAssertions;
using Xunit;

namespace Tw.Architecture.Tests;

public sealed class ForbiddenReferenceTests
{
    [Fact]
    public void RuntimeProjects_DoNotReferenceTestingPackages()
    {
        var projects = Directory.GetFiles(RepositoryLayout.BuildingBlocksSrc, "*.csproj", SearchOption.AllDirectories)
            .Where(path => !RepositoryLayout.IsTestBaseRuntimeProject(path));
        var forbidden = new[] { "Tw.TestBase", "Tw.AspNetCore.TestBase", "Tw.Data.SqlSugar.TestBase", "Tw.EventBus.Cap.TestBase" };

        foreach (var project in projects)
        {
            var document = XDocument.Load(project);
            var references = document.Descendants("ProjectReference")
                .Select(element => element.Attribute("Include")?.Value ?? string.Empty)
                .Concat(document.Descendants("PackageReference").Select(element => element.Attribute("Include")?.Value ?? string.Empty));

            references.Should().NotContain(reference => forbidden.Any(reference.Contains), $"{Path.GetFileName(project)} is a runtime project");
        }
    }

    [Fact]
    public void GatewayYarp_DoesNotReferenceApplicationDataOrEventBusPackages()
    {
        var project = Path.Combine(
            RepositoryLayout.BuildingBlocksSrc,
            "Gateway",
            "Tw.Gateway.Yarp",
            "Tw.Gateway.Yarp.csproj");
        if (!File.Exists(project))
        {
            return;
        }

        var forbidden = new[] { "Tw.Data", "Tw.Uow", "Tw.Application", "Tw.EventBus", "Tw.BackgroundJobs", "Tw.MultiTenancy", "Tw.Sharding" };
        var text = File.ReadAllText(project);

        text.Should().NotContainAny(forbidden);
    }
}
