namespace Tw.BackgroundJobs.Quartz;

/// <summary>
/// 配置QuartzScheduler存储的运行行为
/// </summary>
public sealed class QuartzSchedulerStoreOptions
{
    /// <summary>
    /// SchedulerDatabase键在当前对象中的业务含义
    /// </summary>
    public string SchedulerDatabaseKey { get; set; } = "Scheduler";

    /// <summary>
    /// Clustered在当前对象中的业务含义
    /// </summary>
    public bool Clustered { get; set; } = true;

    /// <summary>
    /// 校验当前配置或输入约束，并在非法时抛出异常
    /// </summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(SchedulerDatabaseKey))
        {
            throw new InvalidOperationException("Background job Scheduler DB key cannot be empty.");
        }
    }
}
