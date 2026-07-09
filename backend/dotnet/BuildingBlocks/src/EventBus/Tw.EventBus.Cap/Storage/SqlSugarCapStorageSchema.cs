namespace Tw.EventBus.Cap.Storage;

/// <summary>表示 SqlSugarCapStorageSchema 声明</summary>
public sealed record SqlSugarCapStorageSchema(IReadOnlyList<string> RequiredTables, bool IsTenantSharded)
{
    /// <summary>执行 FromOptions 操作</summary>
    /// <param name="options">options 参数</param>
    /// <returns>FromOptions 的执行结果</returns>
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
