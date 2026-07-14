namespace Tw.Authorization;

/// <summary>
/// 缓存权限 grant 的允许或拒绝判断，避免重复访问授权存储
/// </summary>
public interface IPermissionGrantCache
{
    /// <summary>
    /// 读取指定权限 grant 缓存键对应的授权判断
    /// </summary>
    /// <param name="key">由主体、租户、权限与资源范围组成的缓存键</param>
    /// <param name="cancellationToken">用于终止缓存读取的取消令牌</param>
    /// <returns>命中时为允许或拒绝状态；未命中时为 null</returns>
    /// <exception cref="OperationCanceledException">读取因调用方取消而终止时抛出</exception>
    Task<bool?> GetAsync(PermissionGrantCacheKey key, CancellationToken cancellationToken);

    /// <summary>
    /// 写入指定权限 grant 缓存键对应的授权判断
    /// </summary>
    /// <param name="key">由主体、租户、权限与资源范围组成的缓存键</param>
    /// <param name="allowed">允许访问时为 true；拒绝访问时为 false</param>
    /// <param name="cancellationToken">用于终止缓存写入的取消令牌</param>
    /// <returns>缓存写入完成后的异步任务</returns>
    /// <exception cref="OperationCanceledException">写入因调用方取消而终止时抛出</exception>
    Task SetAsync(PermissionGrantCacheKey key, bool allowed, CancellationToken cancellationToken);
}
