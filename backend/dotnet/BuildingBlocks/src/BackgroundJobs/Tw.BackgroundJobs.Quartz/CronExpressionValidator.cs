namespace Tw.BackgroundJobs.Quartz;

/// <summary>表示 CronExpressionValidator 类型</summary>
public static class CronExpressionValidator
{
    /// <summary>执行 Validate 操作</summary>
    /// <param name="cronExpression">cronExpression 参数</param>
    public static void Validate(string cronExpression)
    {
        if (!global::Quartz.CronExpression.IsValidExpression(cronExpression))
        {
            throw new InvalidOperationException("Background job Cron expression is invalid.");
        }
    }
}
