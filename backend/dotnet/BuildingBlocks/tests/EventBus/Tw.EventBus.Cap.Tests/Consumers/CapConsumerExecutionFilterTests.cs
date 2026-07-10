using AwesomeAssertions;
using Tw.EventBus.Cap.Consumers;
using Tw.EventBus.Cap.Inbox;
using Xunit;

namespace Tw.EventBus.Cap.Tests.Consumers;

/// <summary>
/// 覆盖CapConsumerExecution过滤器的核心行为和边界条件
/// </summary>
public sealed class CapConsumerExecutionFilterTests
{
    /// <summary>
    /// 验证执行异步拒绝缺少租户ShardOr文化Headers
    /// </summary>
    /// <returns>表示异步流程完成状态的任务</returns>
    [Fact]
    public async Task ExecuteAsync_RejectsMissingTenantShardOrCultureHeaders()
    {
        var filter = new CapConsumerExecutionFilter(new InMemoryInboxMessageStore());
        var context = new CapConsumerContext("message-1", TenantId: "", ShardId: "default", Culture: "zh-CN");

        var act = () => filter.ExecuteAsync(context, _ => Task.CompletedTask, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("CAP consumer message is missing tenant, shard, or culture context.");
    }

    /// <summary>
    /// 验证执行异步Dispatches命令一次针对重复消息
    /// </summary>
    /// <returns>表示异步流程完成状态的任务</returns>
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

    /// <summary>
    /// 覆盖InMemoryInbox消息存储的核心行为和边界条件
    /// </summary>
    private sealed class InMemoryInboxMessageStore : IInboxMessageStore
    {
        /// <summary>
        /// 保存当前类型处理流程依赖的messages
        /// </summary>
        private readonly HashSet<string> _messages = [];

        /// <summary>
        /// 尝试开始幂等请求处理并返回占用状态
        /// </summary>
        /// <param name="message">对外返回的安全错误消息</param>
        /// <param name="cancellationToken">用于传播调用方取消请求的令牌</param>
        /// <returns>异步流程完成后产生的bool</returns>
        public Task<bool> TryBeginAsync(InboxMessage message, CancellationToken cancellationToken)
        {
            return Task.FromResult(_messages.Add(message.MessageId));
        }

        /// <summary>
        /// 将幂等请求标记为完成并保存结果
        /// </summary>
        /// <param name="messageId">用于提供消息标识</param>
        /// <param name="cancellationToken">用于传播调用方取消请求的令牌</param>
        /// <returns>表示异步流程完成状态的任务</returns>
        public Task CompleteAsync(string messageId, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// 将幂等请求标记为失败
        /// </summary>
        /// <param name="messageId">用于提供消息标识</param>
        /// <param name="exception">用于模拟异常流程的异常实例</param>
        /// <param name="cancellationToken">用于传播调用方取消请求的令牌</param>
        /// <returns>表示异步流程完成状态的任务</returns>
        public Task FailAsync(string messageId, Exception exception, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
