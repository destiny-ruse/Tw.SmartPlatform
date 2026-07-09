namespace Tw.Gateway;

public sealed record GatewayRateLimitPolicy(bool StrictGlobalLimit, bool GatewayLocalLimit);
