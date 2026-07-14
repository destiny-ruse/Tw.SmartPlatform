using AwesomeAssertions;
using Tw.EventBus;
using Xunit;

namespace Tw.EventBus.Tests;

/// <summary>
/// 验证集成事件公开元数据契约
/// </summary>
public sealed class EventContractTests
{
    /// <summary>
    /// 事件元数据契约公开稳定且只读的事件标识
    /// </summary>
    [Fact]
    public void IntegrationEvent_ExposesReadOnlyEventIdMetadata()
    {
        IIntegrationEvent integrationEvent = new SampleEvent("event-metadata");
        var eventIdProperty = typeof(IIntegrationEvent).GetProperty(nameof(IIntegrationEvent.EventId));

        integrationEvent.EventId.Should().Be("event-metadata");
        eventIdProperty.Should().NotBeNull();
        eventIdProperty!.PropertyType.Should().Be(typeof(string));
        eventIdProperty.CanRead.Should().BeTrue();
        eventIdProperty.CanWrite.Should().BeFalse();
    }

    /// <summary>
    /// 提供事件元数据契约测试使用的集成事件
    /// </summary>
    /// <param name="EventId">事件唯一标识</param>
    private sealed record SampleEvent(string EventId) : IIntegrationEvent;
}
