using AwesomeAssertions;
using Xunit;

namespace Tw.Templates.Tests;

/// <summary>验证 TemplateSmokeTests 相关行为</summary>
public sealed class TemplateSmokeTests
{
    /// <summary>验证 ServiceTemplate_DoesNotReferenceForbiddenPackages 场景</summary>
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

    /// <summary>验证 FindToolRoot 场景</summary>
    /// <returns>FindToolRoot 的执行结果</returns>
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
