namespace Tw.EventBus.Cap.Cleanup;

public interface ICapMessageCleanupJob
{
    Task ExecuteAsync(CapMessageCleanupOptions options, CancellationToken cancellationToken = default);
}
