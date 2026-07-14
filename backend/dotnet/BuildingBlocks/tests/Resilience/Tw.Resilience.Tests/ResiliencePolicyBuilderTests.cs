using AwesomeAssertions;
using Tw.Resilience;
using Xunit;

namespace Tw.Resilience.Tests;

/// <summary>
/// 验证韧性描述的校验与有效重试次数归一化
/// </summary>
public sealed class ResiliencePolicyBuilderTests
{
    /// <summary>
    /// 已验证策略完整保留具体适配器需要的公司自有策略意图
    /// </summary>
    [Fact]
    public void Build_CopiesCompleteValidatedPolicyIntent()
    {
        var descriptor = new ResiliencePolicyDescriptor(
            OperationName: "CreateOrder",
            OperationKind.NonIdempotentWrite,
            Timeout: TimeSpan.FromSeconds(7),
            RetryCount: 42,
            CircuitBreakerEnabled: false,
            RateLimiterEnabled: true,
            ConcurrencyLimiterEnabled: false,
            FallbackEnabled: true);

        var policy = ResiliencePolicyBuilder.Build(descriptor);

        policy.OperationName.Should().Be("CreateOrder");
        policy.OperationKind.Should().Be(OperationKind.NonIdempotentWrite);
        policy.Timeout.Should().Be(TimeSpan.FromSeconds(7));
        policy.RetryCount.Should().Be(0);
        policy.RetryEnabled.Should().BeFalse();
        policy.CircuitBreakerEnabled.Should().BeFalse();
        policy.RateLimiterEnabled.Should().BeTrue();
        policy.ConcurrencyLimiterEnabled.Should().BeFalse();
        policy.FallbackEnabled.Should().BeTrue();
    }

    /// <summary>
    /// 非幂等写操作始终把有效重试次数归零
    /// </summary>
    [Fact]
    public void Build_DisablesRetryForNonIdempotentWrite()
    {
        var policy = ResiliencePolicyBuilder.Build(
            CreateDescriptor(OperationKind.NonIdempotentWrite, retryCount: 3));

        policy.RetryEnabled.Should().BeFalse();
        policy.RetryCount.Should().Be(0);
        policy.Timeout.Should().Be(TimeSpan.FromSeconds(3));
    }

    /// <summary>
    /// 读取操作保留已经验证的重试次数
    /// </summary>
    [Fact]
    public void Build_PreservesRetryCountForReadOperation()
    {
        var policy = ResiliencePolicyBuilder.Build(CreateDescriptor(OperationKind.Read, retryCount: 2));

        policy.RetryEnabled.Should().BeTrue();
        policy.RetryCount.Should().Be(2);
    }

    /// <summary>
    /// 幂等写操作保留已经验证的重试次数
    /// </summary>
    [Fact]
    public void Build_PreservesRetryCountForIdempotentWrite()
    {
        var policy = ResiliencePolicyBuilder.Build(
            CreateDescriptor(OperationKind.IdempotentWrite, retryCount: 2));

        policy.RetryEnabled.Should().BeTrue();
        policy.RetryCount.Should().Be(2);
    }

    /// <summary>
    /// 有效重试次数为零时禁用自动重试
    /// </summary>
    [Fact]
    public void Build_DisablesRetryWhenRetryCountIsZero()
    {
        var policy = ResiliencePolicyBuilder.Build(
            CreateDescriptor(OperationKind.IdempotentWrite, retryCount: 0));

        policy.RetryEnabled.Should().BeFalse();
        policy.RetryCount.Should().Be(0);
    }

    /// <summary>
    /// 空白操作名称无法构建可诊断的韧性策略
    /// </summary>
    [Fact]
    public void Build_RejectsBlankOperationName()
    {
        var action = () => ResiliencePolicyBuilder.Build(
            CreateDescriptor(OperationKind.Read, operationName: " "));

        action.Should().Throw<ArgumentException>()
            .WithParameterName("descriptor")
            .WithMessage("*操作名称不能为空*");
    }

    /// <summary>
    /// 非正超时时间无法构建韧性策略
    /// </summary>
    [Fact]
    public void Build_RejectsNonPositiveTimeout()
    {
        var action = () => ResiliencePolicyBuilder.Build(
            CreateDescriptor(OperationKind.Read, timeout: TimeSpan.Zero));

        action.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("descriptor")
            .WithMessage("*超时时间必须大于零*");
    }

    /// <summary>
    /// 负数重试次数无法构建韧性策略
    /// </summary>
    [Fact]
    public void Build_RejectsNegativeRetryCount()
    {
        var action = () => ResiliencePolicyBuilder.Build(
            CreateDescriptor(OperationKind.Read, retryCount: -1));

        action.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("descriptor")
            .WithMessage("*重试次数不能小于零*");
    }

    /// <summary>
    /// 未知操作分类无法绕过幂等重试边界
    /// </summary>
    [Fact]
    public void Build_RejectsUnknownOperationKind()
    {
        var action = () => ResiliencePolicyBuilder.Build(CreateDescriptor((OperationKind)999));

        action.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("descriptor")
            .WithMessage("*操作分类不受支持*");
    }

    /// <summary>
    /// provider-neutral 构建器不替具体适配器定义重试次数上限
    /// </summary>
    [Fact]
    public void Build_AcceptsProviderSpecificRetryUpperBound()
    {
        var policy = ResiliencePolicyBuilder.Build(
            CreateDescriptor(OperationKind.Read, retryCount: int.MaxValue));

        policy.RetryCount.Should().Be(int.MaxValue);
        policy.RetryEnabled.Should().BeTrue();
    }

    /// <summary>
    /// 创建覆盖构建器输入边界的公司自有策略描述
    /// </summary>
    /// <param name="operationKind">决定自动重试安全性的操作分类</param>
    /// <param name="retryCount">策略允许的最大重试次数</param>
    /// <param name="operationName">用于诊断和治理的操作名称</param>
    /// <param name="timeout">单次操作允许的最长时间</param>
    /// <returns>不含第三方 provider 类型的策略描述</returns>
    private static ResiliencePolicyDescriptor CreateDescriptor(
        OperationKind operationKind,
        int retryCount = 3,
        string operationName = "GetOrder",
        TimeSpan? timeout = null)
    {
        return new ResiliencePolicyDescriptor(
            operationName,
            operationKind,
            timeout ?? TimeSpan.FromSeconds(3),
            retryCount,
            CircuitBreakerEnabled: true,
            RateLimiterEnabled: true,
            ConcurrencyLimiterEnabled: true,
            FallbackEnabled: false);
    }
}
