namespace Tw.Data.Repositories;

public interface IRepository<TEntity, TKey>
{
    Task<TEntity?> FindAsync(TKey id, CancellationToken cancellationToken = default);

    Task InsertAsync(TEntity entity, CancellationToken cancellationToken = default);

    Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);

    Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default);
}
