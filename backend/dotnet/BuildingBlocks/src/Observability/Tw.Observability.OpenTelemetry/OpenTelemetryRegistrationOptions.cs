namespace Tw.Observability.OpenTelemetry;

/// <summary>表示 OpenTelemetryRegistrationOptions 声明</summary>
public sealed record OpenTelemetryRegistrationOptions(bool EnableGrpcNetClientInstrumentation)
{
    /// <summary>表示 Default 属性</summary>
    public static OpenTelemetryRegistrationOptions Default { get; } = new(EnableGrpcNetClientInstrumentation: false);
}
