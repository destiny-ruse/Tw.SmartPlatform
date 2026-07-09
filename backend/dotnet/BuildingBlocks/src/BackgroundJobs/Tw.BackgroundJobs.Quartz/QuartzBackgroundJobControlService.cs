using Tw.BackgroundJobs.Abstractions;

namespace Tw.BackgroundJobs.Quartz;

/// <summary>表示 QuartzBackgroundJobControlService 类型</summary>
public sealed class QuartzBackgroundJobControlService(IBackgroundJobStateStore stateStore) : IBackgroundJobControlService
{
    /// <summary>执行 ExecuteAsync 操作</summary>
    /// <param name="command">command 参数</param>
    /// <param name="cancellationToken">cancellationToken 参数</param>
    /// <returns>ExecuteAsync 的执行结果</returns>
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
