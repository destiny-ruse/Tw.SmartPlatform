namespace Tw.Idempotency.Hosts;

/// <summary>
/// 根据 CAP 消息标识创建幂等键上下文
/// </summary>
public static class CapIdempotencyContextFactory
{
    /// <summary>
    /// 创建统一 API 错误响应对象
    /// </summary>
    /// <param name="tenantId">用于提供tenant标识</param>
    /// <param name="eventName">用于提供eventName</param>
    /// <param name="messageId">用于提供消息标识</param>
    /// <returns>方法计算得到的文本值</returns>
    public static IdempotencyKey Create(string tenantId, string eventName, string messageId)
    {
        HttpIdempotencyContextFactory.Validate(tenantId, eventName, messageId);
        return new IdempotencyKey(IdempotencyBoundary.Cap, tenantId, eventName, "Consume", messageId);
    }
}
