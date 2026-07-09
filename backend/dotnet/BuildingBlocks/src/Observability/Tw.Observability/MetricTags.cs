namespace Tw.Observability;

/// <summary>表示 MetricTags 声明</summary>
public sealed record MetricTags(IReadOnlyDictionary<string, string> Values)
{
    /// <summary>执行 Create 操作</summary>
    /// <param name="serviceName">serviceName 参数</param>
    /// <param name="tenantId">tenantId 参数</param>
    /// <param name="shardId">shardId 参数</param>
    /// <param name="operationName">operationName 参数</param>
    /// <returns>Create 的执行结果</returns>
    public static MetricTags Create(string serviceName, string tenantId, string shardId, string operationName)
    {
        return new MetricTags(new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["service.name"] = serviceName,
            ["tenant.id"] = tenantId,
            ["shard.id"] = shardId,
            ["operation.name"] = operationName
        });
    }
}
