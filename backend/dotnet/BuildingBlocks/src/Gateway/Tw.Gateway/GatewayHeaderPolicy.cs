namespace Tw.Gateway;

/// <summary>表示 GatewayHeaderPolicy 类型</summary>
public static class GatewayHeaderPolicy
{
    /// <summary>表示 CallerSuppliedIdentityHeaders 属性</summary>
    public static IReadOnlySet<string> CallerSuppliedIdentityHeaders { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "X-Tenant-Id",
        "X-User-Id",
        "X-Permissions",
        "X-Roles"
    };
}
