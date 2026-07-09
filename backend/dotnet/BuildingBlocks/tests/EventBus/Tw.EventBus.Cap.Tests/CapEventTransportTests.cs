using AwesomeAssertions;
using Tw.EventBus.Abstractions;
using Tw.EventBus.Cap;
using Tw.EventBus.Cap.Outbox;
using Tw.Uow;
using Xunit;

namespace Tw.EventBus.Cap.Tests;

/// <summary>验证 CapEventTransportTests 相关行为</summary>
public sealed class CapEventTransportTests
{
    /// <summary>验证 PublishAsync_Throws_WhenCurrentUnitOfWorkIsMissing 场景</summary>
    /// <returns>PublishAsync_Throws_WhenCurrentUnitOfWorkIsMissing 的执行结果</returns>
    [Fact]
    public async Task PublishAsync_Throws_WhenCurrentUnitOfWorkIsMissing()
    {
        var transport = new CapEventTransport(new NullUnitOfWorkManager(), new RecordingOutboxWriter());

        var act = () => transport.PublishAsync(new SampleEvent("event-1"), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("CAP Outbox writes require the current unit of work transaction.");
    }

    /// <summary>验证 PublishAsync_Throws_WhenCurrentUnitOfWorkCannotCoverOutbox 场景</summary>
    /// <returns>PublishAsync_Throws_WhenCurrentUnitOfWorkCannotCoverOutbox 的执行结果</returns>
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

    /// <summary>验证 PublishAsync_WritesOutboxThroughCurrentUnitOfWork 场景</summary>
    /// <returns>PublishAsync_WritesOutboxThroughCurrentUnitOfWork 的执行结果</returns>
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

    /// <summary>表示 SampleEvent 声明</summary>
    private sealed record SampleEvent(string EventId) : IIntegrationEvent;

    /// <summary>验证 NullUnitOfWorkManager 相关行为</summary>
    private sealed class NullUnitOfWorkManager : IUnitOfWorkManager
    {
        /// <summary>表示 Current 属性</summary>
        public IUnitOfWork? Current => null;

        /// <summary>验证 BeginAsync 场景</summary>
        /// <param name="options">options 参数</param>
        /// <param name="cancellationToken">cancellationToken 参数</param>
        /// <returns>BeginAsync 的执行结果</returns>
        public Task<IUnitOfWork> BeginAsync(UnitOfWorkOptions options, CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("The missing-UoW test must not start a new CAP transaction.");
        }
    }

    /// <summary>验证 ActiveUnitOfWorkManager 相关行为</summary>
    private sealed class ActiveUnitOfWorkManager(IUnitOfWork current) : IUnitOfWorkManager
    {
        /// <summary>表示 Current 属性</summary>
        public IUnitOfWork? Current => current;

        /// <summary>验证 BeginAsync 场景</summary>
        /// <param name="options">options 参数</param>
        /// <param name="cancellationToken">cancellationToken 参数</param>
        /// <returns>BeginAsync 的执行结果</returns>
        public Task<IUnitOfWork> BeginAsync(UnitOfWorkOptions options, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(current);
        }
    }

    /// <summary>验证 RecordingUnitOfWork 相关行为</summary>
    private sealed class RecordingUnitOfWork(bool canWriteOutbox) : IUnitOfWork, IOutboxTransactionBoundary
    {
        /// <summary>表示 CancellationToken 属性</summary>
        public CancellationToken CancellationToken => CancellationToken.None;

        /// <summary>表示 CanWriteOutbox 属性</summary>
        public bool CanWriteOutbox => canWriteOutbox;

        /// <summary>表示 IsCompleted 属性</summary>
        public bool IsCompleted { get; private set; }

        /// <summary>验证 CommitAsync 场景</summary>
        /// <param name="cancellationToken">cancellationToken 参数</param>
        /// <returns>CommitAsync 的执行结果</returns>
        public Task CommitAsync(CancellationToken cancellationToken = default)
        {
            IsCompleted = true;
            return Task.CompletedTask;
        }

        /// <summary>验证 RollbackAsync 场景</summary>
        /// <param name="cancellationToken">cancellationToken 参数</param>
        /// <returns>RollbackAsync 的执行结果</returns>
        public Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            IsCompleted = true;
            return Task.CompletedTask;
        }

        /// <summary>验证 DisposeAsync 场景</summary>
        /// <returns>DisposeAsync 的执行结果</returns>
        public ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }
    }

    /// <summary>验证 RecordingOutboxWriter 相关行为</summary>
    private sealed class RecordingOutboxWriter : IOutboxWriter
    {
        /// <summary>表示 Writes 属性</summary>
        public List<OutboxWrite> Writes { get; } = [];

        /// <summary>验证 WriteAsync 场景</summary>
        /// <param name="unitOfWork">unitOfWork 参数</param>
        /// <param name="integrationEvent">integrationEvent 参数</param>
        /// <param name="cancellationToken">cancellationToken 参数</param>
        /// <returns>WriteAsync 的执行结果</returns>
        public Task WriteAsync(IUnitOfWork unitOfWork, IIntegrationEvent integrationEvent, CancellationToken cancellationToken)
        {
            Writes.Add(new OutboxWrite(unitOfWork, integrationEvent));
            return Task.CompletedTask;
        }
    }

    /// <summary>表示 OutboxWrite 声明</summary>
    private sealed record OutboxWrite(IUnitOfWork UnitOfWork, IIntegrationEvent IntegrationEvent);
}
