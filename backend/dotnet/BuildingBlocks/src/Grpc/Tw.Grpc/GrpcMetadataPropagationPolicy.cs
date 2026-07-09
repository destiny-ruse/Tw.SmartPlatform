namespace Tw.Grpc;

public static class GrpcMetadataPropagationPolicy
{
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
