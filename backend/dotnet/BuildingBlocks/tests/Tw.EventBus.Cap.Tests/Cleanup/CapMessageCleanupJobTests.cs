using AwesomeAssertions;
using Tw.EventBus.Cap.Cleanup;
using Xunit;

namespace Tw.EventBus.Cap.Tests.Cleanup;

public sealed class CapMessageCleanupJobTests
{
    [Fact]
    public void Options_Defaults_DoNotDeleteFailedMessages()
    {
        var options = CapMessageCleanupOptions.Default;

        options.DeleteFailedMessages.Should().BeFalse();
        options.BatchSize.Should().Be(500);
    }
}
