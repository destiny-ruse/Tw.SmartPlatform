namespace Tw.Auditing.Contracts;

/// <summary>
/// 封装审计事件相关的数据和行为
/// </summary>
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
    /// <summary>
    /// 说明Security拒绝在当前类型中的职责
    /// </summary>
    /// <param name="actor">用于提供actor</param>
    /// <param name="actionName">目标 MVC Action 的名称</param>
    /// <param name="errorCode">用于提供错误代码</param>
    /// <returns>方法计算得到的文本值</returns>
    public static AuditEvent SecurityDenied(AuditActor actor, string actionName, string errorCode)
    {
        return new AuditEvent(actor, new AuditAction(actionName), string.Empty, "Denied", errorCode, null, null, null, DateTimeOffset.UtcNow);
    }

    /// <summary>
    /// 说明ConfigurationChanged在当前类型中的职责
    /// </summary>
    /// <param name="actor">用于提供actor</param>
    /// <param name="key">用于定位目标数据或缓存项的键</param>
    /// <param name="oldValue">用于提供old值</param>
    /// <param name="newValue">用于提供new值</param>
    /// <returns>方法计算得到的文本值</returns>
    public static AuditEvent ConfigurationChanged(AuditActor actor, string key, string oldValue, string newValue)
    {
        return new AuditEvent(actor, new AuditAction("Configuration.Changed"), key, "Succeeded", null, $"old={oldValue};new={newValue}", null, null, DateTimeOffset.UtcNow);
    }
}
