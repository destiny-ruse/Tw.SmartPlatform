using AwesomeAssertions;
using Tw.BackgroundJobs.Quartz;
using Xunit;

namespace Tw.BackgroundJobs.Quartz.Tests;

/// <summary>验证 CronExpressionValidatorTests 相关行为</summary>
public sealed class CronExpressionValidatorTests
{
    /// <summary>验证 Validate_RejectsInvalidCron 场景</summary>
    [Fact]
    public void Validate_RejectsInvalidCron()
    {
        var act = () => CronExpressionValidator.Validate("not-a-cron");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Background job Cron expression is invalid.");
    }
}
