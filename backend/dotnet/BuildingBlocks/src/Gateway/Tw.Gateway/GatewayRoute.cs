namespace Tw.Gateway;

public sealed record GatewayRoute(
    string RouteId,
    string ClusterId,
    string Path,
    IReadOnlyList<string> Methods,
    string Destination,
    string ServiceDiscoveryName,
    int Weight,
    TimeSpan Timeout,
    string RetryPolicy,
    GatewayRateLimitPolicy RateLimitPolicy,
    bool WebSocketPassThrough,
    bool SsePassThrough,
    bool GrpcPassThrough,
    string TrustedTenantSource);
