namespace Tw.Http;

/// <summary>
/// 封装HttpHeader名称集合相关的数据和行为
/// </summary>
public static class HttpHeaderNames
{
    /// <summary>
    /// 当前类型内部复用的Correlation标识常量值
    /// </summary>
    public const string CorrelationId = "X-Correlation-Id";

    /// <summary>
    /// 当前类型内部复用的租户标识常量值
    /// </summary>
    public const string TenantId = "X-Tenant-Id";
}
