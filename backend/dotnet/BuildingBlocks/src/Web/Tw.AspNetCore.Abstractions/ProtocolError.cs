namespace Tw.AspNetCore.Abstractions;

/// <summary>
/// 描述Protocol过程中发现的错误项
/// </summary>
public sealed record ProtocolError(int StatusCode, string Code, string Message, string? TraceId)
{
    /// <summary>
    /// 创建幂等请求冲突结果
    /// </summary>
    /// <param name="code">对外返回的稳定错误码</param>
    /// <param name="message">对外返回的安全错误消息</param>
    /// <param name="traceId">用于关联请求链路的 trace 标识</param>
    /// <returns>方法计算得到的文本值</returns>
    public static ProtocolError Conflict(string code, string message, string? traceId = null)
    {
        return new ProtocolError(409, code, message, traceId);
    }
}
