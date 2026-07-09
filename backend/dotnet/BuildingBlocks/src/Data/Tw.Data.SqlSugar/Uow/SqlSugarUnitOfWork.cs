using Tw.Data.SqlSugar.Connection;
using Tw.Uow;

namespace Tw.Data.SqlSugar.Uow;

public sealed class SqlSugarUnitOfWork : IUnitOfWork, IOutboxTransactionBoundary
{
    private readonly Action _restoreCurrent;
    private bool _disposed;

    public SqlSugarUnitOfWork(
        ISqlSugarClientFactory clientFactory,
        Action restoreCurrent,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(clientFactory);
        ArgumentNullException.ThrowIfNull(restoreCurrent);

        Client = clientFactory.CreateClient(cancellationToken);
        _restoreCurrent = restoreCurrent;
        CancellationToken = cancellationToken;
    }

    public object Client { get; }

    public CancellationToken CancellationToken { get; }

    public bool CanWriteOutbox => !_disposed;

    public bool IsCompleted { get; private set; }

    public bool IsRolledBack { get; private set; }

    public Task CommitAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IsCompleted = true;
        return Task.CompletedTask;
    }

    public Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IsCompleted = true;
        IsRolledBack = true;
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return ValueTask.CompletedTask;
        }

        _restoreCurrent();
        _disposed = true;
        return ValueTask.CompletedTask;
    }
}
