using AwesomeAssertions;
using Tw.MultiTenancy;
using Xunit;

namespace Tw.MultiTenancy.Tests;

/// <summary>
/// 覆盖当前租户默认值与认证后租户解析规则
/// </summary>
public sealed class TenantResolverTests
{
    /// <summary>
    /// 默认租户使用稳定的默认标识
    /// </summary>
    [Fact]
    public void CurrentTenantDefault_UsesStableDefaultIdentifier()
    {
        CurrentTenant.Default.Id.Should().Be("default");
    }

    /// <summary>
    /// 仅存在令牌租户时返回令牌租户
    /// </summary>
    [Fact]
    public void Resolve_ReturnsTokenTenant_WhenOnlyTokenTenantIsProvided()
    {
        var tenant = new TenantResolver().Resolve(tokenTenantId: "tenant-a", hintedTenantId: null);

        tenant.Should().Be(new CurrentTenant("tenant-a"));
    }

    /// <summary>
    /// 仅存在提示租户时返回提示租户
    /// </summary>
    [Fact]
    public void Resolve_ReturnsHintedTenant_WhenOnlyHintedTenantIsProvided()
    {
        var tenant = new TenantResolver().Resolve(tokenTenantId: null, hintedTenantId: "tenant-a");

        tenant.Should().Be(new CurrentTenant("tenant-a"));
    }

    /// <summary>
    /// 令牌租户与提示租户相同时返回该租户
    /// </summary>
    [Fact]
    public void Resolve_ReturnsTenant_WhenTokenAndHintMatch()
    {
        var tenant = new TenantResolver().Resolve(tokenTenantId: "tenant-a", hintedTenantId: "tenant-a");

        tenant.Should().Be(new CurrentTenant("tenant-a"));
    }

    /// <summary>
    /// 令牌租户与提示租户不同时拒绝解析
    /// </summary>
    [Fact]
    public void Resolve_RejectsHintedTenant_WhenTokenTenantDiffers()
    {
        var resolver = new TenantResolver();

        var act = () => resolver.Resolve(tokenTenantId: "tenant-a", hintedTenantId: "tenant-b");

        act.Should().Throw<TenantMismatchException>()
            .WithMessage("Tenant id does not match the authenticated token tenant.");
    }

    /// <summary>
    /// 未提供租户时返回默认租户
    /// </summary>
    [Fact]
    public void Resolve_UsesDefaultTenant_WhenNoTenantIsProvided()
    {
        var resolver = new TenantResolver();

        var tenant = resolver.Resolve(tokenTenantId: null, hintedTenantId: null);

        tenant.Id.Should().Be("default");
    }
}
