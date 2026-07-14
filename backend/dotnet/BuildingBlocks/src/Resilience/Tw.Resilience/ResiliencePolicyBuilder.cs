namespace Tw.Resilience;

/// <summary>
/// 校验公司自有韧性描述并计算 provider-neutral 策略结果
/// </summary>
public static class ResiliencePolicyBuilder
{
    /// <summary>
    /// 校验描述并根据幂等分类决定是否允许自动重试
    /// </summary>
    /// <param name="descriptor">需要校验和计算的公司自有策略描述</param>
    /// <returns>不包含具体 provider 类型的策略结果</returns>
    /// <exception cref="ArgumentNullException">descriptor 为 null 时抛出</exception>
    /// <exception cref="ArgumentException">操作名称为空白时抛出</exception>
    /// <exception cref="ArgumentOutOfRangeException">超时或重试次数超出有效范围时抛出</exception>
    public static ResiliencePolicy Build(ResiliencePolicyDescriptor descriptor)
    {
        if (descriptor is null)
        {
            throw new ArgumentNullException(nameof(descriptor), "韧性策略描述不能为空");
        }

        if (string.IsNullOrWhiteSpace(descriptor.OperationName))
        {
            throw new ArgumentException("操作名称不能为空", nameof(descriptor));
        }

        if (!Enum.IsDefined(descriptor.OperationKind))
        {
            throw new ArgumentOutOfRangeException(
                nameof(descriptor),
                descriptor.OperationKind,
                "操作分类不受支持");
        }

        if (descriptor.Timeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(descriptor),
                descriptor.Timeout,
                "超时时间必须大于零");
        }

        if (descriptor.RetryCount < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(descriptor),
                descriptor.RetryCount,
                "重试次数不能小于零");
        }

        return new ResiliencePolicy(
            RetryEnabled: descriptor.OperationKind != OperationKind.NonIdempotentWrite
                && descriptor.RetryCount > 0,
            descriptor.Timeout);
    }
}

/// <summary>
/// 表示通过验证且可由具体适配器消费的韧性策略结果
/// </summary>
/// <param name="RetryEnabled">当前操作满足幂等前提且配置了自动重试时为 <see langword="true"/></param>
/// <param name="Timeout">单次操作允许的最长时间</param>
public sealed record ResiliencePolicy(bool RetryEnabled, TimeSpan Timeout);
