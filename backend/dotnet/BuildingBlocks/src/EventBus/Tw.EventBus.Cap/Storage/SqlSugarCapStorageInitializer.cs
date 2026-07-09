namespace Tw.EventBus.Cap.Storage;

public sealed class SqlSugarCapStorageInitializer(SqlSugarCapStorageSchema schema)
{
    public SqlSugarCapStorageSchema Schema { get; } = schema;

    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }
}
