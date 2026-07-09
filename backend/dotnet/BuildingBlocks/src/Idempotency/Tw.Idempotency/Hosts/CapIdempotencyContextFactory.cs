namespace Tw.Idempotency.Hosts;

public static class CapIdempotencyContextFactory
{
    public static IdempotencyKey Create(string tenantId, string eventName, string messageId)
    {
        HttpIdempotencyContextFactory.Validate(tenantId, eventName, messageId);
        return new IdempotencyKey(IdempotencyBoundary.Cap, tenantId, eventName, "Consume", messageId);
    }
}
