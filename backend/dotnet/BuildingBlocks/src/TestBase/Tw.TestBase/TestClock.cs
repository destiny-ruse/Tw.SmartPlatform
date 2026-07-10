namespace Tw.TestBase;

/// <summary>
/// 封装TestClock相关的数据和行为
/// </summary>
public sealed class TestClock(DateTimeOffset utcNow)
{
    /// <summary>
    /// UtcNow在当前对象中的业务含义
    /// </summary>
    public DateTimeOffset UtcNow { get; private set; } = utcNow;

    /// <summary>
    /// 说明AdvanceBy在当前类型中的职责
    /// </summary>
    /// <param name="duration">用于提供duration</param>
    public void AdvanceBy(TimeSpan duration)
    {
        UtcNow = UtcNow.Add(duration);
    }
}
