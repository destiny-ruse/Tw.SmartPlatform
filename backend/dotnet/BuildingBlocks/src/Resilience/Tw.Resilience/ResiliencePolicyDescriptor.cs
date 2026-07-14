namespace Tw.Resilience;

/// <summary>
/// 描述公司自有且不依赖具体 provider 的韧性策略意图
/// </summary>
/// <param name="OperationName">用于诊断、配置和治理的稳定操作名称</param>
/// <param name="OperationKind">决定自动重试安全性的操作分类</param>
/// <param name="Timeout">单次操作允许的最长时间</param>
/// <param name="RetryCount">失败后允许的最大重试次数</param>
/// <param name="CircuitBreakerEnabled">是否要求具体适配器启用熔断</param>
/// <param name="RateLimiterEnabled">是否要求具体适配器启用速率限制</param>
/// <param name="ConcurrencyLimiterEnabled">是否要求具体适配器启用并发隔离</param>
/// <param name="FallbackEnabled">是否存在已声明用户行为和数据边界的降级策略</param>
public sealed record ResiliencePolicyDescriptor(
    string OperationName,
    OperationKind OperationKind,
    TimeSpan Timeout,
    int RetryCount,
    bool CircuitBreakerEnabled,
    bool RateLimiterEnabled,
    bool ConcurrencyLimiterEnabled,
    bool FallbackEnabled)
{
    /// <summary>
    /// 创建适用于出站 HTTP 适配器的默认策略描述
    /// </summary>
    /// <param name="operationName">用于诊断、配置和治理的稳定操作名称</param>
    /// <param name="operationKind">决定自动重试安全性的操作分类</param>
    /// <param name="timeout">单次 HTTP 操作允许的最长时间</param>
    /// <returns>仅包含公司自有策略语义的描述</returns>
    /// <exception cref="ArgumentException">operationName 为空白时抛出</exception>
    /// <exception cref="ArgumentOutOfRangeException">timeout 不大于零时抛出</exception>
    public static ResiliencePolicyDescriptor ForHttp(
        string operationName,
        OperationKind operationKind,
        TimeSpan timeout)
    {
        if (string.IsNullOrWhiteSpace(operationName))
        {
            throw new ArgumentException("操作名称不能为空", nameof(operationName));
        }

        if (timeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(timeout), timeout, "超时时间必须大于零");
        }

        return new ResiliencePolicyDescriptor(
            operationName,
            operationKind,
            timeout,
            RetryCount: 3,
            CircuitBreakerEnabled: true,
            RateLimiterEnabled: true,
            ConcurrencyLimiterEnabled: true,
            FallbackEnabled: false);
    }
}
