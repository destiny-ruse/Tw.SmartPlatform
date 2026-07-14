namespace Tw.Http;

/// <summary>
/// 提供平台 HTTP 协议边界使用的稳定请求头名称
/// </summary>
public static class HttpHeaderNames
{
    /// <summary>
    /// 跨服务关联一次业务请求的请求头名称
    /// </summary>
    public const string CorrelationId = "X-Correlation-Id";

    /// <summary>
    /// 仅在可信服务端边界验证后传播的租户标识请求头名称
    /// </summary>
    public const string TenantId = "X-Tenant-Id";
}
