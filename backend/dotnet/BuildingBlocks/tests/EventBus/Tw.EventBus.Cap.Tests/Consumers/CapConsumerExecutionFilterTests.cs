using AwesomeAssertions;
using Tw.EventBus.Cap.Consumers;
using Tw.EventBus.Cap.Inbox;
using Xunit;

namespace Tw.EventBus.Cap.Tests.Consumers;

public sealed class CapConsumerExecutionFilterTests
{
    [Fact]
    public async Task ExecuteAsync_RejectsMissingTenantShardOrCultureHeaders()
    {
        var filter = new CapConsumerExecutionFilter(new InMemoryInboxMessageStore());
        var context = new CapConsumerContext("message-1", TenantId: "", ShardId: "default", Culture: "zh-CN");

        var act = () => filter.ExecuteAsync(context, _ => Task.CompletedTask, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("CAP consumer message is missing tenant, shard, or culture context.");
    }

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

    private sealed class InMemoryInboxMessageStore : IInboxMessageStore
    {
        private readonly HashSet<string> _messages = [];

        public Task<bool> TryBeginAsync(InboxMessage message, CancellationToken cancellationToken)
        {
            return Task.FromResult(_messages.Add(message.MessageId));
        }

        public Task CompleteAsync(string messageId, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task FailAsync(string messageId, Exception exception, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
