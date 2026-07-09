namespace Tw.Authorization.Abstractions;

/// <summary>
/// 权限 grant 缓存边界
/// </summary>
public interface IPermissionGrantCache
{
    /// <summary>
    /// 读取权限 grant 缓存结果
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>缓存命中时返回授权结果；未命中时返回 null</returns>
    Task<bool?> GetAsync(PermissionGrantCacheKey key, CancellationToken cancellationToken);

    /// <summary>
    /// 写入权限 grant 缓存结果
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <param name="allowed">是否允许访问</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>异步执行任务</returns>
    Task SetAsync(PermissionGrantCacheKey key, bool allowed, CancellationToken cancellationToken);
}
