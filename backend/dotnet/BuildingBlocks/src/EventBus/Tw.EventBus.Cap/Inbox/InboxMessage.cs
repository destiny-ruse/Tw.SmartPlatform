namespace Tw.EventBus.Cap.Inbox;

public sealed record InboxMessage(
    string MessageId,
    string TenantId,
    string ShardId,
    string Culture,
    DateTimeOffset ReceivedAt);
