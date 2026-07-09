namespace Tw.BackgroundJobs.Quartz;

/// <summary>表示 QuartzSchedulerStoreOptions 类型</summary>
public sealed class QuartzSchedulerStoreOptions
{
    /// <summary>表示 SchedulerDatabaseKey 属性</summary>
    public string SchedulerDatabaseKey { get; set; } = "Scheduler";

    /// <summary>表示 Clustered 属性</summary>
    public bool Clustered { get; set; } = true;

    /// <summary>执行 Validate 操作</summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(SchedulerDatabaseKey))
        {
            throw new InvalidOperationException("Background job Scheduler DB key cannot be empty.");
        }
    }
}
