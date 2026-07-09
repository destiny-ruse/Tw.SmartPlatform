using AwesomeAssertions;
using Tw.Gateway;
using Xunit;

namespace Tw.Gateway.Tests;

public sealed class GatewayHeaderSanitizerTests
{
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
