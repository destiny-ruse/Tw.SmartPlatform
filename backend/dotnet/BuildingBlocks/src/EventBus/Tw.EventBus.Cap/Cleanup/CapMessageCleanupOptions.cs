namespace Tw.EventBus.Cap.Cleanup;

/// <summary>
/// 配置Cap消息Cleanup的运行行为
/// </summary>
public sealed record CapMessageCleanupOptions(int BatchSize, TimeSpan Retention, bool DeleteFailedMessages)
{
    /// <summary>
    /// new在当前对象中的业务含义
    /// </summary>
    public static CapMessageCleanupOptions Default { get; } = new(500, TimeSpan.FromDays(7), false);
}
