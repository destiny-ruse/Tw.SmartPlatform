namespace Tw.Resilience;

/// <summary>
/// 封装Resilience策略Descriptor相关的数据和行为
/// </summary>
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
    /// 说明ForHttp在当前类型中的职责
    /// </summary>
    /// <param name="operationName">用于提供操作Name</param>
    /// <param name="operationKind">用于提供操作Kind</param>
    /// <param name="timeout">用于提供timeout</param>
    /// <returns>方法计算得到的文本值</returns>
    public static ResiliencePolicyDescriptor ForHttp(string operationName, OperationKind operationKind, TimeSpan timeout)
    {
        if (timeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(timeout));
        }

        return new ResiliencePolicyDescriptor(operationName, operationKind, timeout, RetryCount: 3, true, true, true, false);
    }
}
