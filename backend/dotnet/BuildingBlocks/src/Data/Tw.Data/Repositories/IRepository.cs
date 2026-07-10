namespace Tw.Data.Repositories;

/// <summary>
/// 定义仓库的能力边界
/// </summary>
/// <typeparam name="TEntity">响应数据的运行时类型</typeparam>
/// <typeparam name="TKey">响应数据的运行时类型</typeparam>
public interface IRepository<TEntity, TKey>
{
    /// <summary>
    /// 说明查找Async在当前类型中的职责
    /// </summary>
    /// <param name="id">解析得到的长整型标识</param>
    /// <param name="cancellationToken">用于传播调用方取消请求的令牌</param>
    /// <returns>异步流程完成后产生的TEntity</returns>
    Task<TEntity?> FindAsync(TKey id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 说明nsertAsync在当前类型中的职责
    /// </summary>
    /// <param name="entity">用于提供entity</param>
    /// <param name="cancellationToken">用于传播调用方取消请求的令牌</param>
    /// <returns>表示异步流程完成状态的任务</returns>
    Task InsertAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// 说明UpdateAsync在当前类型中的职责
    /// </summary>
    /// <param name="entity">用于提供entity</param>
    /// <param name="cancellationToken">用于传播调用方取消请求的令牌</param>
    /// <returns>表示异步流程完成状态的任务</returns>
    Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// 说明DeleteAsync在当前类型中的职责
    /// </summary>
    /// <param name="entity">用于提供entity</param>
    /// <param name="cancellationToken">用于传播调用方取消请求的令牌</param>
    /// <returns>表示异步流程完成状态的任务</returns>
    Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default);
}
