namespace Tw.AspNetCore.Mvc.Responses;

/// <summary>
/// 封装Api错误响应Factory相关的数据和行为
/// </summary>
public static class ApiErrorResponseFactory
{
    /// <summary>
    /// 创建统一 API 错误响应对象
    /// </summary>
    /// <param name="code">对外返回的稳定错误码</param>
    /// <param name="message">对外返回的安全错误消息</param>
    /// <param name="traceId">用于关联请求链路的 trace 标识</param>
    /// <param name="correlationId">用于跨系统关联诊断信息的 correlation 标识</param>
    /// <returns>方法计算得到的文本值</returns>
    public static ApiResponse<object> Create(string code, string message, string? traceId = null, string? correlationId = null)
    {
        return new ApiResponse<object>(false, code, message, null, traceId, correlationId, DateTimeOffset.UtcNow);
    }
}
