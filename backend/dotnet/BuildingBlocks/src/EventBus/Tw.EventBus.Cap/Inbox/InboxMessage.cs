namespace Tw.EventBus.Cap.Inbox;

/// <summary>表示 InboxMessage 声明</summary>
public sealed record InboxMessage(
    string MessageId,
    string TenantId,
    string ShardId,
    string Culture,
    DateTimeOffset ReceivedAt);
