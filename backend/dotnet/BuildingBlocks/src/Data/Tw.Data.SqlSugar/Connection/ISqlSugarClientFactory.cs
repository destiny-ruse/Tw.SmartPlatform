namespace Tw.Data.SqlSugar.Connection;

public interface ISqlSugarClientFactory
{
    object CreateClient(CancellationToken cancellationToken = default);
}
