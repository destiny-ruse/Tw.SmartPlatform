namespace Tw.Features;

/// <summary>
/// Feature 检查器
/// </summary>
public interface IFeatureChecker
{
    /// <summary>
    /// 检查指定租户和服务下的 Feature 是否启用
    /// </summary>
    /// <param name="feature">Feature 名称</param>
    /// <param name="tenantId">租户标识</param>
    /// <param name="serviceName">服务名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>Feature 检查结果</returns>
    Task<FeatureCheckResult> CheckAsync(
        string feature,
        string tenantId,
        string serviceName,
        CancellationToken cancellationToken);

    /// <summary>
    /// 按刷新请求移除精确匹配的缓存值
    /// </summary>
    /// <param name="request">刷新请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>异步执行任务</returns>
    Task RefreshAsync(FeatureRefreshRequest request, CancellationToken cancellationToken);
}
