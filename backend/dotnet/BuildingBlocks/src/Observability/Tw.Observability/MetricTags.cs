namespace Tw.Observability;

public sealed record MetricTags(IReadOnlyDictionary<string, string> Values)
{
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
