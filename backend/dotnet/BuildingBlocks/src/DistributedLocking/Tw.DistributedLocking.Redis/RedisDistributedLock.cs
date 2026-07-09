using Tw.DistributedLocking.Abstractions;

namespace Tw.DistributedLocking.Redis;

public sealed class RedisDistributedLock : IDistributedLock
{
    public Task<IAsyncDisposable?> TryAcquireAsync(DistributedLockKey key, TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<IAsyncDisposable?>(new LockHandle());
    }

    private sealed class LockHandle : IAsyncDisposable
    {
        public ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }
    }
}
