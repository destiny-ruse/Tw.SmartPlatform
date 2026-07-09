namespace Tw.EventBus.Cap.Cleanup;

/// <summary>表示 CapMessageCleanupOptions 声明</summary>
public sealed record CapMessageCleanupOptions(int BatchSize, TimeSpan Retention, bool DeleteFailedMessages)
{
    /// <summary>表示 Default 属性</summary>
    public static CapMessageCleanupOptions Default { get; } = new(500, TimeSpan.FromDays(7), false);
}
