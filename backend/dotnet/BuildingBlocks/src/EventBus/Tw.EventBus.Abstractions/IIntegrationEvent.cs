namespace Tw.EventBus.Abstractions;

/// <summary>
/// 定义Integration事件的能力边界
/// </summary>
public interface IIntegrationEvent
{
    /// <summary>
    /// 事件标识在当前对象中的业务含义
    /// </summary>
    string EventId { get; }
}
