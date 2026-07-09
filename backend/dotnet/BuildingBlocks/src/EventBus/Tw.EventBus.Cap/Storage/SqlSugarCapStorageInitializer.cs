namespace Tw.EventBus.Cap.Storage;

/// <summary>表示 SqlSugarCapStorageInitializer 类型</summary>
public sealed class SqlSugarCapStorageInitializer(SqlSugarCapStorageSchema schema)
{
    /// <summary>表示 Schema 属性</summary>
    public SqlSugarCapStorageSchema Schema { get; } = schema;

    /// <summary>执行 InitializeAsync 操作</summary>
    /// <param name="cancellationToken">cancellationToken 参数</param>
    /// <returns>InitializeAsync 的执行结果</returns>
    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }
}
