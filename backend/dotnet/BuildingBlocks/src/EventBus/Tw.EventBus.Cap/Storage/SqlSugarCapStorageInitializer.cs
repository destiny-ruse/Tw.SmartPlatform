namespace Tw.EventBus.Cap.Storage;

/// <summary>
/// 封装SqlSugarCapStorageInitializer相关的数据和行为
/// </summary>
public sealed class SqlSugarCapStorageInitializer(SqlSugarCapStorageSchema schema)
{
    /// <summary>
    /// 架构在当前对象中的业务含义
    /// </summary>
    public SqlSugarCapStorageSchema Schema { get; } = schema;

    /// <summary>
    /// 说明nitializeAsync在当前类型中的职责
    /// </summary>
    /// <param name="cancellationToken">用于传播调用方取消请求的令牌</param>
    /// <returns>表示异步流程完成状态的任务</returns>
    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }
}
