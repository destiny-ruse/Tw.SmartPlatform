namespace Tw.TestBase;

public sealed class TestClock(DateTimeOffset utcNow)
{
    public DateTimeOffset UtcNow { get; private set; } = utcNow;

    public void AdvanceBy(TimeSpan duration)
    {
        UtcNow = UtcNow.Add(duration);
    }
}
