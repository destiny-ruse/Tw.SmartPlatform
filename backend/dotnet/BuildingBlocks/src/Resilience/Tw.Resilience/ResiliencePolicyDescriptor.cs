namespace Tw.Resilience;

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
    public static ResiliencePolicyDescriptor ForHttp(string operationName, OperationKind operationKind, TimeSpan timeout)
    {
        if (timeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(timeout));
        }

        return new ResiliencePolicyDescriptor(operationName, operationKind, timeout, RetryCount: 3, true, true, true, false);
    }
}
