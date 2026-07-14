using AwesomeAssertions;
using Tw.EventBus;
using Xunit;

namespace Tw.EventBus.Tests;

/// <summary>
/// 验证集成事件元数据与默认发布器的分发语义
/// </summary>
public sealed class EventPublisherTests
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
    /// 发布器把同一事件与取消令牌精确分发到传输边界一次
    /// </summary>
    /// <returns>测试异步操作</returns>
    [Fact]
    public async Task PublishAsync_DelegatesToTransport()
    {
        var transport = new RecordingEventTransport();
        var publisher = new EventPublisher(transport);
        var integrationEvent = new SampleEvent("event-1");
        using var cancellationTokenSource = new CancellationTokenSource();

        await publisher.PublishAsync(integrationEvent, cancellationTokenSource.Token);

        transport.Invocations.Should().ContainSingle()
            .Which.Should().Be(new TransportInvocation(integrationEvent, cancellationTokenSource.Token));
    }

    /// <summary>
    /// 传输边界观察到调用方取消后，发布器保持取消结果且不触发额外交互
    /// </summary>
    /// <returns>测试异步操作</returns>
    [Fact]
    public async Task PublishAsync_PropagatesCancellation()
    {
        var transport = new RecordingEventTransport();
        var publisher = new EventPublisher(transport);
        var integrationEvent = new SampleEvent("event-cancelled");
        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        var act = () => publisher.PublishAsync(integrationEvent, cancellationTokenSource.Token);

        await act.Should().ThrowAsync<OperationCanceledException>()
            .Where(exception => exception.CancellationToken == cancellationTokenSource.Token);
        transport.Invocations.Should().ContainSingle()
            .Which.Should().Be(new TransportInvocation(integrationEvent, cancellationTokenSource.Token));
    }

    /// <summary>
    /// 传输失败原样返回给发布调用方且不会再次分发
    /// </summary>
    /// <returns>测试异步操作</returns>
    [Fact]
    public async Task PublishAsync_PropagatesTransportFailure()
    {
        var failure = new InvalidOperationException("传输写入失败");
        var transport = new RecordingEventTransport(failure);
        var publisher = new EventPublisher(transport);
        var integrationEvent = new SampleEvent("event-failed");
        using var cancellationTokenSource = new CancellationTokenSource();

        var act = () => publisher.PublishAsync(integrationEvent, cancellationTokenSource.Token);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .Where(exception => ReferenceEquals(exception, failure));
        transport.Invocations.Should().ContainSingle()
            .Which.Should().Be(new TransportInvocation(integrationEvent, cancellationTokenSource.Token));
    }

    /// <summary>
    /// 提供发布器测试使用的集成事件
    /// </summary>
    /// <param name="EventId">事件唯一标识</param>
    private sealed record SampleEvent(string EventId) : IIntegrationEvent;

    /// <summary>
    /// 记录传输调用并模拟取消或失败边界
    /// </summary>
    /// <param name="failure">需要由传输边界返回的指定失败</param>
    private sealed class RecordingEventTransport(Exception? failure = null) : IEventTransport
    {
        /// <summary>
        /// 传输边界收到的全部发布调用
        /// </summary>
        public List<TransportInvocation> Invocations { get; } = [];

        /// <summary>
        /// 记录发布参数，并模拟取消或传输失败
        /// </summary>
        /// <param name="integrationEvent">发布器传入的集成事件</param>
        /// <param name="cancellationToken">发布器透传的取消令牌</param>
        /// <returns>记录完成状态</returns>
        /// <exception cref="OperationCanceledException">调用方已请求取消</exception>
        public Task PublishAsync(IIntegrationEvent integrationEvent, CancellationToken cancellationToken)
        {
            Invocations.Add(new TransportInvocation(integrationEvent, cancellationToken));
            cancellationToken.ThrowIfCancellationRequested();

            if (failure is not null)
            {
                return Task.FromException(failure);
            }

            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// 关联一次传输调用使用的事件与取消令牌
    /// </summary>
    /// <param name="IntegrationEvent">传输边界收到的集成事件</param>
    /// <param name="CancellationToken">传输边界收到的取消令牌</param>
    private sealed record TransportInvocation(
        IIntegrationEvent IntegrationEvent,
        CancellationToken CancellationToken);
}
