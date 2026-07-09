using AwesomeAssertions;
using Tw.EventBus.Abstractions;
using Tw.EventBus.Cap;
using Tw.EventBus.Cap.Outbox;
using Tw.Uow;
using Xunit;

namespace Tw.EventBus.Cap.Tests;

public sealed class CapEventTransportTests
{
    [Fact]
    public async Task PublishAsync_Throws_WhenCurrentUnitOfWorkIsMissing()
    {
        var transport = new CapEventTransport(new NullUnitOfWorkManager(), new RecordingOutboxWriter());

        var act = () => transport.PublishAsync(new SampleEvent("event-1"), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("CAP Outbox writes require the current unit of work transaction.");
    }

    [Fact]
    public async Task PublishAsync_Throws_WhenCurrentUnitOfWorkCannotCoverOutbox()
    {
        var transport = new CapEventTransport(
            new ActiveUnitOfWorkManager(new RecordingUnitOfWork(canWriteOutbox: false)),
            new RecordingOutboxWriter());

        var act = () => transport.PublishAsync(new SampleEvent("event-1"), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("The current unit of work cannot cover business writes and CAP Outbox writes.");
    }

    [Fact]
    public async Task PublishAsync_WritesOutboxThroughCurrentUnitOfWork()
    {
        var unitOfWork = new RecordingUnitOfWork(canWriteOutbox: true);
        var outboxWriter = new RecordingOutboxWriter();
        var transport = new CapEventTransport(new ActiveUnitOfWorkManager(unitOfWork), outboxWriter);
        var integrationEvent = new SampleEvent("event-2");

        await transport.PublishAsync(integrationEvent, CancellationToken.None);

        outboxWriter.Writes.Should().ContainSingle()
            .Which.Should().Be(new OutboxWrite(unitOfWork, integrationEvent));
    }

    private sealed record SampleEvent(string EventId) : IIntegrationEvent;

    private sealed class NullUnitOfWorkManager : IUnitOfWorkManager
    {
        public IUnitOfWork? Current => null;

        public Task<IUnitOfWork> BeginAsync(UnitOfWorkOptions options, CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("The missing-UoW test must not start a new CAP transaction.");
        }
    }

    private sealed class ActiveUnitOfWorkManager(IUnitOfWork current) : IUnitOfWorkManager
    {
        public IUnitOfWork? Current => current;

        public Task<IUnitOfWork> BeginAsync(UnitOfWorkOptions options, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(current);
        }
    }

    private sealed class RecordingUnitOfWork(bool canWriteOutbox) : IUnitOfWork, IOutboxTransactionBoundary
    {
        public CancellationToken CancellationToken => CancellationToken.None;

        public bool CanWriteOutbox => canWriteOutbox;

        public bool IsCompleted { get; private set; }

        public Task CommitAsync(CancellationToken cancellationToken = default)
        {
            IsCompleted = true;
            return Task.CompletedTask;
        }

        public Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            IsCompleted = true;
            return Task.CompletedTask;
        }

        public ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }
    }

    private sealed class RecordingOutboxWriter : IOutboxWriter
    {
        public List<OutboxWrite> Writes { get; } = [];

        public Task WriteAsync(IUnitOfWork unitOfWork, IIntegrationEvent integrationEvent, CancellationToken cancellationToken)
        {
            Writes.Add(new OutboxWrite(unitOfWork, integrationEvent));
            return Task.CompletedTask;
        }
    }

    private sealed record OutboxWrite(IUnitOfWork UnitOfWork, IIntegrationEvent IntegrationEvent);
}
