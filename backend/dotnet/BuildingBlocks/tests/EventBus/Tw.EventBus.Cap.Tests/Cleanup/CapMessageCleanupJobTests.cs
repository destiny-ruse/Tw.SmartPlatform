using AwesomeAssertions;
using Tw.EventBus.Cap.Cleanup;
using Xunit;

namespace Tw.EventBus.Cap.Tests.Cleanup;

/// <summary>验证 CapMessageCleanupJobTests 相关行为</summary>
public sealed class CapMessageCleanupJobTests
{
    /// <summary>验证 Options_Defaults_DoNotDeleteFailedMessages 场景</summary>
    [Fact]
    public void Options_Defaults_DoNotDeleteFailedMessages()
    {
        var options = CapMessageCleanupOptions.Default;

        options.DeleteFailedMessages.Should().BeFalse();
        options.BatchSize.Should().Be(500);
    }
}
