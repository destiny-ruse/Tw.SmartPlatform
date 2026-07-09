using Tw.DistributedLocking.Abstractions;

namespace Tw.DistributedLocking.Redis;

/// <summary>表示 RedisDistributedLock 类型</summary>
public sealed class RedisDistributedLock : IDistributedLock
{
    /// <summary>执行 TryAcquireAsync 操作</summary>
    /// <param name="key">key 参数</param>
    /// <param name="timeout">timeout 参数</param>
    /// <param name="cancellationToken">cancellationToken 参数</param>
    /// <returns>TryAcquireAsync 的执行结果</returns>
    public Task<IAsyncDisposable?> TryAcquireAsync(DistributedLockKey key, TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<IAsyncDisposable?>(new LockHandle());
    }

    /// <summary>表示 LockHandle 类型</summary>
    private sealed class LockHandle : IAsyncDisposable
    {
        /// <summary>执行 DisposeAsync 操作</summary>
        /// <returns>DisposeAsync 的执行结果</returns>
        public ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }
    }
}
