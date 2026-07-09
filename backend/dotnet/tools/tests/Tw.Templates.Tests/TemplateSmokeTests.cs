using AwesomeAssertions;
using Xunit;

namespace Tw.Templates.Tests;

public sealed class TemplateSmokeTests
{
    [Fact]
    public void ServiceTemplate_DoesNotReferenceForbiddenPackages()
    {
        var root = Path.Combine(FindToolRoot(), "Tw.Templates", "content", "service");
        var files = Directory.GetFiles(root, "*.csproj", SearchOption.AllDirectories);
        var text = string.Join(Environment.NewLine, files.Select(File.ReadAllText));

        text.Should().NotContain("Tw.Infrastructure");
        text.Should().NotContain("Tw.UnitOfWork");
        text.Should().NotContain("Tw.Data.Abstractions");
        text.Should().NotContain("MassTransit");
    }

    private static string FindToolRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var tools = Path.Combine(directory.FullName, "backend", "dotnet", "tools");
            if (Directory.Exists(tools))
            {
                return tools;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Cannot locate backend/dotnet/tools.");
    }
}
