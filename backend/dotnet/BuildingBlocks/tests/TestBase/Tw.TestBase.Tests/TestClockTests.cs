using AwesomeAssertions;
using Tw.TestBase;
using Xunit;

namespace Tw.TestBase.Tests;

/// <summary>验证 TestClockTests 相关行为</summary>
public sealed class TestClockTests
{
    /// <summary>验证 AdvanceBy_MovesUtcNowForward 场景</summary>
    [Fact]
    public void AdvanceBy_MovesUtcNowForward()
    {
        var clock = new TestClock(new DateTimeOffset(2026, 7, 9, 0, 0, 0, TimeSpan.Zero));

        clock.AdvanceBy(TimeSpan.FromMinutes(5));

        clock.UtcNow.Should().Be(new DateTimeOffset(2026, 7, 9, 0, 5, 0, TimeSpan.Zero));
    }
}
