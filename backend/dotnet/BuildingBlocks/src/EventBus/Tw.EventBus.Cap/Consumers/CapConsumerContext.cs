namespace Tw.EventBus.Cap.Consumers;

/// <summary>表示 CapConsumerContext 声明</summary>
public sealed record CapConsumerContext(string MessageId, string TenantId, string ShardId, string Culture);
