using AwesomeAssertions;
using Tw.Resilience;
using Xunit;

namespace Tw.Resilience.Tests;

/// <summary>验证 ResiliencePolicyBuilderTests 相关行为</summary>
public sealed class ResiliencePolicyBuilderTests
{
    /// <summary>验证 Build_DisablesRetryForNonIdempotentWrite 场景</summary>
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
