using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Tw.IdGeneration;
using Tw.IdGeneration.Yitter;
using Xunit;

namespace Tw.IdGeneration.Yitter.Tests;

public sealed class YitterIdGeneratorTests
{
    [Fact]
    public void NewId_ReturnsPositiveLong()
    {
        var generator = YitterIdGenerator.CreateForWorker(1);

        var id = generator.NewId();

        id.Should().BePositive();
    }

    [Fact]
    public void AddYitterIdGeneration_RegistersGenerator()
    {
        var services = new ServiceCollection();

        services.AddYitterIdGeneration(1);

        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<IIdGenerator>()
            .NewId()
            .Should()
            .BePositive();
    }
}
