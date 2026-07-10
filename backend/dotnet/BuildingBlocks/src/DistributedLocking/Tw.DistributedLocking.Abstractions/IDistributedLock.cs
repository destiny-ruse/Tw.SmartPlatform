namespace Tw.DistributedLocking.Abstractions;

/// <summary>
/// 定义DistributedLock的能力边界
/// </summary>
public interface IDistributedLock
{
    /// <summary>
    /// 说明尝试AcquireAsync在当前类型中的职责
    /// </summary>
    /// <param name="key">用于定位目标数据或缓存项的键</param>
    /// <param name="timeout">用于提供timeout</param>
    /// <param name="cancellationToken">用于传播调用方取消请求的令牌</param>
    /// <returns>异步流程完成后产生的I异步Disposable</returns>
    Task<IAsyncDisposable?> TryAcquireAsync(DistributedLockKey key, TimeSpan timeout, CancellationToken cancellationToken = default);
}
