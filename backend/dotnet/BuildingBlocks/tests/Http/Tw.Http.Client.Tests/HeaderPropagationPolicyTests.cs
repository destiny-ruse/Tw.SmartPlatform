using AwesomeAssertions;
using Tw.Http.Client.HeaderPropagation;
using Xunit;

namespace Tw.Http.Client.Tests;

/// <summary>
/// 覆盖HeaderPropagation策略的核心行为和边界条件
/// </summary>
public sealed class HeaderPropagationPolicyTests
{
    /// <summary>
    /// 验证ShouldPropagate不PropagateClient租户Header
    /// </summary>
    [Fact]
    public void ShouldPropagate_DoesNotPropagateClientTenantHeader()
    {
        HeaderPropagationPolicy.ShouldPropagate("X-Tenant-Id", HeaderTrustLevel.ClientSupplied)
            .Should()
            .BeFalse();
    }

    /// <summary>
    /// 验证ShouldPropagatePropagatesVerified租户Header
    /// </summary>
    [Fact]
    public void ShouldPropagate_PropagatesVerifiedTenantHeader()
    {
        HeaderPropagationPolicy.ShouldPropagate("X-Tenant-Id", HeaderTrustLevel.Verified)
            .Should()
            .BeTrue();
    }
}
