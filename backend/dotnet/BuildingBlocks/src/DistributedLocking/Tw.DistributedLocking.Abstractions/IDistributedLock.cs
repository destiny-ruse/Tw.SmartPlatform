namespace Tw.DistributedLocking.Abstractions;

public interface IDistributedLock
{
    Task<IAsyncDisposable?> TryAcquireAsync(DistributedLockKey key, TimeSpan timeout, CancellationToken cancellationToken = default);
}
