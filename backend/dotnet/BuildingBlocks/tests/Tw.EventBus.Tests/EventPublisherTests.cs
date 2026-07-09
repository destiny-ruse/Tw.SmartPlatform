using AwesomeAssertions;
using Tw.EventBus;
using Tw.EventBus.Abstractions;
using Xunit;

namespace Tw.EventBus.Tests;

public sealed class EventPublisherTests
{
    [Fact]
    public async Task PublishAsync_DelegatesToTransport()
    {
        var transport = new RecordingEventTransport();
        var publisher = new EventPublisher(transport);
        var integrationEvent = new SampleEvent("event-1");

        await publisher.PublishAsync(integrationEvent, CancellationToken.None);

        transport.Published.Should().ContainSingle().Which.Should().Be(integrationEvent);
    }

    private sealed record SampleEvent(string EventId) : IIntegrationEvent;

    private sealed class RecordingEventTransport : IEventTransport
    {
        public List<IIntegrationEvent> Published { get; } = [];

        public Task PublishAsync(IIntegrationEvent integrationEvent, CancellationToken cancellationToken)
        {
            Published.Add(integrationEvent);
            return Task.CompletedTask;
        }
    }
}
