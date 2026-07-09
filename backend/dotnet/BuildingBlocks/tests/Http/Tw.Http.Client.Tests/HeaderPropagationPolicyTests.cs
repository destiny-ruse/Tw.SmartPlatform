using AwesomeAssertions;
using Tw.Http.Client.HeaderPropagation;
using Xunit;

namespace Tw.Http.Client.Tests;

/// <summary>验证 HeaderPropagationPolicyTests 相关行为</summary>
public sealed class HeaderPropagationPolicyTests
{
    /// <summary>验证 ShouldPropagate_DoesNotPropagateClientTenantHeader 场景</summary>
    [Fact]
    public void ShouldPropagate_DoesNotPropagateClientTenantHeader()
    {
        HeaderPropagationPolicy.ShouldPropagate("X-Tenant-Id", HeaderTrustLevel.ClientSupplied)
            .Should()
            .BeFalse();
    }

    /// <summary>验证 ShouldPropagate_PropagatesVerifiedTenantHeader 场景</summary>
    [Fact]
    public void ShouldPropagate_PropagatesVerifiedTenantHeader()
    {
        HeaderPropagationPolicy.ShouldPropagate("X-Tenant-Id", HeaderTrustLevel.Verified)
            .Should()
            .BeTrue();
    }
}
