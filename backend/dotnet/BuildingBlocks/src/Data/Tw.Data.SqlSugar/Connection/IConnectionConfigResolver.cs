namespace Tw.Data.SqlSugar.Connection;

public interface IConnectionConfigResolver
{
    Task<object> ResolveAsync(CancellationToken cancellationToken = default);
}
