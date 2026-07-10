using Tw.BackgroundJobs.Abstractions;

namespace Tw.BackgroundJobs.Quartz;

/// <summary>
/// 封装Quartz后台作业Control服务相关的数据和行为
/// </summary>
public sealed class QuartzBackgroundJobControlService(IBackgroundJobStateStore stateStore) : IBackgroundJobControlService
{
    /// <summary>
    /// 异步执行当前组件的核心处理流程
    /// </summary>
    /// <param name="command">用于提供command</param>
    /// <param name="cancellationToken">用于传播调用方取消请求的令牌</param>
    /// <returns>表示异步流程完成状态的任务</returns>
    public Task ExecuteAsync(BackgroundJobControlCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        return command.Action switch
        {
            BackgroundJobControlAction.Create when command.Definition is not null => stateStore.SaveAsync(command.Definition, cancellationToken),
            BackgroundJobControlAction.Pause => stateStore.MarkPausedAsync(command.JobName, cancellationToken),
            BackgroundJobControlAction.Resume or BackgroundJobControlAction.Trigger => stateStore.MarkRunningAsync(command.JobName, cancellationToken),
            BackgroundJobControlAction.Stop => stateStore.MarkStoppedAsync(command.JobName, cancellationToken),
            _ => Task.CompletedTask
        };
    }
}
