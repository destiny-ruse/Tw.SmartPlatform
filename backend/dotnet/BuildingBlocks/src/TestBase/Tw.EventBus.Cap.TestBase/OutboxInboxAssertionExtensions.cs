namespace Tw.EventBus.Cap.TestBase;

public static class OutboxInboxAssertionExtensions
{
    public static void ShouldHaveNoOutboxInboxLeak(this object value)
    {
        ArgumentNullException.ThrowIfNull(value);
    }
}
