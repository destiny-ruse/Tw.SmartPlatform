namespace Tw.Data.Uow;

/// <summary>
/// 封装一次提交或回滚的数据变更边界
/// </summary>
public interface IUnitOfWork : IAsyncDisposable
{
    /// <summary>
    /// 创建当前工作单元时关联的取消令牌
    /// </summary>
    CancellationToken CancellationToken { get; }

    /// <summary>
    /// 提交当前工作单元中的数据变更
    /// </summary>
    /// <param name="cancellationToken">等待提交完成时使用的取消令牌</param>
    /// <returns>提交完成任务</returns>
    /// <remarks>成功返回后，当前事务边界进入完成状态</remarks>
    Task CommitAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 回滚当前工作单元中的数据变更
    /// </summary>
    /// <param name="cancellationToken">等待回滚完成时使用的取消令牌</param>
    /// <returns>回滚完成任务</returns>
    /// <remarks>成功返回后，当前事务边界进入完成状态</remarks>
    Task RollbackAsync(CancellationToken cancellationToken = default);
}
