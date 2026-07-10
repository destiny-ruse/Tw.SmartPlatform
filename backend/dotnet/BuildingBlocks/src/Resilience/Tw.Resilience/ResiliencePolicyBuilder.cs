namespace Tw.Resilience;

/// <summary>
/// 封装Resilience策略构建器相关的数据和行为
/// </summary>
public static class ResiliencePolicyBuilder
{
    /// <summary>
    /// 说明Build在当前类型中的职责
    /// </summary>
    /// <param name="descriptor">用于提供描述符</param>
    /// <returns>方法完成后返回给调用方的结果对象</returns>
    public static ResiliencePolicy Build(ResiliencePolicyDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        return new ResiliencePolicy(
            RetryEnabled: descriptor.OperationKind != OperationKind.NonIdempotentWrite && descriptor.RetryCount > 0,
            descriptor.Timeout);
    }
}

/// <summary>
/// 封装Resilience策略相关的数据和行为
/// </summary>
public sealed record ResiliencePolicy(bool RetryEnabled, TimeSpan Timeout);
