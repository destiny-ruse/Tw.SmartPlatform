namespace Tw.AspNetCore.Abstractions;

/// <summary>表示 ProtocolError 声明</summary>
public sealed record ProtocolError(int StatusCode, string Code, string Message, string? TraceId)
{
    /// <summary>执行 Conflict 操作</summary>
    /// <param name="code">code 参数</param>
    /// <param name="message">message 参数</param>
    /// <param name="traceId">traceId 参数</param>
    /// <returns>Conflict 的执行结果</returns>
    public static ProtocolError Conflict(string code, string message, string? traceId = null)
    {
        return new ProtocolError(409, code, message, traceId);
    }
}
