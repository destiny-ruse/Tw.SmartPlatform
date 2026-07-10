using AwesomeAssertions;
using Tw.Gateway;
using Xunit;

namespace Tw.Gateway.Tests;

/// <summary>
/// 覆盖GatewayHeaderSanitizer的核心行为和边界条件
/// </summary>
public sealed class GatewayHeaderSanitizerTests
{
    /// <summary>
    /// 验证SanitizeRemovesCallerSupplied身份租户权限和RoleHeaders
    /// </summary>
    [Fact]
    public void Sanitize_RemovesCallerSuppliedIdentityTenantPermissionAndRoleHeaders()
    {
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Authorization"] = "Bearer token",
            ["X-Tenant-Id"] = "forged",
            ["X-User-Id"] = "forged",
            ["X-Permissions"] = "forged",
            ["X-Roles"] = "forged"
        };

        var sanitized = GatewayHeaderSanitizer.Sanitize(headers);

        sanitized.Should().ContainKey("Authorization");
        sanitized.Should().NotContainKey("X-Tenant-Id");
        sanitized.Should().NotContainKey("X-User-Id");
        sanitized.Should().NotContainKey("X-Permissions");
        sanitized.Should().NotContainKey("X-Roles");
    }
}
