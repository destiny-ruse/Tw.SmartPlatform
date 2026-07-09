namespace Tw.Settings;

/// <summary>
/// Setting 读取服务
/// </summary>
public interface ISettingProvider
{
    /// <summary>
    /// 按 user、tenant、service、definition default 顺序读取 Setting 值
    /// </summary>
    /// <param name="name">Setting 名称</param>
    /// <param name="tenantId">租户标识</param>
    /// <param name="serviceName">服务名称</param>
    /// <param name="userId">用户标识</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>Setting 值，不存在且没有定义默认值时返回 null</returns>
    /// <exception cref="ArgumentException"><paramref name="name"/>、<paramref name="tenantId"/> 或 <paramref name="serviceName"/> 为空白时抛出</exception>
    Task<string?> GetAsync(
        string name,
        string tenantId,
        string serviceName,
        string? userId,
        CancellationToken cancellationToken);

    /// <summary>
    /// 按刷新请求移除精确匹配的缓存值
    /// </summary>
    /// <param name="request">刷新请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>异步执行任务</returns>
    /// <exception cref="ArgumentNullException"><paramref name="request"/> 为 null 时抛出</exception>
    Task RefreshAsync(SettingRefreshRequest request, CancellationToken cancellationToken);
}
