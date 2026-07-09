using AwesomeAssertions;
using Tw.MultiTenancy;
using Xunit;

namespace Tw.MultiTenancy.Tests;

/// <summary>验证 TenantResolverTests 相关行为</summary>
public sealed class TenantResolverTests
{
    /// <summary>验证 Resolve_RejectsHeaderTenantWhenTokenTenantDiffers 场景</summary>
    [Fact]
    public void Resolve_RejectsHeaderTenantWhenTokenTenantDiffers()
    {
        var resolver = new TenantResolver();

        var act = () => resolver.Resolve(tokenTenantId: "tenant-a", hintedTenantId: "tenant-b");

        act.Should().Throw<TenantMismatchException>()
            .WithMessage("Tenant id does not match the authenticated token tenant.");
    }

    /// <summary>验证 Resolve_UsesDefaultTenant_WhenNoTenantIsProvided 场景</summary>
    [Fact]
    public void Resolve_UsesDefaultTenant_WhenNoTenantIsProvided()
    {
        var resolver = new TenantResolver();

        var tenant = resolver.Resolve(tokenTenantId: null, hintedTenantId: null);

        tenant.Id.Should().Be("default");
    }
}
