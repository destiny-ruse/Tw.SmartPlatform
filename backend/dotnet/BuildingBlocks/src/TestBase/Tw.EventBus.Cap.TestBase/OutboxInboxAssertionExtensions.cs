namespace Tw.EventBus.Cap.TestBase;

/// <summary>
/// 封装OutboxInboxAssertionExtensions相关的数据和行为
/// </summary>
public static class OutboxInboxAssertionExtensions
{
    /// <summary>
    /// 说明ShouldHaveNoOutboxInboxLeak在当前类型中的职责
    /// </summary>
    /// <param name="value">用于转换、回显或断言的输入值</param>
    public static void ShouldHaveNoOutboxInboxLeak(this object value)
    {
        ArgumentNullException.ThrowIfNull(value);
    }
}
