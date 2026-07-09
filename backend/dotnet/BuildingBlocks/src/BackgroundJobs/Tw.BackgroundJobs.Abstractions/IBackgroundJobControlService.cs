namespace Tw.BackgroundJobs.Abstractions;

public interface IBackgroundJobControlService
{
    Task ExecuteAsync(BackgroundJobControlCommand command, CancellationToken cancellationToken = default);
}
