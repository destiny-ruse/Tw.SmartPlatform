namespace Tw.BackgroundJobs.Quartz;

public sealed class QuartzSchedulerStoreOptions
{
    public string SchedulerDatabaseKey { get; set; } = "Scheduler";

    public bool Clustered { get; set; } = true;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(SchedulerDatabaseKey))
        {
            throw new InvalidOperationException("Background job Scheduler DB key cannot be empty.");
        }
    }
}
