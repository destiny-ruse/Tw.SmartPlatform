namespace Tw.Gateway;

/// <summary>
/// 封装GatewayHeader策略相关的数据和行为
/// </summary>
public static class GatewayHeaderPolicy
{
    /// <summary>
    /// Hash写入在当前对象中的业务含义
    /// </summary>
    public static IReadOnlySet<string> CallerSuppliedIdentityHeaders { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "X-Tenant-Id",
        "X-User-Id",
        "X-Permissions",
        "X-Roles"
    };
}
