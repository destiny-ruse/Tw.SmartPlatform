namespace Tw.BackgroundJobs.Abstractions;

/// <summary>
/// 定义后台作业状态存储的能力边界
/// </summary>
public interface IBackgroundJobStateStore
{
    /// <summary>
    /// 说明SaveAsync在当前类型中的职责
    /// </summary>
    /// <param name="definition">需要保存的后台作业定义</param>
    /// <param name="cancellationToken">用于传播调用方取消请求的令牌</param>
    /// <returns>表示异步流程完成状态的任务</returns>
    Task SaveAsync(BackgroundJobDefinition definition, CancellationToken cancellationToken = default);

    /// <summary>
    /// 将后台作业标记为暂停状态
    /// </summary>
    /// <param name="jobName">需要变更状态的后台作业名称</param>
    /// <param name="cancellationToken">用于传播调用方取消请求的令牌</param>
    /// <returns>表示异步流程完成状态的任务</returns>
    Task MarkPausedAsync(string jobName, CancellationToken cancellationToken = default);

    /// <summary>
    /// 将后台作业标记为运行状态
    /// </summary>
    /// <param name="jobName">需要变更状态的后台作业名称</param>
    /// <param name="cancellationToken">用于传播调用方取消请求的令牌</param>
    /// <returns>表示异步流程完成状态的任务</returns>
    Task MarkRunningAsync(string jobName, CancellationToken cancellationToken = default);

    /// <summary>
    /// 将后台作业标记为停止状态
    /// </summary>
    /// <param name="jobName">需要变更状态的后台作业名称</param>
    /// <param name="cancellationToken">用于传播调用方取消请求的令牌</param>
    /// <returns>表示异步流程完成状态的任务</returns>
    Task MarkStoppedAsync(string jobName, CancellationToken cancellationToken = default);
}
