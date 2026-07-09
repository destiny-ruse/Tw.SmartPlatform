namespace Tw.Resilience;

public static class ResiliencePolicyBuilder
{
    public static ResiliencePolicy Build(ResiliencePolicyDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        return new ResiliencePolicy(
            RetryEnabled: descriptor.OperationKind != OperationKind.NonIdempotentWrite && descriptor.RetryCount > 0,
            descriptor.Timeout);
    }
}

public sealed record ResiliencePolicy(bool RetryEnabled, TimeSpan Timeout);
