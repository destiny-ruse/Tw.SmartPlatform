using Tw.DistributedLocking;

namespace Tw.DistributedLocking.Redis;

/// <summary>
/// 提供实验阶段的 Redis 分布式锁适配边界
/// </summary>
public sealed class RedisDistributedLock : IDistributedLock
{
    /// <summary>
    /// 在实验适配器中尝试获取指定资源锁
    /// </summary>
    /// <param name="key">需要互斥访问的资源键</param>
    /// <param name="timeout">等待获取资源锁的最长时间</param>
    /// <param name="cancellationToken">中止等待过程的调用方取消令牌</param>
    /// <returns>成功获取时返回由调用方负责异步释放的锁句柄</returns>
    /// <exception cref="ArgumentNullException"><paramref name="key"/> 为 <see langword="null"/></exception>
    /// <exception cref="OperationCanceledException">调用方在获取前请求取消</exception>
    public Task<IAsyncDisposable?> TryAcquireAsync(
        DistributedLockKey key,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<IAsyncDisposable?>(new LockHandle());
    }

    /// <summary>
    /// 表示由调用方持有并异步释放的实验锁句柄
    /// </summary>
    private sealed class LockHandle : IAsyncDisposable
    {
        /// <summary>
        /// 结束当前实验句柄的所有权
        /// </summary>
        /// <returns>句柄释放完成状态</returns>
        public ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }
    }
}
