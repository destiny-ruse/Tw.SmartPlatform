namespace Tw.BackgroundJobs.Abstractions;

/// <summary>
/// 定义后台作业Control服务的能力边界
/// </summary>
public interface IBackgroundJobControlService
{
    /// <summary>
    /// 异步执行当前组件的核心处理流程
    /// </summary>
    /// <param name="command">用于提供command</param>
    /// <param name="cancellationToken">用于传播调用方取消请求的令牌</param>
    /// <returns>表示异步流程完成状态的任务</returns>
    Task ExecuteAsync(BackgroundJobControlCommand command, CancellationToken cancellationToken = default);
}
