namespace Tw.EventBus.Cap.Inbox;

public interface IInboxMessageStore
{
    Task<bool> TryBeginAsync(InboxMessage message, CancellationToken cancellationToken = default);

    Task CompleteAsync(string messageId, CancellationToken cancellationToken = default);

    Task FailAsync(string messageId, Exception exception, CancellationToken cancellationToken = default);
}
