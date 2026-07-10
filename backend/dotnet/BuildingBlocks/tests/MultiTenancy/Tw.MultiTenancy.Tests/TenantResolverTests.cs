using AwesomeAssertions;
using Tw.MultiTenancy;
using Xunit;

namespace Tw.MultiTenancy.Tests;

/// <summary>
/// 覆盖租户Resolver的核心行为和边界条件
/// </summary>
public sealed class TenantResolverTests
{
    /// <summary>
    /// 验证Resolve拒绝Header租户当令牌租户Differs
    /// </summary>
    [Fact]
    public void Resolve_RejectsHeaderTenantWhenTokenTenantDiffers()
    {
        var resolver = new TenantResolver();

        var act = () => resolver.Resolve(tokenTenantId: "tenant-a", hintedTenantId: "tenant-b");

        act.Should().Throw<TenantMismatchException>()
            .WithMessage("Tenant id does not match the authenticated token tenant.");
    }

    /// <summary>
    /// 验证ResolveUses默认租户当No租户IsProvided
    /// </summary>
    [Fact]
    public void Resolve_UsesDefaultTenant_WhenNoTenantIsProvided()
    {
        var resolver = new TenantResolver();

        var tenant = resolver.Resolve(tokenTenantId: null, hintedTenantId: null);

        tenant.Id.Should().Be("default");
    }
}
