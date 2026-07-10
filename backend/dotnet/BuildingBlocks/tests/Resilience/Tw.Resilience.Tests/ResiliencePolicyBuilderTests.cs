using AwesomeAssertions;
using Tw.Resilience;
using Xunit;

namespace Tw.Resilience.Tests;

/// <summary>
/// 覆盖Resilience策略构建器的核心行为和边界条件
/// </summary>
public sealed class ResiliencePolicyBuilderTests
{
    /// <summary>
    /// 验证BuildDisablesRetry针对NonIdempotentWrite
    /// </summary>
    [Fact]
    public void Build_DisablesRetryForNonIdempotentWrite()
    {
        var descriptor = ResiliencePolicyDescriptor.ForHttp(
            operationName: "CreateOrder",
            operationKind: OperationKind.NonIdempotentWrite,
            timeout: TimeSpan.FromSeconds(3));

        var policy = ResiliencePolicyBuilder.Build(descriptor);

        policy.RetryEnabled.Should().BeFalse();
        policy.Timeout.Should().Be(TimeSpan.FromSeconds(3));
    }
}
