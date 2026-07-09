using System.Collections.Concurrent;

namespace Tw.EventBus.Cap.Inbox;

public sealed class SqlSugarInboxMessageStore : IInboxMessageStore
{
    private readonly ConcurrentDictionary<string, InboxMessage> _messages = new(StringComparer.Ordinal);

    public Task<bool> TryBeginAsync(InboxMessage message, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_messages.TryAdd(message.MessageId, message));
    }

    public Task CompleteAsync(string messageId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }

    public Task FailAsync(string messageId, Exception exception, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }
}
