namespace Tw.Http.Client.HeaderPropagation;

/// <summary>
/// 定义 HeaderTrustLevel 枚举
/// </summary>
public enum HeaderTrustLevel
{
    /// <summary>
    /// 表示 ClientSupplied 枚举值
    /// </summary>
    ClientSupplied,
    /// <summary>
    /// 表示 Verified 枚举值
    /// </summary>
    Verified
}

/// <summary>
/// 封装HeaderPropagation策略相关的数据和行为
/// </summary>
public static class HeaderPropagationPolicy
{
    /// <summary>
    /// 保存当前类型处理流程依赖的AllowList
    /// </summary>
    private static readonly HashSet<string> AllowList = new(StringComparer.OrdinalIgnoreCase)
    {
        "Authorization",
        "traceparent",
        "tracestate",
        "X-Correlation-Id",
        "X-Tenant-Id",
        "X-Culture",
        "Idempotency-Key"
    };

    /// <summary>
    /// 说明ShouldPropagate在当前类型中的职责
    /// </summary>
    /// <param name="headerName">用于提供headerName</param>
    /// <param name="trustLevel">用于提供trustLevel</param>
    /// <returns>条件满足时返回 <see langword="true"/></returns>
    public static bool ShouldPropagate(string headerName, HeaderTrustLevel trustLevel)
    {
        return AllowList.Contains(headerName)
            && (!string.Equals(headerName, "X-Tenant-Id", StringComparison.OrdinalIgnoreCase)
                || trustLevel == HeaderTrustLevel.Verified);
    }
}
