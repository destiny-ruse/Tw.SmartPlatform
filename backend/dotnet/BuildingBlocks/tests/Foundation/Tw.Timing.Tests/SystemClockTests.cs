using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Tw.Timing;
using Xunit;

namespace Tw.Timing.Tests;

/// <summary>验证 SystemClockTests 相关行为</summary>
public sealed class SystemClockTests
{
    /// <summary>验证 FixedClock_ReturnsConfiguredInstant 场景</summary>
    [Fact]
    public void FixedClock_ReturnsConfiguredInstant()
    {
        var instant = new DateTimeOffset(2026, 7, 9, 10, 0, 0, TimeSpan.FromHours(8));
        IClock clock = new FixedClock(instant);

        clock.Now.Should().Be(instant);
    }

    /// <summary>验证 AddTiming_RegistersSystemClock 场景</summary>
    [Fact]
    public void AddTiming_RegistersSystemClock()
    {
        var services = new ServiceCollection();

        services.AddTiming();

        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<IClock>().Should().BeOfType<SystemClock>();
    }
}
