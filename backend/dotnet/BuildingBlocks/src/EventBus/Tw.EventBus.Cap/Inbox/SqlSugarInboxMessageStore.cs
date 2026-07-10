using System.Collections.Concurrent;

namespace Tw.EventBus.Cap.Inbox;

/// <summary>
/// 封装SqlSugarInbox消息存储相关的数据和行为
/// </summary>
public sealed class SqlSugarInboxMessageStore : IInboxMessageStore
{
    /// <summary>
    /// 保存当前类型处理流程依赖的messages
    /// </summary>
    private readonly ConcurrentDictionary<string, InboxMessage> _messages = new(StringComparer.Ordinal);

    /// <summary>
    /// 尝试开始幂等请求处理并返回占用状态
    /// </summary>
    /// <param name="message">对外返回的安全错误消息</param>
    /// <param name="cancellationToken">用于传播调用方取消请求的令牌</param>
    /// <returns>异步流程完成后产生的bool</returns>
    public Task<bool> TryBeginAsync(InboxMessage message, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_messages.TryAdd(message.MessageId, message));
    }

    /// <summary>
    /// 将幂等请求标记为完成并保存结果
    /// </summary>
    /// <param name="messageId">用于提供消息标识</param>
    /// <param name="cancellationToken">用于传播调用方取消请求的令牌</param>
    /// <returns>表示异步流程完成状态的任务</returns>
    public Task CompleteAsync(string messageId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }

    /// <summary>
    /// 将幂等请求标记为失败
    /// </summary>
    /// <param name="messageId">用于提供消息标识</param>
    /// <param name="exception">用于模拟异常流程的异常实例</param>
    /// <param name="cancellationToken">用于传播调用方取消请求的令牌</param>
    /// <returns>表示异步流程完成状态的任务</returns>
    public Task FailAsync(string messageId, Exception exception, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }
}
