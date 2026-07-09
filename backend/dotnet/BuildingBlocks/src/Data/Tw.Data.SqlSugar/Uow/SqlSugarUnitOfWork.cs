using Tw.Data.SqlSugar.Connection;
using Tw.Uow;

namespace Tw.Data.SqlSugar.Uow;

/// <summary>表示 SqlSugarUnitOfWork 类型</summary>
public sealed class SqlSugarUnitOfWork : IUnitOfWork, IOutboxTransactionBoundary
{
    /// <summary>表示 _restoreCurrent 字段</summary>
    private readonly Action _restoreCurrent;
    /// <summary>表示 _disposed 字段</summary>
    private bool _disposed;

    /// <summary>初始化 SqlSugarUnitOfWork 实例</summary>
    /// <param name="clientFactory">clientFactory 参数</param>
    /// <param name="restoreCurrent">restoreCurrent 参数</param>
    /// <param name="cancellationToken">cancellationToken 参数</param>
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

    /// <summary>表示 Client 属性</summary>
    public object Client { get; }

    /// <summary>表示 CancellationToken 属性</summary>
    public CancellationToken CancellationToken { get; }

    /// <summary>表示 CanWriteOutbox 属性</summary>
    public bool CanWriteOutbox => !_disposed;

    /// <summary>表示 IsCompleted 属性</summary>
    public bool IsCompleted { get; private set; }

    /// <summary>表示 IsRolledBack 属性</summary>
    public bool IsRolledBack { get; private set; }

    /// <summary>执行 CommitAsync 操作</summary>
    /// <param name="cancellationToken">cancellationToken 参数</param>
    /// <returns>CommitAsync 的执行结果</returns>
    public Task CommitAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IsCompleted = true;
        return Task.CompletedTask;
    }

    /// <summary>执行 RollbackAsync 操作</summary>
    /// <param name="cancellationToken">cancellationToken 参数</param>
    /// <returns>RollbackAsync 的执行结果</returns>
    public Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IsCompleted = true;
        IsRolledBack = true;
        return Task.CompletedTask;
    }

    /// <summary>执行 DisposeAsync 操作</summary>
    /// <returns>DisposeAsync 的执行结果</returns>
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
