using AwesomeAssertions;
using Tw.TestBase;
using Xunit;

namespace Tw.TestBase.Tests;

/// <summary>
/// 覆盖TestClock的核心行为和边界条件
/// </summary>
public sealed class TestClockTests
{
    /// <summary>
    /// 验证AdvanceByMovesUtcNowForward
    /// </summary>
    [Fact]
    public void AdvanceBy_MovesUtcNowForward()
    {
        var clock = new TestClock(new DateTimeOffset(2026, 7, 9, 0, 0, 0, TimeSpan.Zero));

        clock.AdvanceBy(TimeSpan.FromMinutes(5));

        clock.UtcNow.Should().Be(new DateTimeOffset(2026, 7, 9, 0, 5, 0, TimeSpan.Zero));
    }
}
