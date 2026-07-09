namespace Tw.Timing;

/// <summary>
/// 使用系统 UTC 时间作为当前时间来源的时钟实现
/// </summary>
public sealed class SystemClock : IClock
{
    /// <inheritdoc />
    public DateTimeOffset Now => DateTimeOffset.UtcNow;
}
