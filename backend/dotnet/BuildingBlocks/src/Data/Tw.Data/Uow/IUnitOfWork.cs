namespace Tw.Data.Uow;

/// <summary>
/// 封装一次提交或回滚的数据变更边界
/// </summary>
/// <remarks>
/// 成功提交、成功回滚或释放工作单元后均不得继续写入 Outbox。
/// 释放仅结束当前作用域，不隐式提交数据变更；调用方不得重复提交或回滚同一工作单元。
/// </remarks>
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
    /// <remarks>成功返回后，当前事务边界进入完成状态且不再允许 Outbox 写入</remarks>
    Task CommitAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 回滚当前工作单元中的数据变更
    /// </summary>
    /// <param name="cancellationToken">等待回滚完成时使用的取消令牌</param>
    /// <returns>回滚完成任务</returns>
    /// <remarks>成功返回后，当前事务边界进入完成状态且不再允许 Outbox 写入</remarks>
    Task RollbackAsync(CancellationToken cancellationToken = default);
}
