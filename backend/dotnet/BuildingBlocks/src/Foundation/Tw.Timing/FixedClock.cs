namespace Tw.Timing;

/// <summary>
/// 始终返回构造时指定时间的固定时钟，主要用于测试和可重复执行场景
/// </summary>
/// <param name="now">要返回的固定时间</param>
public sealed class FixedClock(DateTimeOffset now) : IClock
{
    /// <inheritdoc />
    public DateTimeOffset Now { get; } = now;
}
