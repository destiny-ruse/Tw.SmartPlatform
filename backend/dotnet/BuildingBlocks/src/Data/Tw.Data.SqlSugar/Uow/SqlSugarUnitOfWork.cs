using Tw.Data.SqlSugar.Connection;
using Tw.Data.Uow;

namespace Tw.Data.SqlSugar.Uow;

/// <summary>
/// 表示由 SqlSugar 客户端承载的工作单元及其 Outbox 事务状态
/// </summary>
public sealed class SqlSugarUnitOfWork : IUnitOfWork, IOutboxTransactionBoundary
{
    /// <summary>
    /// 在当前工作单元释放时恢复先前活动作用域的回调
    /// </summary>
    private readonly Action _restoreCurrent;

    /// <summary>
    /// 当前工作单元是否已经释放
    /// </summary>
    private bool _disposed;

    /// <summary>
    /// 使用指定客户端工厂创建 SqlSugar 工作单元
    /// </summary>
    /// <param name="clientFactory">为当前作用域创建 SqlSugar 客户端的工厂</param>
    /// <param name="restoreCurrent">释放当前作用域时恢复先前工作单元的回调</param>
    /// <param name="cancellationToken">创建当前工作单元时关联的取消令牌</param>
    /// <exception cref="ArgumentNullException"><paramref name="clientFactory"/> 或 <paramref name="restoreCurrent"/> 为 <see langword="null"/></exception>
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
    /// 当前工作单元使用的 SqlSugar 客户端实例
    /// </summary>
    public object Client { get; }

    /// <summary>
    /// 创建当前工作单元时关联的取消令牌
    /// </summary>
    public CancellationToken CancellationToken { get; }

    /// <summary>
    /// 当前工作单元是否仍允许写入同一事务边界内的 Outbox
    /// </summary>
    public bool CanWriteOutbox => !_disposed && !IsCompleted;

    /// <summary>
    /// 当前事务边界是否已经提交或回滚
    /// </summary>
    public bool IsCompleted { get; private set; }

    /// <summary>
    /// 当前事务边界是否通过回滚完成
    /// </summary>
    public bool IsRolledBack { get; private set; }

    /// <summary>
    /// 提交当前工作单元并将事务边界标记为完成
    /// </summary>
    /// <param name="cancellationToken">等待提交完成时使用的取消令牌</param>
    /// <returns>提交完成任务</returns>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> 已请求取消</exception>
    public Task CommitAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IsCompleted = true;
        return Task.CompletedTask;
    }

    /// <summary>
    /// 回滚当前工作单元并将事务边界标记为完成
    /// </summary>
    /// <param name="cancellationToken">等待回滚完成时使用的取消令牌</param>
    /// <returns>回滚完成任务</returns>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> 已请求取消</exception>
    public Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IsCompleted = true;
        IsRolledBack = true;
        return Task.CompletedTask;
    }

    /// <summary>
    /// 恢复先前的活动工作单元并关闭当前 Outbox 写入边界
    /// </summary>
    /// <returns>释放完成状态</returns>
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
