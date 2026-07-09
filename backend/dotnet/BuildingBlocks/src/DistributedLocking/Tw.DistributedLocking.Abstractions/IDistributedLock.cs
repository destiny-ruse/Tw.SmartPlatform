namespace Tw.DistributedLocking.Abstractions;

/// <summary>定义 IDistributedLock 契约</summary>
public interface IDistributedLock
{
    /// <summary>执行 TryAcquireAsync 操作</summary>
    /// <param name="key">key 参数</param>
    /// <param name="timeout">timeout 参数</param>
    /// <param name="cancellationToken">cancellationToken 参数</param>
    /// <returns>TryAcquireAsync 的执行结果</returns>
    Task<IAsyncDisposable?> TryAcquireAsync(DistributedLockKey key, TimeSpan timeout, CancellationToken cancellationToken = default);
}
