namespace Tw.Observability.OpenTelemetry;

public sealed record OpenTelemetryRegistrationOptions(bool EnableGrpcNetClientInstrumentation)
{
    public static OpenTelemetryRegistrationOptions Default { get; } = new(EnableGrpcNetClientInstrumentation: false);
}
