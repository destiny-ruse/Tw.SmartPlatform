namespace Tw.AspNetCore.Abstractions;

public sealed record ProtocolError(int StatusCode, string Code, string Message, string? TraceId)
{
    public static ProtocolError Conflict(string code, string message, string? traceId = null)
    {
        return new ProtocolError(409, code, message, traceId);
    }
}
