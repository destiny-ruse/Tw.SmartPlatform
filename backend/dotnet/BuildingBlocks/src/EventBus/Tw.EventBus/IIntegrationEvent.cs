namespace Tw.EventBus;

/// <summary>
/// 标识可由 provider-neutral 事件总线发布的集成事件
/// </summary>
public interface IIntegrationEvent
{
    /// <summary>
    /// 用于追踪、去重与诊断的事件唯一标识
    /// </summary>
    string EventId { get; }
}
