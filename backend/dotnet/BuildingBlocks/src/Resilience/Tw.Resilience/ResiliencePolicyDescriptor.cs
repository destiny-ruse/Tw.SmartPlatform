namespace Tw.Resilience;

/// <summary>表示 ResiliencePolicyDescriptor 声明</summary>
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
    /// <summary>执行 ForHttp 操作</summary>
    /// <param name="operationName">operationName 参数</param>
    /// <param name="operationKind">operationKind 参数</param>
    /// <param name="timeout">timeout 参数</param>
    /// <returns>ForHttp 的执行结果</returns>
    public static ResiliencePolicyDescriptor ForHttp(string operationName, OperationKind operationKind, TimeSpan timeout)
    {
        if (timeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(timeout));
        }

        return new ResiliencePolicyDescriptor(operationName, operationKind, timeout, RetryCount: 3, true, true, true, false);
    }
}
