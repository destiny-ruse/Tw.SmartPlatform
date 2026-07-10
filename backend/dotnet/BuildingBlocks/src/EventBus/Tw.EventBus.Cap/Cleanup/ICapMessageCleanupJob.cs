namespace Tw.EventBus.Cap.Cleanup;

/// <summary>
/// 定义Cap消息Cleanup作业的能力边界
/// </summary>
public interface ICapMessageCleanupJob
{
    /// <summary>
    /// 异步执行当前组件的核心处理流程
    /// </summary>
    /// <param name="options">用于配置当前组件行为的选项</param>
    /// <param name="cancellationToken">用于传播调用方取消请求的令牌</param>
    /// <returns>表示异步流程完成状态的任务</returns>
    Task ExecuteAsync(CapMessageCleanupOptions options, CancellationToken cancellationToken = default);
}
