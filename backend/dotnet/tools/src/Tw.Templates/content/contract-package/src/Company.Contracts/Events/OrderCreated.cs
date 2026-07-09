namespace Company.Contracts.Events;

/// <summary>表示 OrderCreated 声明</summary>
public sealed record OrderCreated(string EventId, string TenantId, string CorrelationId, string OrderId, string IdempotencyKey);
