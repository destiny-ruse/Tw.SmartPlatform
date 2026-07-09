namespace Tw.EventBus.Cap.TestBase;

/// <summary>表示 OutboxInboxAssertionExtensions 类型</summary>
public static class OutboxInboxAssertionExtensions
{
    /// <summary>执行 ShouldHaveNoOutboxInboxLeak 操作</summary>
    /// <param name="value">value 参数</param>
    public static void ShouldHaveNoOutboxInboxLeak(this object value)
    {
        ArgumentNullException.ThrowIfNull(value);
    }
}
