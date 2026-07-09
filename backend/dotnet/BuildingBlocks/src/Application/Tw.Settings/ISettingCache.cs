namespace Tw.Settings;

/// <summary>
/// Setting 值缓存边界
/// </summary>
public interface ISettingCache
{
    /// <summary>
    /// 读取缓存中的 Setting 值
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>缓存值，不存在时返回 null</returns>
    Task<SettingValue?> GetAsync(SettingCacheKey key, CancellationToken cancellationToken);

    /// <summary>
    /// 写入缓存中的 Setting 值
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <param name="value">Setting 值</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>异步执行任务</returns>
    Task SetAsync(SettingCacheKey key, SettingValue value, CancellationToken cancellationToken);

    /// <summary>
    /// 移除缓存中的 Setting 值
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>异步执行任务</returns>
    Task RemoveAsync(SettingCacheKey key, CancellationToken cancellationToken);
}
