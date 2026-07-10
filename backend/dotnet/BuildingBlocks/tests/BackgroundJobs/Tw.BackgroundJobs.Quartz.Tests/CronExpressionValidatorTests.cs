using AwesomeAssertions;
using Tw.BackgroundJobs.Quartz;
using Xunit;

namespace Tw.BackgroundJobs.Quartz.Tests;

/// <summary>
/// 覆盖CronExpressionValidator的核心行为和边界条件
/// </summary>
public sealed class CronExpressionValidatorTests
{
    /// <summary>
    /// 验证校验拒绝非法Cron
    /// </summary>
    [Fact]
    public void Validate_RejectsInvalidCron()
    {
        var act = () => CronExpressionValidator.Validate("not-a-cron");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Background job Cron expression is invalid.");
    }
}
