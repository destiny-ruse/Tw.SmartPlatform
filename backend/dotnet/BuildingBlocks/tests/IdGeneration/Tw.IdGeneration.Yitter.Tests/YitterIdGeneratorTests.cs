using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Tw.IdGeneration;
using Tw.IdGeneration.Yitter;
using Xunit;

namespace Tw.IdGeneration.Yitter.Tests;

/// <summary>验证 YitterIdGeneratorTests 相关行为</summary>
public sealed class YitterIdGeneratorTests
{
    /// <summary>验证 NewId_ReturnsPositiveLong 场景</summary>
    [Fact]
    public void NewId_ReturnsPositiveLong()
    {
        var generator = YitterIdGenerator.CreateForWorker(1);

        var id = generator.NewId();

        id.Should().BePositive();
    }

    /// <summary>验证 AddYitterIdGeneration_RegistersGenerator 场景</summary>
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
