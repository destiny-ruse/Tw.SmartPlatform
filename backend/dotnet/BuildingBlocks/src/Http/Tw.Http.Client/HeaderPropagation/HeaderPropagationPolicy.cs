namespace Tw.Http.Client.HeaderPropagation;

public enum HeaderTrustLevel
{
    ClientSupplied,
    Verified
}

public static class HeaderPropagationPolicy
{
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

    public static bool ShouldPropagate(string headerName, HeaderTrustLevel trustLevel)
    {
        return AllowList.Contains(headerName)
            && (!string.Equals(headerName, "X-Tenant-Id", StringComparison.OrdinalIgnoreCase)
                || trustLevel == HeaderTrustLevel.Verified);
    }
}
