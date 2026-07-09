namespace Tw.EventBus.Cap.Consumers;

public sealed record CapConsumerContext(string MessageId, string TenantId, string ShardId, string Culture);
