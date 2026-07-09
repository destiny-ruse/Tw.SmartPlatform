using AwesomeAssertions;
using Tw.EventBus;
using Tw.EventBus.Abstractions;
using Xunit;

namespace Tw.EventBus.Tests;

/// <summary>验证 EventPublisherTests 相关行为</summary>
public sealed class EventPublisherTests
{
    /// <summary>验证 PublishAsync_DelegatesToTransport 场景</summary>
    /// <returns>PublishAsync_DelegatesToTransport 的执行结果</returns>
    [Fact]
    public async Task PublishAsync_DelegatesToTransport()
    {
        var transport = new RecordingEventTransport();
        var publisher = new EventPublisher(transport);
        var integrationEvent = new SampleEvent("event-1");

        await publisher.PublishAsync(integrationEvent, CancellationToken.None);

        transport.Published.Should().ContainSingle().Which.Should().Be(integrationEvent);
    }

    /// <summary>表示 SampleEvent 声明</summary>
    private sealed record SampleEvent(string EventId) : IIntegrationEvent;

    /// <summary>验证 RecordingEventTransport 相关行为</summary>
    private sealed class RecordingEventTransport : IEventTransport
    {
        /// <summary>表示 Published 属性</summary>
        public List<IIntegrationEvent> Published { get; } = [];

        /// <summary>验证 PublishAsync 场景</summary>
        /// <param name="integrationEvent">integrationEvent 参数</param>
        /// <param name="cancellationToken">cancellationToken 参数</param>
        /// <returns>PublishAsync 的执行结果</returns>
        public Task PublishAsync(IIntegrationEvent integrationEvent, CancellationToken cancellationToken)
        {
            Published.Add(integrationEvent);
            return Task.CompletedTask;
        }
    }
}
