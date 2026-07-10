namespace Tw.BackgroundJobs.Quartz;

/// <summary>
/// 封装CronExpressionValidator相关的数据和行为
/// </summary>
public static class CronExpressionValidator
{
    /// <summary>
    /// 校验当前配置或输入约束，并在非法时抛出异常
    /// </summary>
    /// <param name="cronExpression">用于提供cronExpression</param>
    public static void Validate(string cronExpression)
    {
        if (!global::Quartz.CronExpression.IsValidExpression(cronExpression))
        {
            throw new InvalidOperationException("Background job Cron expression is invalid.");
        }
    }
}
