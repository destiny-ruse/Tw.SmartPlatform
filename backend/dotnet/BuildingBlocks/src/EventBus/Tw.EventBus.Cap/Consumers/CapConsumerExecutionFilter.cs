using Tw.EventBus.Cap.Inbox;

namespace Tw.EventBus.Cap.Consumers;

/// <summary>表示 CapConsumerExecutionFilter 类型</summary>
public sealed class CapConsumerExecutionFilter(IInboxMessageStore inboxStore)
{
    /// <summary>执行 ExecuteAsync 操作</summary>
    /// <param name="context">context 参数</param>
    /// <param name="dispatch">dispatch 参数</param>
    /// <param name="cancellationToken">cancellationToken 参数</param>
    /// <returns>ExecuteAsync 的执行结果</returns>
    public async Task<CapConsumerResult> ExecuteAsync(
        CapConsumerContext context,
        Func<CancellationToken, Task> dispatch,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(dispatch);

        if (string.IsNullOrWhiteSpace(context.TenantId)
            || string.IsNullOrWhiteSpace(context.ShardId)
            || string.IsNullOrWhiteSpace(context.Culture))
        {
            throw new InvalidOperationException("CAP consumer message is missing tenant, shard, or culture context.");
        }

        var inboxMessage = new InboxMessage(
            context.MessageId,
            context.TenantId,
            context.ShardId,
            context.Culture,
            DateTimeOffset.UtcNow);

        if (!await inboxStore.TryBeginAsync(inboxMessage, cancellationToken))
        {
            return new CapConsumerResult(CapConsumerStatus.Duplicate);
        }

        try
        {
            await dispatch(cancellationToken);
            await inboxStore.CompleteAsync(context.MessageId, cancellationToken);
            return new CapConsumerResult(CapConsumerStatus.Succeeded);
        }
        catch (Exception exception)
        {
            await inboxStore.FailAsync(context.MessageId, exception, cancellationToken);
            throw;
        }
    }
}
