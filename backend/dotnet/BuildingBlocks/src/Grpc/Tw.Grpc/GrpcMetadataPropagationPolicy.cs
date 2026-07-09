namespace Tw.Grpc;

/// <summary>表示 GrpcMetadataPropagationPolicy 类型</summary>
public static class GrpcMetadataPropagationPolicy
{
    /// <summary>表示 AllowedMetadata 属性</summary>
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
