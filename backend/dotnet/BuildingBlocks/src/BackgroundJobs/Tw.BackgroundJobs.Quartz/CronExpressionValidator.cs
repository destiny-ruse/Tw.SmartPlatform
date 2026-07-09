namespace Tw.BackgroundJobs.Quartz;

public static class CronExpressionValidator
{
    public static void Validate(string cronExpression)
    {
        if (!global::Quartz.CronExpression.IsValidExpression(cronExpression))
        {
            throw new InvalidOperationException("Background job Cron expression is invalid.");
        }
    }
}
