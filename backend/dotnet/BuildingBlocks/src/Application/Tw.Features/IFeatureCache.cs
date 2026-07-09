namespace Tw.Features;

/// <summary>
/// Feature 值缓存边界
/// </summary>
public interface IFeatureCache
{
    /// <summary>
    /// 获取 Feature 缓存值
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>Feature 缓存值；未命中时返回 null</returns>
    Task<FeatureValue?> GetAsync(FeatureCacheKey key, CancellationToken cancellationToken);

    /// <summary>
    /// 写入 Feature 缓存值
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <param name="value">Feature 值</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>异步执行任务</returns>
    Task SetAsync(FeatureCacheKey key, FeatureValue value, CancellationToken cancellationToken);

    /// <summary>
    /// 移除 Feature 缓存值
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>异步执行任务</returns>
    Task RemoveAsync(FeatureCacheKey key, CancellationToken cancellationToken);
}
