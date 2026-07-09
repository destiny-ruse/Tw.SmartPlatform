namespace Tw.BackgroundJobs.Abstractions;

public interface IBackgroundJob<TArgs>
{
    Task ExecuteAsync(TArgs args, BackgroundJobContext context, CancellationToken cancellationToken = default);
}
