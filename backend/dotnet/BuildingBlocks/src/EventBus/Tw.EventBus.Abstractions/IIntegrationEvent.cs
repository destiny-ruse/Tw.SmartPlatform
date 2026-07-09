namespace Tw.EventBus.Abstractions;

/// <summary>定义 IIntegrationEvent 契约</summary>
public interface IIntegrationEvent
{
    /// <summary>表示 EventId 属性</summary>
    string EventId { get; }
}
