using System.Collections.Concurrent;

namespace Tw.EventBus.Cap.Inbox;

/// <summary>表示 SqlSugarInboxMessageStore 类型</summary>
public sealed class SqlSugarInboxMessageStore : IInboxMessageStore
{
    /// <summary>表示 _messages 字段</summary>
    private readonly ConcurrentDictionary<string, InboxMessage> _messages = new(StringComparer.Ordinal);

    /// <summary>执行 TryBeginAsync 操作</summary>
    /// <param name="message">message 参数</param>
    /// <param name="cancellationToken">cancellationToken 参数</param>
    /// <returns>TryBeginAsync 的执行结果</returns>
    public Task<bool> TryBeginAsync(InboxMessage message, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_messages.TryAdd(message.MessageId, message));
    }

    /// <summary>执行 CompleteAsync 操作</summary>
    /// <param name="messageId">messageId 参数</param>
    /// <param name="cancellationToken">cancellationToken 参数</param>
    /// <returns>CompleteAsync 的执行结果</returns>
    public Task CompleteAsync(string messageId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }

    /// <summary>执行 FailAsync 操作</summary>
    /// <param name="messageId">messageId 参数</param>
    /// <param name="exception">exception 参数</param>
    /// <param name="cancellationToken">cancellationToken 参数</param>
    /// <returns>FailAsync 的执行结果</returns>
    public Task FailAsync(string messageId, Exception exception, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }
}
