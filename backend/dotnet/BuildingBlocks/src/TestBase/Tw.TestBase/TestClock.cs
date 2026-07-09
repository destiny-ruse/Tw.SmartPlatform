namespace Tw.TestBase;

/// <summary>表示 TestClock 类型</summary>
public sealed class TestClock(DateTimeOffset utcNow)
{
    /// <summary>表示 UtcNow 属性</summary>
    public DateTimeOffset UtcNow { get; private set; } = utcNow;

    /// <summary>执行 AdvanceBy 操作</summary>
    /// <param name="duration">duration 参数</param>
    public void AdvanceBy(TimeSpan duration)
    {
        UtcNow = UtcNow.Add(duration);
    }
}
