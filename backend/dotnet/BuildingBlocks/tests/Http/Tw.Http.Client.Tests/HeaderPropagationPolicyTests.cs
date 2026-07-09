using AwesomeAssertions;
using Tw.Http.Client.HeaderPropagation;
using Xunit;

namespace Tw.Http.Client.Tests;

public sealed class HeaderPropagationPolicyTests
{
    [Fact]
    public void ShouldPropagate_DoesNotPropagateClientTenantHeader()
    {
        HeaderPropagationPolicy.ShouldPropagate("X-Tenant-Id", HeaderTrustLevel.ClientSupplied)
            .Should()
            .BeFalse();
    }

    [Fact]
    public void ShouldPropagate_PropagatesVerifiedTenantHeader()
    {
        HeaderPropagationPolicy.ShouldPropagate("X-Tenant-Id", HeaderTrustLevel.Verified)
            .Should()
            .BeTrue();
    }
}
