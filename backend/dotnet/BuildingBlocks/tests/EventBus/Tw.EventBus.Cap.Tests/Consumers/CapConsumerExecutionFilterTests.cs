using AwesomeAssertions;
using Tw.EventBus.Cap.Consumers;
using Tw.EventBus.Cap.Inbox;
using Xunit;

namespace Tw.EventBus.Cap.Tests.Consumers;

/// <summary>验证 CapConsumerExecutionFilterTests 相关行为</summary>
public sealed class CapConsumerExecutionFilterTests
{
    /// <summary>验证 ExecuteAsync_RejectsMissingTenantShardOrCultureHeaders 场景</summary>
    /// <returns>ExecuteAsync_RejectsMissingTenantShardOrCultureHeaders 的执行结果</returns>
    [Fact]
    public async Task ExecuteAsync_RejectsMissingTenantShardOrCultureHeaders()
    {
        var filter = new CapConsumerExecutionFilter(new InMemoryInboxMessageStore());
        var context = new CapConsumerContext("message-1", TenantId: "", ShardId: "default", Culture: "zh-CN");

        var act = () => filter.ExecuteAsync(context, _ => Task.CompletedTask, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("CAP consumer message is missing tenant, shard, or culture context.");
    }

    /// <summary>验证 ExecuteAsync_DispatchesCommandOnceForDuplicateMessage 场景</summary>
    /// <returns>ExecuteAsync_DispatchesCommandOnceForDuplicateMessage 的执行结果</returns>
    [Fact]
    public async Task ExecuteAsync_DispatchesCommandOnceForDuplicateMessage()
    {
        var inbox = new InMemoryInboxMessageStore();
        var dispatchCount = 0;
        var filter = new CapConsumerExecutionFilter(inbox);
        var context = new CapConsumerContext("message-1", "tenant-a", "orders-2026", "zh-CN");

        await filter.ExecuteAsync(context, _ =>
        {
            dispatchCount++;
            return Task.CompletedTask;
        }, CancellationToken.None);

        var duplicate = await filter.ExecuteAsync(context, _ =>
        {
            dispatchCount++;
            return Task.CompletedTask;
        }, CancellationToken.None);

        duplicate.Status.Should().Be(CapConsumerStatus.Duplicate);
        dispatchCount.Should().Be(1);
    }

    /// <summary>验证 InMemoryInboxMessageStore 相关行为</summary>
    private sealed class InMemoryInboxMessageStore : IInboxMessageStore
    {
        /// <summary>表示 _messages 字段</summary>
        private readonly HashSet<string> _messages = [];

        /// <summary>验证 TryBeginAsync 场景</summary>
        /// <param name="message">message 参数</param>
        /// <param name="cancellationToken">cancellationToken 参数</param>
        /// <returns>TryBeginAsync 的执行结果</returns>
        public Task<bool> TryBeginAsync(InboxMessage message, CancellationToken cancellationToken)
        {
            return Task.FromResult(_messages.Add(message.MessageId));
        }

        /// <summary>验证 CompleteAsync 场景</summary>
        /// <param name="messageId">messageId 参数</param>
        /// <param name="cancellationToken">cancellationToken 参数</param>
        /// <returns>CompleteAsync 的执行结果</returns>
        public Task CompleteAsync(string messageId, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        /// <summary>验证 FailAsync 场景</summary>
        /// <param name="messageId">messageId 参数</param>
        /// <param name="exception">exception 参数</param>
        /// <param name="cancellationToken">cancellationToken 参数</param>
        /// <returns>FailAsync 的执行结果</returns>
        public Task FailAsync(string messageId, Exception exception, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
