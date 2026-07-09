namespace Tw.EventBus.Cap.Cleanup;

public sealed class CapMessageCleanupJob : ICapMessageCleanupJob
{
    public Task ExecuteAsync(CapMessageCleanupOptions options, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        cancellationToken.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }
}
