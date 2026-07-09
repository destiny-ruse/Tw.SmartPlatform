namespace Tw.EventBus.Cap.Storage;

public sealed record SqlSugarCapStorageSchema(IReadOnlyList<string> RequiredTables, bool IsTenantSharded)
{
    public static SqlSugarCapStorageSchema FromOptions(SqlSugarCapStorageOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();

        return new SqlSugarCapStorageSchema(
            [
                $"{options.Schema}.{options.PublishedTable}",
                $"{options.Schema}.{options.ReceivedTable}",
                $"{options.Schema}.{options.LockTable}"
            ],
            IsTenantSharded: false);
    }
}
