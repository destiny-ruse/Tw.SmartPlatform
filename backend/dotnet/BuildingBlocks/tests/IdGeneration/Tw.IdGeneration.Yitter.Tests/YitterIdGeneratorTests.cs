using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Tw.IdGeneration;
using Tw.IdGeneration.Yitter;
using Xunit;

namespace Tw.IdGeneration.Yitter.Tests;

/// <summary>
/// 覆盖Yitter标识Generator的核心行为和边界条件
/// </summary>
public sealed class YitterIdGeneratorTests
{
    /// <summary>
    /// 验证New标识返回Positive长整型
    /// </summary>
    [Fact]
    public void NewId_ReturnsPositiveLong()
    {
        var generator = YitterIdGenerator.CreateForWorker(1);

        var id = generator.NewId();

        id.Should().BePositive();
    }

    /// <summary>
    /// 验证添加Yitter标识Generation注册Generator
    /// </summary>
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
