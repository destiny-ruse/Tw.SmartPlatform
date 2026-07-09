namespace Tw.Uow;

/// <summary>
/// 工作单元实例，封装一次提交或回滚边界
/// </summary>
public interface IUnitOfWork : IAsyncDisposable
{
    /// <summary>
    /// 工作单元关联的取消令牌
    /// </summary>
    CancellationToken CancellationToken { get; }

    /// <summary>
    /// 提交当前工作单元
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>表示异步提交操作的任务</returns>
    Task CommitAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 回滚当前工作单元
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>表示异步回滚操作的任务</returns>
    Task RollbackAsync(CancellationToken cancellationToken = default);
}
