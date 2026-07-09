using AwesomeAssertions;
using Tw.MultiTenancy;
using Xunit;

namespace Tw.MultiTenancy.Tests;

public sealed class TenantResolverTests
{
    [Fact]
    public void Resolve_RejectsHeaderTenantWhenTokenTenantDiffers()
    {
        var resolver = new TenantResolver();

        var act = () => resolver.Resolve(tokenTenantId: "tenant-a", hintedTenantId: "tenant-b");

        act.Should().Throw<TenantMismatchException>()
            .WithMessage("Tenant id does not match the authenticated token tenant.");
    }

    [Fact]
    public void Resolve_UsesDefaultTenant_WhenNoTenantIsProvided()
    {
        var resolver = new TenantResolver();

        var tenant = resolver.Resolve(tokenTenantId: null, hintedTenantId: null);

        tenant.Id.Should().Be("default");
    }
}
