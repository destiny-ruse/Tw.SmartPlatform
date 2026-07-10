namespace Tw.Observability;

/// <summary>
/// 定义Observability上下文Accessor的能力边界
/// </summary>
public interface IObservabilityContextAccessor
{
    /// <summary>
    /// Current在当前对象中的业务含义
    /// </summary>
    CorrelationContext Current { get; }
}
