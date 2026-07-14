namespace Tw.AspNetCore.Errors;

/// <summary>
/// 描述可由 ASP.NET Core 入口适配器映射的协议错误
/// </summary>
/// <param name="StatusCode">对外返回的 HTTP 状态码</param>
/// <param name="Code">供调用方稳定识别的错误码</param>
/// <param name="Message">不包含内部实现细节的安全错误消息</param>
/// <param name="TraceId">关联日志与链路追踪的可选标识</param>
public sealed record ProtocolError(int StatusCode, string Code, string Message, string? TraceId)
{
    /// <summary>
    /// 创建幂等请求冲突结果
    /// </summary>
    /// <param name="code">对外返回的稳定错误码</param>
    /// <param name="message">对外返回的安全错误消息</param>
    /// <param name="traceId">用于关联请求链路的 trace 标识</param>
    /// <returns>HTTP 状态码固定为 409 的协议错误</returns>
    public static ProtocolError Conflict(string code, string message, string? traceId = null)
    {
        return new ProtocolError(409, code, message, traceId);
    }
}
