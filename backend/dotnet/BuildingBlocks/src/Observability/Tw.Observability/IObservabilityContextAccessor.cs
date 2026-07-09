namespace Tw.Observability;

/// <summary>定义 IObservabilityContextAccessor 契约</summary>
public interface IObservabilityContextAccessor
{
    /// <summary>表示 Current 属性</summary>
    CorrelationContext Current { get; }
}
