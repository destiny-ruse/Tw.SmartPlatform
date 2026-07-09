using Tw.BackgroundJobs.Abstractions;

namespace Tw.BackgroundJobs.Quartz;

public sealed class QuartzBackgroundJobControlService(IBackgroundJobStateStore stateStore) : IBackgroundJobControlService
{
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
