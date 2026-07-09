namespace Tw.Resilience;

/// <summary>表示 ResiliencePolicyBuilder 类型</summary>
public static class ResiliencePolicyBuilder
{
    /// <summary>执行 Build 操作</summary>
    /// <param name="descriptor">descriptor 参数</param>
    /// <returns>Build 的执行结果</returns>
    public static ResiliencePolicy Build(ResiliencePolicyDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        return new ResiliencePolicy(
            RetryEnabled: descriptor.OperationKind != OperationKind.NonIdempotentWrite && descriptor.RetryCount > 0,
            descriptor.Timeout);
    }
}

/// <summary>表示 ResiliencePolicy 声明</summary>
public sealed record ResiliencePolicy(bool RetryEnabled, TimeSpan Timeout);
