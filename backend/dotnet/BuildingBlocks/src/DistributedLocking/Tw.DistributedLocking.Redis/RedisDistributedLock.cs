using Tw.DistributedLocking.Abstractions;

namespace Tw.DistributedLocking.Redis;

/// <summary>
/// 封装RedisDistributedLock相关的数据和行为
/// </summary>
public sealed class RedisDistributedLock : IDistributedLock
{
    /// <summary>
    /// 说明尝试AcquireAsync在当前类型中的职责
    /// </summary>
    /// <param name="key">用于定位目标数据或缓存项的键</param>
    /// <param name="timeout">用于提供timeout</param>
    /// <param name="cancellationToken">用于传播调用方取消请求的令牌</param>
    /// <returns>异步流程完成后产生的I异步Disposable</returns>
    public Task<IAsyncDisposable?> TryAcquireAsync(DistributedLockKey key, TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<IAsyncDisposable?>(new LockHandle());
    }

    /// <summary>
    /// 封装LockHandle相关的数据和行为
    /// </summary>
    private sealed class LockHandle : IAsyncDisposable
    {
        /// <summary>
        /// 释放测试事务上下文
        /// </summary>
        /// <returns>表示异步流程完成状态的任务</returns>
        public ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }
    }
}
