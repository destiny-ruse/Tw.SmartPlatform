using Tw.Data.SqlSugar.Connection;
using Tw.Uow;

namespace Tw.Data.SqlSugar.Uow;

/// <summary>
/// 封装SqlSugarUnitOfWork相关的数据和行为
/// </summary>
public sealed class SqlSugarUnitOfWork : IUnitOfWork, IOutboxTransactionBoundary
{
    /// <summary>
    /// 保存当前类型处理流程依赖的restoreCurrent
    /// </summary>
    private readonly Action _restoreCurrent;
    /// <summary>
    /// 保存当前类型处理流程依赖的disposed
    /// </summary>
    private bool _disposed;

    /// <summary>
    /// 初始化 SqlSugarUnitOfWork 实例
    /// </summary>
    /// <param name="clientFactory">用于提供client工厂</param>
    /// <param name="restoreCurrent">用于提供restoreCurrent</param>
    /// <param name="cancellationToken">用于传播调用方取消请求的令牌</param>
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

    /// <summary>
    /// Client在当前对象中的业务含义
    /// </summary>
    public object Client { get; }

    /// <summary>
    /// Cancellation令牌在当前对象中的业务含义
    /// </summary>
    public CancellationToken CancellationToken { get; }

    /// <summary>
    /// CanWriteOutbox在当前对象中的业务含义
    /// </summary>
    public bool CanWriteOutbox => !_disposed;

    /// <summary>
    /// sCompleted在当前对象中的业务含义
    /// </summary>
    public bool IsCompleted { get; private set; }

    /// <summary>
    /// sRolled回在当前对象中的业务含义
    /// </summary>
    public bool IsRolledBack { get; private set; }

    /// <summary>
    /// 提交测试事务上下文
    /// </summary>
    /// <param name="cancellationToken">用于传播调用方取消请求的令牌</param>
    /// <returns>表示异步流程完成状态的任务</returns>
    public Task CommitAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IsCompleted = true;
        return Task.CompletedTask;
    }

    /// <summary>
    /// 回滚测试事务上下文
    /// </summary>
    /// <param name="cancellationToken">用于传播调用方取消请求的令牌</param>
    /// <returns>表示异步流程完成状态的任务</returns>
    public Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IsCompleted = true;
        IsRolledBack = true;
        return Task.CompletedTask;
    }

    /// <summary>
    /// 释放测试事务上下文
    /// </summary>
    /// <returns>表示异步流程完成状态的任务</returns>
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
