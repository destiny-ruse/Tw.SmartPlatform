using AwesomeAssertions;
using Tw.Resilience;
using Xunit;

namespace Tw.Resilience.Tests;

/// <summary>
/// 验证默认 HTTP 策略描述工厂的输入与幂等边界
/// </summary>
public sealed class ResiliencePolicyDescriptorTests
{
    /// <summary>
    /// 描述工厂拒绝缺少诊断名称的操作
    /// </summary>
    [Fact]
    public void ForHttp_RejectsBlankOperationName()
    {
        var action = () => ResiliencePolicyDescriptor.ForHttp(
            " ",
            OperationKind.Read,
            TimeSpan.FromSeconds(3));

        action.Should().Throw<ArgumentException>()
            .WithParameterName("operationName")
            .WithMessage("*操作名称不能为空*");
    }

    /// <summary>
    /// 描述工厂拒绝未知操作分类
    /// </summary>
    [Fact]
    public void ForHttp_RejectsUnknownOperationKind()
    {
        var action = () => ResiliencePolicyDescriptor.ForHttp(
            "GetOrder",
            (OperationKind)999,
            TimeSpan.FromSeconds(3));

        action.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("operationKind")
            .WithMessage("*操作分类不受支持*");
    }

    /// <summary>
    /// 非幂等写操作的描述不会携带可误用的重试次数
    /// </summary>
    [Fact]
    public void ForHttp_NormalizesNonIdempotentRetryCount()
    {
        var descriptor = ResiliencePolicyDescriptor.ForHttp(
            "CreateOrder",
            OperationKind.NonIdempotentWrite,
            TimeSpan.FromSeconds(3));

        descriptor.RetryCount.Should().Be(0);
        ResiliencePolicyBuilder.Build(descriptor).RetryCount.Should().Be(0);
    }

    /// <summary>
    /// 描述工厂以可诊断消息拒绝非正超时时间
    /// </summary>
    [Fact]
    public void ForHttp_RejectsNonPositiveTimeout()
    {
        var action = () => ResiliencePolicyDescriptor.ForHttp(
            "GetOrder",
            OperationKind.Read,
            TimeSpan.Zero);

        action.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("timeout")
            .WithMessage("*超时时间必须大于零*");
    }

    /// <summary>
    /// provider-neutral 描述接受所有正数超时边界
    /// </summary>
    [Fact]
    public void ForHttp_AcceptsPositiveTimeoutBoundaries()
    {
        var minimum = ResiliencePolicyDescriptor.ForHttp(
            "MinimumTimeout",
            OperationKind.Read,
            TimeSpan.FromTicks(1));
        var maximum = ResiliencePolicyDescriptor.ForHttp(
            "MaximumTimeout",
            OperationKind.Read,
            TimeSpan.MaxValue);

        minimum.Timeout.Should().Be(TimeSpan.FromTicks(1));
        maximum.Timeout.Should().Be(TimeSpan.MaxValue);
    }
}
