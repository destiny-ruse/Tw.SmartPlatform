using Tw.EventBus.Cap.Inbox;

namespace Tw.EventBus.Cap.Consumers;

/// <summary>
/// 封装CapConsumerExecution过滤器相关的数据和行为
/// </summary>
public sealed class CapConsumerExecutionFilter(IInboxMessageStore inboxStore)
{
    /// <summary>
    /// 异步执行当前组件的核心处理流程
    /// </summary>
    /// <param name="context">当前调用携带的上下文信息</param>
    /// <param name="dispatch">用于提供dispatch</param>
    /// <param name="cancellationToken">用于传播调用方取消请求的令牌</param>
    /// <returns>异步流程完成后产生的CapConsumer结果</returns>
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
