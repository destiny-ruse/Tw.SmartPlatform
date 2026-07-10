namespace Tw.Grpc;

/// <summary>
/// 封装GrpcMetadataPropagation策略相关的数据和行为
/// </summary>
public static class GrpcMetadataPropagationPolicy
{
    /// <summary>
    /// Hash写入在当前对象中的业务含义
    /// </summary>
    public static IReadOnlySet<string> AllowedMetadata { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "traceparent",
        "tracestate",
        "correlation-id",
        "tenant-id",
        "culture",
        "authorization"
    };
}
