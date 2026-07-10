namespace Tw.Gateway;

/// <summary>
/// 封装GatewayRateLimit策略相关的数据和行为
/// </summary>
public sealed record GatewayRateLimitPolicy(bool StrictGlobalLimit, bool GatewayLocalLimit);
