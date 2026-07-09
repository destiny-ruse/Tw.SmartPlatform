namespace Tw.Idempotency.Hosts;

/// <summary>表示 CapIdempotencyContextFactory 类型</summary>
public static class CapIdempotencyContextFactory
{
    /// <summary>执行 Create 操作</summary>
    /// <param name="tenantId">tenantId 参数</param>
    /// <param name="eventName">eventName 参数</param>
    /// <param name="messageId">messageId 参数</param>
    /// <returns>Create 的执行结果</returns>
    public static IdempotencyKey Create(string tenantId, string eventName, string messageId)
    {
        HttpIdempotencyContextFactory.Validate(tenantId, eventName, messageId);
        return new IdempotencyKey(IdempotencyBoundary.Cap, tenantId, eventName, "Consume", messageId);
    }
}
