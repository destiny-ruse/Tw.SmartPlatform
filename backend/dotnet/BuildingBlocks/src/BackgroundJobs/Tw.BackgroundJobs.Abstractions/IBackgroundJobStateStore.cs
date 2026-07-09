namespace Tw.BackgroundJobs.Abstractions;

public interface IBackgroundJobStateStore
{
    Task SaveAsync(BackgroundJobDefinition definition, CancellationToken cancellationToken = default);

    Task MarkPausedAsync(string jobName, CancellationToken cancellationToken = default);

    Task MarkRunningAsync(string jobName, CancellationToken cancellationToken = default);

    Task MarkStoppedAsync(string jobName, CancellationToken cancellationToken = default);
}
