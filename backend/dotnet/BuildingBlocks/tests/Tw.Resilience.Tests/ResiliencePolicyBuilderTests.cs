using AwesomeAssertions;
using Tw.Resilience;
using Xunit;

namespace Tw.Resilience.Tests;

public sealed class ResiliencePolicyBuilderTests
{
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
