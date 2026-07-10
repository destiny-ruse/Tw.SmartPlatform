namespace Tw.Observability;

/// <summary>
/// 封装MetricTags相关的数据和行为
/// </summary>
public sealed record MetricTags(IReadOnlyDictionary<string, string> Values)
{
    /// <summary>
    /// 创建统一 API 错误响应对象
    /// </summary>
    /// <param name="serviceName">用于提供服务Name</param>
    /// <param name="tenantId">用于提供tenant标识</param>
    /// <param name="shardId">用于提供shard标识</param>
    /// <param name="operationName">用于提供操作Name</param>
    /// <returns>方法计算得到的文本值</returns>
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
