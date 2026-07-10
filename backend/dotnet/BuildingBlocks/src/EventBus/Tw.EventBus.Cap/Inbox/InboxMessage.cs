namespace Tw.EventBus.Cap.Inbox;

/// <summary>
/// 封装nbox消息相关的数据和行为
/// </summary>
public sealed record InboxMessage(
    string MessageId,
    string TenantId,
    string ShardId,
    string Culture,
    DateTimeOffset ReceivedAt);
