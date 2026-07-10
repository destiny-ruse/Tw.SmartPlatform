namespace Tw.EventBus.Cap.Consumers;

/// <summary>
/// 封装CapConsumer上下文相关的数据和行为
/// </summary>
public sealed record CapConsumerContext(string MessageId, string TenantId, string ShardId, string Culture);
