namespace Tw.EventBus.Cap.Storage;

/// <summary>
/// 封装SqlSugarCapStorage架构相关的数据和行为
/// </summary>
public sealed record SqlSugarCapStorageSchema(IReadOnlyList<string> RequiredTables, bool IsTenantSharded)
{
    /// <summary>
    /// 说明FromOptions在当前类型中的职责
    /// </summary>
    /// <param name="options">用于配置当前组件行为的选项</param>
    /// <returns>方法完成后返回给调用方的结果对象</returns>
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
