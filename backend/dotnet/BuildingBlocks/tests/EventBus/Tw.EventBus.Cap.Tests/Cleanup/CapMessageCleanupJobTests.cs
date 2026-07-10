using AwesomeAssertions;
using Tw.EventBus.Cap.Cleanup;
using Xunit;

namespace Tw.EventBus.Cap.Tests.Cleanup;

/// <summary>
/// 覆盖Cap消息Cleanup作业的核心行为和边界条件
/// </summary>
public sealed class CapMessageCleanupJobTests
{
    /// <summary>
    /// 验证选项DefaultsDo不删除FailedMessages
    /// </summary>
    [Fact]
    public void Options_Defaults_DoNotDeleteFailedMessages()
    {
        var options = CapMessageCleanupOptions.Default;

        options.DeleteFailedMessages.Should().BeFalse();
        options.BatchSize.Should().Be(500);
    }
}
