namespace Tw.Http.Client.HeaderPropagation;

/// <summary>定义 HeaderTrustLevel 枚举</summary>
public enum HeaderTrustLevel
{
    /// <summary>表示 ClientSupplied 枚举值</summary>
    ClientSupplied,
    /// <summary>表示 Verified 枚举值</summary>
    Verified
}

/// <summary>表示 HeaderPropagationPolicy 类型</summary>
public static class HeaderPropagationPolicy
{
    /// <summary>表示 AllowList 字段</summary>
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

    /// <summary>执行 ShouldPropagate 操作</summary>
    /// <param name="headerName">headerName 参数</param>
    /// <param name="trustLevel">trustLevel 参数</param>
    /// <returns>ShouldPropagate 的执行结果</returns>
    public static bool ShouldPropagate(string headerName, HeaderTrustLevel trustLevel)
    {
        return AllowList.Contains(headerName)
            && (!string.Equals(headerName, "X-Tenant-Id", StringComparison.OrdinalIgnoreCase)
                || trustLevel == HeaderTrustLevel.Verified);
    }
}
