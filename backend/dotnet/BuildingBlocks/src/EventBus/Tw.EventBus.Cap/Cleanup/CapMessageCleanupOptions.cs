namespace Tw.EventBus.Cap.Cleanup;

public sealed record CapMessageCleanupOptions(int BatchSize, TimeSpan Retention, bool DeleteFailedMessages)
{
    public static CapMessageCleanupOptions Default { get; } = new(500, TimeSpan.FromDays(7), false);
}
