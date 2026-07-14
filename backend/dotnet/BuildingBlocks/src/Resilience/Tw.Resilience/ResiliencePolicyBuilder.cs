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

        return new ResiliencePolicy(
            descriptor.OperationName,
            descriptor.OperationKind,
            descriptor.Timeout,
            effectiveRetryCount,
            descriptor.CircuitBreakerEnabled,
            descriptor.RateLimiterEnabled,
            descriptor.ConcurrencyLimiterEnabled,
            descriptor.FallbackEnabled);
    }
}

/// <summary>
/// 表示供具体适配器消费且完整保留已验证意图的 provider-neutral 策略结果
/// </summary>
/// <remarks>
/// 具体适配器只能消费该结果，不得直接把未经验证的 <see cref="ResiliencePolicyDescriptor"/> 映射为 provider 配置
/// </remarks>
public sealed record ResiliencePolicy
{
    /// <summary>
    /// 仅允许构建器在完成输入验证与幂等归一化后创建策略结果
    /// </summary>
    /// <param name="operationName">用于诊断、配置和治理的稳定操作名称</param>
    /// <param name="operationKind">决定自动重试安全性的已验证操作分类</param>
    /// <param name="timeout">单次操作允许的最长时间</param>
    /// <param name="retryCount">完成幂等归一化后的有效重试次数</param>
    /// <param name="circuitBreakerEnabled">是否要求具体适配器启用熔断</param>
    /// <param name="rateLimiterEnabled">是否要求具体适配器启用速率限制</param>
    /// <param name="concurrencyLimiterEnabled">是否要求具体适配器启用并发隔离</param>
    /// <param name="fallbackEnabled">是否存在已声明用户行为和数据边界的降级策略</param>
    internal ResiliencePolicy(
        string operationName,
        OperationKind operationKind,
        TimeSpan timeout,
        int retryCount,
        bool circuitBreakerEnabled,
        bool rateLimiterEnabled,
        bool concurrencyLimiterEnabled,
        bool fallbackEnabled)
    {
        OperationName = operationName;
        OperationKind = operationKind;
        Timeout = timeout;
        RetryCount = retryCount;
        CircuitBreakerEnabled = circuitBreakerEnabled;
        RateLimiterEnabled = rateLimiterEnabled;
        ConcurrencyLimiterEnabled = concurrencyLimiterEnabled;
        FallbackEnabled = fallbackEnabled;
    }

    /// <summary>
    /// 用于诊断、配置和治理的稳定操作名称
    /// </summary>
    public string OperationName { get; }

    /// <summary>
    /// 决定自动重试安全性的已验证操作分类
    /// </summary>
    public OperationKind OperationKind { get; }

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

    /// <summary>
    /// 是否要求具体适配器启用熔断
    /// </summary>
    public bool CircuitBreakerEnabled { get; }

    /// <summary>
    /// 是否要求具体适配器启用速率限制
    /// </summary>
    public bool RateLimiterEnabled { get; }

    /// <summary>
    /// 是否要求具体适配器启用并发隔离
    /// </summary>
    public bool ConcurrencyLimiterEnabled { get; }

    /// <summary>
    /// 是否存在已声明用户行为和数据边界的降级策略
    /// </summary>
    public bool FallbackEnabled { get; }
}
