using AwesomeAssertions;
using Xunit;

namespace Tw.Architecture.Tests;

public sealed class PackageTopologyTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    [Fact]
    public void BuildingBlocks_RuntimeProjects_LiveUnderCapabilityFolders()
    {
        var srcRoot = Path.Combine(RepositoryRoot, "backend", "dotnet", "BuildingBlocks", "src");
        var projectFiles = Directory.GetFiles(srcRoot, "*.csproj", SearchOption.AllDirectories);

        projectFiles.Should().NotBeEmpty();
        projectFiles.Should().OnlyContain(
            path => Path.GetRelativePath(srcRoot, path).Replace('\\', '/').Count(ch => ch == '/') == 2,
            "runtime projects must use src/<Capability>/<Package>/<Package>.csproj");
    }

    [Fact]
    public void ForbiddenPackages_DoNotExist()
    {
        var forbiddenPackages = new[]
        {
            "Tw.Infrastructure",
            "Tw.Context",
            "Tw.ExecutionPipeline",
            "Tw.Swagger",
            "Tw.ApiVersioning",
            "Tw.Validation",
            "Tw.RateLimiting",
            "Tw.HealthChecks",
            "Tw.ObjectStorage",
            "Tw.Serialization",
            "Tw.Bff",
            "Tw.DynamicApi",
            "Tw.AspNetCore.DynamicApi",
            "Tw.ApplicationConfiguration",
            "Tw.Snowflake",
            "Tw.DistributedLock",
            "Tw.Autofac",
            "Tw.Localization.AspNetCore",
            "Tw.Grpc.AspNetCore",
            "Tw.Cqrs",
            "Tw.UnitOfWork",
            "Tw.Data.Abstractions",
            "Tw.Testing"
        };

        var srcRoot = Path.Combine(RepositoryRoot, "backend", "dotnet", "BuildingBlocks", "src");
        var actualPackages = Directory.GetFiles(srcRoot, "*.csproj", SearchOption.AllDirectories)
            .Select(Path.GetFileNameWithoutExtension)
            .ToHashSet(StringComparer.Ordinal);

        actualPackages.Should().NotIntersectWith(forbiddenPackages);
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
