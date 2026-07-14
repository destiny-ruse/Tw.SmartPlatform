namespace Tw.DistributedLocking;

/// <summary>
/// 定义 provider-neutral 分布式锁获取边界
/// </summary>
public interface IDistributedLock
{
    /// <summary>
    /// 在限定等待时间内尝试获取指定资源锁
    /// </summary>
    /// <param name="key">需要互斥访问的资源键</param>
    /// <param name="timeout">等待获取资源锁的最长时间</param>
    /// <param name="cancellationToken">中止等待过程的调用方取消令牌</param>
    /// <returns>成功获取时返回由调用方负责异步释放的锁句柄，超时未获取时返回 <see langword="null"/></returns>
    /// <exception cref="OperationCanceledException">调用方在等待期间请求取消</exception>
    /// <remarks>调用方必须异步释放非空句柄，释放行为用于结束锁所有权</remarks>
    Task<IAsyncDisposable?> TryAcquireAsync(
        DistributedLockKey key,
        TimeSpan timeout,
        CancellationToken cancellationToken = default);
}
