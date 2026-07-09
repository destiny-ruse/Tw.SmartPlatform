using AwesomeAssertions;
using Tw.BackgroundJobs.Quartz;
using Xunit;

namespace Tw.BackgroundJobs.Quartz.Tests;

public sealed class CronExpressionValidatorTests
{
    [Fact]
    public void Validate_RejectsInvalidCron()
    {
        var act = () => CronExpressionValidator.Validate("not-a-cron");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Background job Cron expression is invalid.");
    }
}
