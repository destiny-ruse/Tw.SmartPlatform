namespace Tw.Auditing.Contracts;

/// <summary>表示 AuditEvent 声明</summary>
public sealed record AuditEvent(
    AuditActor Actor,
    AuditAction Action,
    string Resource,
    string Result,
    string? ErrorCode,
    string? Details,
    string? CorrelationId,
    string? TraceId,
    DateTimeOffset Timestamp)
{
    /// <summary>执行 SecurityDenied 操作</summary>
    /// <param name="actor">actor 参数</param>
    /// <param name="actionName">actionName 参数</param>
    /// <param name="errorCode">errorCode 参数</param>
    /// <returns>SecurityDenied 的执行结果</returns>
    public static AuditEvent SecurityDenied(AuditActor actor, string actionName, string errorCode)
    {
        return new AuditEvent(actor, new AuditAction(actionName), string.Empty, "Denied", errorCode, null, null, null, DateTimeOffset.UtcNow);
    }

    /// <summary>执行 ConfigurationChanged 操作</summary>
    /// <param name="actor">actor 参数</param>
    /// <param name="key">key 参数</param>
    /// <param name="oldValue">oldValue 参数</param>
    /// <param name="newValue">newValue 参数</param>
    /// <returns>ConfigurationChanged 的执行结果</returns>
    public static AuditEvent ConfigurationChanged(AuditActor actor, string key, string oldValue, string newValue)
    {
        return new AuditEvent(actor, new AuditAction("Configuration.Changed"), key, "Succeeded", null, $"old={oldValue};new={newValue}", null, null, DateTimeOffset.UtcNow);
    }
}
