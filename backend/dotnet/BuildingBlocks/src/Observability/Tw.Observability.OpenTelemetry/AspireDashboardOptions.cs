namespace Tw.Observability.OpenTelemetry;

/// <summary>
/// 配置AspireDashboard的运行行为
/// </summary>
public sealed record AspireDashboardOptions(string? OtlpEndpoint);
