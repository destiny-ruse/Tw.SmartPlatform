namespace Tw.Data.Repositories;

/// <summary>定义 IRepository 契约</summary>
/// <typeparam name="TEntity">TEntity 类型参数</typeparam>
/// <typeparam name="TKey">TKey 类型参数</typeparam>
public interface IRepository<TEntity, TKey>
{
    /// <summary>执行 FindAsync 操作</summary>
    /// <param name="id">id 参数</param>
    /// <param name="cancellationToken">cancellationToken 参数</param>
    /// <returns>FindAsync 的执行结果</returns>
    Task<TEntity?> FindAsync(TKey id, CancellationToken cancellationToken = default);

    /// <summary>执行 InsertAsync 操作</summary>
    /// <param name="entity">entity 参数</param>
    /// <param name="cancellationToken">cancellationToken 参数</param>
    /// <returns>InsertAsync 的执行结果</returns>
    Task InsertAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>执行 UpdateAsync 操作</summary>
    /// <param name="entity">entity 参数</param>
    /// <param name="cancellationToken">cancellationToken 参数</param>
    /// <returns>UpdateAsync 的执行结果</returns>
    Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>执行 DeleteAsync 操作</summary>
    /// <param name="entity">entity 参数</param>
    /// <param name="cancellationToken">cancellationToken 参数</param>
    /// <returns>DeleteAsync 的执行结果</returns>
    Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default);
}
