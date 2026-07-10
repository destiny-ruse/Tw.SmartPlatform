namespace Tw.EventBus.Cap.Inbox;

/// <summary>
/// 定义Inbox消息存储的能力边界
/// </summary>
public interface IInboxMessageStore
{
    /// <summary>
    /// 尝试开始幂等请求处理并返回占用状态
    /// </summary>
    /// <param name="message">对外返回的安全错误消息</param>
    /// <param name="cancellationToken">用于传播调用方取消请求的令牌</param>
    /// <returns>异步流程完成后产生的bool</returns>
    Task<bool> TryBeginAsync(InboxMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// 将幂等请求标记为完成并保存结果
    /// </summary>
    /// <param name="messageId">用于提供消息标识</param>
    /// <param name="cancellationToken">用于传播调用方取消请求的令牌</param>
    /// <returns>表示异步流程完成状态的任务</returns>
    Task CompleteAsync(string messageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 将幂等请求标记为失败
    /// </summary>
    /// <param name="messageId">用于提供消息标识</param>
    /// <param name="exception">用于模拟异常流程的异常实例</param>
    /// <param name="cancellationToken">用于传播调用方取消请求的令牌</param>
    /// <returns>表示异步流程完成状态的任务</returns>
    Task FailAsync(string messageId, Exception exception, CancellationToken cancellationToken = default);
}
