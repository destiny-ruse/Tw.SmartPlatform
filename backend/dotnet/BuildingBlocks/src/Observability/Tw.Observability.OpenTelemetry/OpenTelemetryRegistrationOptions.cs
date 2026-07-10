namespace Tw.Observability.OpenTelemetry;

/// <summary>
/// 配置OpenTelemetryRegistration的运行行为
/// </summary>
public sealed record OpenTelemetryRegistrationOptions(bool EnableGrpcNetClientInstrumentation)
{
    /// <summary>
    /// new在当前对象中的业务含义
    /// </summary>
    public static OpenTelemetryRegistrationOptions Default { get; } = new(EnableGrpcNetClientInstrumentation: false);
}
