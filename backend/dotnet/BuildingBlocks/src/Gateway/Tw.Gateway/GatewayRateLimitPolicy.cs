namespace Tw.Gateway;

/// <summary>表示 GatewayRateLimitPolicy 声明</summary>
public sealed record GatewayRateLimitPolicy(bool StrictGlobalLimit, bool GatewayLocalLimit);
