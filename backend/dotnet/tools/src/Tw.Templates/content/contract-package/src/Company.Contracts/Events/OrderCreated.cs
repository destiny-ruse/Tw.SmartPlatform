namespace Company.Contracts.Events;

public sealed record OrderCreated(string EventId, string TenantId, string CorrelationId, string OrderId, string IdempotencyKey);
