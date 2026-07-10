namespace Company.Contracts.Events;

/// <summary>
/// 封装OrderCreated相关的数据和行为
/// </summary>
public sealed record OrderCreated(string EventId, string TenantId, string CorrelationId, string OrderId, string IdempotencyKey);
