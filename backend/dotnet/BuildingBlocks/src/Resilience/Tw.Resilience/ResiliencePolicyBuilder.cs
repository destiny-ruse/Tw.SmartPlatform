namespace Tw.Resilience;

/// <summary>
/// 校验公司自有韧性描述并计算 provider-neutral 策略结果
/// </summary>
public static class ResiliencePolicyBuilder
{
    /// <summary>
    /// 校验描述并根据幂等分类归一化有效重试次数
    /// </summary>
    /// <param name="descriptor">需要校验和计算的公司自有策略描述</param>
    /// <returns>供具体适配器消费且不包含 provider 类型的已验证策略结果</returns>
    /// <exception cref="ArgumentNullException">descriptor 为 null 时抛出</exception>
    /// <exception cref="ArgumentException">操作名称为空白时抛出</exception>
    /// <exception cref="ArgumentOutOfRangeException">操作分类未定义、超时不大于零或重试次数小于零时抛出</exception>
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

        var effectiveRetryCount = descriptor.OperationKind == OperationKind.NonIdempotentWrite
            ? 0
            : descriptor.RetryCount;

        return new ResiliencePolicy(descriptor.Timeout, effectiveRetryCount);
    }
}

/// <summary>
/// 表示通过验证且完成重试次数归一化的韧性策略结果
/// </summary>
public sealed record ResiliencePolicy
{
    /// <summary>
    /// 仅允许构建器在完成输入验证与幂等归一化后创建策略结果
    /// </summary>
    /// <param name="timeout">单次操作允许的最长时间</param>
    /// <param name="retryCount">完成幂等归一化后的有效重试次数</param>
    internal ResiliencePolicy(TimeSpan timeout, int retryCount)
    {
        Timeout = timeout;
        RetryCount = retryCount;
    }

    /// <summary>
    /// 有效重试次数大于零时为 <see langword="true"/>
    /// </summary>
    public bool RetryEnabled => RetryCount > 0;

    /// <summary>
    /// 单次操作允许的最长时间
    /// </summary>
    /// <remarks>
    /// 具体适配器必须在消费前额外校验自身支持的上限
    /// </remarks>
    public TimeSpan Timeout { get; }

    /// <summary>
    /// 完成幂等归一化后的有效重试次数
    /// </summary>
    /// <remarks>
    /// 非幂等写操作固定为零，具体适配器必须在消费前额外校验自身支持的上限
    /// </remarks>
    public int RetryCount { get; }
}
