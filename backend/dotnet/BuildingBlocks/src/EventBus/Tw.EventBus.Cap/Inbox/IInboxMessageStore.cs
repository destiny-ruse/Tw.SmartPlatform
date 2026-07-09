namespace Tw.EventBus.Cap.Inbox;

/// <summary>定义 IInboxMessageStore 契约</summary>
public interface IInboxMessageStore
{
    /// <summary>执行 TryBeginAsync 操作</summary>
    /// <param name="message">message 参数</param>
    /// <param name="cancellationToken">cancellationToken 参数</param>
    /// <returns>TryBeginAsync 的执行结果</returns>
    Task<bool> TryBeginAsync(InboxMessage message, CancellationToken cancellationToken = default);

    /// <summary>执行 CompleteAsync 操作</summary>
    /// <param name="messageId">messageId 参数</param>
    /// <param name="cancellationToken">cancellationToken 参数</param>
    /// <returns>CompleteAsync 的执行结果</returns>
    Task CompleteAsync(string messageId, CancellationToken cancellationToken = default);

    /// <summary>执行 FailAsync 操作</summary>
    /// <param name="messageId">messageId 参数</param>
    /// <param name="exception">exception 参数</param>
    /// <param name="cancellationToken">cancellationToken 参数</param>
    /// <returns>FailAsync 的执行结果</returns>
    Task FailAsync(string messageId, Exception exception, CancellationToken cancellationToken = default);
}
