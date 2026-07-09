namespace Tw.Observability;

public interface IObservabilityContextAccessor
{
    CorrelationContext Current { get; }
}
