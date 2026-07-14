using AwesomeAssertions;
using Tw.Authorization;
using Xunit;

namespace Tw.Authorization.Tests;

/// <summary>
/// 验证授权结果、权限定义与缓存键的公开值契约
/// </summary>
public sealed class AuthorizationContractTests
{
    /// <summary>
    /// 成功结果使用允许标记与稳定系统结果信息
    /// </summary>
    [Fact]
    public void Success_ReturnsAllowedResultWithStableMetadata()
    {
        var result = AuthorizationResult.Success();

        result.Allowed.Should().BeTrue();
        result.Code.Should().Be("SYSTEM:000000");
        result.Message.Should().Be("success");
    }

    /// <summary>
    /// 拒绝结果保留调用方提供的稳定错误信息
    /// </summary>
    [Fact]
    public void Denied_ReturnsRejectedResultWithProvidedMetadata()
    {
        var result = AuthorizationResult.Denied("AUTHORIZATION:000099", "当前主体无权访问资源");

        result.Allowed.Should().BeFalse();
        result.Code.Should().Be("AUTHORIZATION:000099");
        result.Message.Should().Be("当前主体无权访问资源");
    }

    /// <summary>
    /// 权限定义保留稳定名称与显示元数据并提供值相等语义
    /// </summary>
    [Fact]
    public void PermissionDefinition_PreservesNameDisplayMetadataAndValueShape()
    {
        var definition = new PermissionDefinition("orders.approve", "审批订单");

        definition.Name.Should().Be("orders.approve");
        definition.DisplayName.Should().Be("审批订单");
        definition.Should().Be(new PermissionDefinition("orders.approve", "审批订单"));
    }

    /// <summary>
    /// 权限缓存键的全部稳定字段共同参与值相等判断
    /// </summary>
    [Fact]
    public void PermissionGrantCacheKey_UsesEveryStableFieldForEquality()
    {
        var key = new PermissionGrantCacheKey(
            "user-1",
            "tenant-1",
            "orders.approve",
            "Order",
            "order-1");

        key.Should().Be(new PermissionGrantCacheKey(
            "user-1",
            "tenant-1",
            "orders.approve",
            "Order",
            "order-1"));
        key.Should().NotBe(key with { SubjectId = "user-2" });
        key.Should().NotBe(key with { TenantId = "tenant-2" });
        key.Should().NotBe(key with { Permission = "orders.read" });
        key.Should().NotBe(key with { ResourceType = "Invoice" });
        key.Should().NotBe(key with { ResourceId = "order-2" });
    }

    /// <summary>
    /// 无资源范围的权限缓存键保留空字段并保持值相等语义
    /// </summary>
    [Fact]
    public void PermissionGrantCacheKey_PreservesNullResourceScope()
    {
        var key = new PermissionGrantCacheKey(
            "user-1",
            "tenant-1",
            "orders.list",
            ResourceType: null,
            ResourceId: null);

        key.ResourceType.Should().BeNull();
        key.ResourceId.Should().BeNull();
        key.Should().Be(new PermissionGrantCacheKey(
            "user-1",
            "tenant-1",
            "orders.list",
            ResourceType: null,
            ResourceId: null));
    }
}
