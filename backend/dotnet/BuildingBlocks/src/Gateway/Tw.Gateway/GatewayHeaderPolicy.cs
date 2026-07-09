namespace Tw.Gateway;

public static class GatewayHeaderPolicy
{
    public static IReadOnlySet<string> CallerSuppliedIdentityHeaders { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "X-Tenant-Id",
        "X-User-Id",
        "X-Permissions",
        "X-Roles"
    };
}
