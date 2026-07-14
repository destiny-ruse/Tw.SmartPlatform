using AwesomeAssertions;
using Tw.EventBus;
using Xunit;

namespace Tw.EventBus.Tests;

/// <summary>
/// 验证默认事件发布器的分发、取消与失败语义
/// </summary>
public sealed class EventPublisherTests
{
    /// <summary>
    /// 发布器把同一事件实例与取消令牌精确分发到传输边界一次
    /// </summary>
    /// <returns>测试异步操作</returns>
    [Fact]
    public async Task PublishAsync_DelegatesOriginalEventAndCancellationTokenOnce()
    {
        var transport = new RecordingEventTransport();
        var publisher = new EventPublisher(transport);
        var integrationEvent = new SampleEvent("event-1");
        using var cancellationTokenSource = new CancellationTokenSource();

        await publisher.PublishAsync(integrationEvent, cancellationTokenSource.Token);

        transport.Invocations.Should().ContainSingle();
        transport.CallCount.Should().Be(1);
        var invocation = transport.Invocations.Single();
        invocation.IntegrationEvent.Should().BeSameAs(integrationEvent);
        invocation.CancellationToken.Should().Be(cancellationTokenSource.Token);
    }

    /// <summary>
    /// 空事件在传输边界交互前以公开参数名拒绝
    /// </summary>
    /// <returns>测试异步操作</returns>
    [Fact]
    public async Task PublishAsync_ThrowsArgumentNullException_WhenIntegrationEventIsNull()
    {
        var transport = new RecordingEventTransport();
        var publisher = new EventPublisher(transport);
        using var cancellationTokenSource = new CancellationTokenSource();

        var act = () => publisher.PublishAsync(null!, cancellationTokenSource.Token);

        var exception = await act.Should().ThrowAsync<ArgumentNullException>();
        exception.Which.Should().BeOfType<ArgumentNullException>();
        exception.Which.ParamName.Should().Be("integrationEvent");
        transport.Invocations.Should().BeEmpty();
        transport.CallCount.Should().Be(0);
    }

    /// <summary>
    /// 传输边界返回的取消异常实例与令牌保持不变且不会再次分发
    /// </summary>
    /// <returns>测试异步操作</returns>
    [Fact]
    public async Task PublishAsync_PropagatesOriginalCancellationExceptionOnce()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();
        var cancellation = new OperationCanceledException(cancellationTokenSource.Token);
        var transport = new RecordingEventTransport(cancellation);
        var publisher = new EventPublisher(transport);
        var integrationEvent = new SampleEvent("event-cancelled");

        var act = () => publisher.PublishAsync(integrationEvent, cancellationTokenSource.Token);

        var exception = await act.Should().ThrowAsync<OperationCanceledException>();
        exception.Which.Should().BeSameAs(cancellation);
        exception.Which.CancellationToken.Should().Be(cancellationTokenSource.Token);
        transport.Invocations.Should().ContainSingle();
        transport.CallCount.Should().Be(1);
        var invocation = transport.Invocations.Single();
        invocation.IntegrationEvent.Should().BeSameAs(integrationEvent);
        invocation.CancellationToken.Should().Be(cancellationTokenSource.Token);
    }

    /// <summary>
    /// 传输失败实例原样返回给发布调用方且不会再次分发
    /// </summary>
    /// <returns>测试异步操作</returns>
    [Fact]
    public async Task PublishAsync_PropagatesOriginalTransportFailureOnce()
    {
        var failure = new InvalidOperationException("传输写入失败");
        var transport = new RecordingEventTransport(failure);
        var publisher = new EventPublisher(transport);
        var integrationEvent = new SampleEvent("event-failed");
        using var cancellationTokenSource = new CancellationTokenSource();

        var act = () => publisher.PublishAsync(integrationEvent, cancellationTokenSource.Token);

        var exception = await act.Should().ThrowAsync<InvalidOperationException>();
        exception.Which.Should().BeSameAs(failure);
        transport.Invocations.Should().ContainSingle();
        transport.CallCount.Should().Be(1);
        var invocation = transport.Invocations.Single();
        invocation.IntegrationEvent.Should().BeSameAs(integrationEvent);
        invocation.CancellationToken.Should().Be(cancellationTokenSource.Token);
    }

    /// <summary>
    /// 提供发布器测试使用的集成事件
    /// </summary>
    /// <param name="EventId">事件唯一标识</param>
    private sealed record SampleEvent(string EventId) : IIntegrationEvent;

    /// <summary>
    /// 记录传输调用并返回预先指定的失败实例
    /// </summary>
    /// <param name="failure">需要由传输边界返回的指定失败实例</param>
    private sealed class RecordingEventTransport(Exception? failure = null) : IEventTransport
    {
        /// <summary>
        /// 传输边界收到的全部发布调用
        /// </summary>
        public List<TransportInvocation> Invocations { get; } = [];

        /// <summary>
        /// 传输边界收到的发布调用次数
        /// </summary>
        public int CallCount { get; private set; }

        /// <summary>
        /// 记录发布参数，并返回预先指定的传输结果
        /// </summary>
        /// <param name="integrationEvent">发布器传入的集成事件</param>
        /// <param name="cancellationToken">发布器透传的取消令牌</param>
        /// <returns>成功完成或携带指定失败实例的任务</returns>
        public Task PublishAsync(IIntegrationEvent integrationEvent, CancellationToken cancellationToken)
        {
            CallCount++;
            Invocations.Add(new TransportInvocation(integrationEvent, cancellationToken));

            return failure is null
                ? Task.CompletedTask
                : Task.FromException(failure);
        }
    }

    /// <summary>
    /// 关联一次传输调用使用的事件实例与取消令牌
    /// </summary>
    /// <param name="IntegrationEvent">传输边界收到的集成事件实例</param>
    /// <param name="CancellationToken">传输边界收到的取消令牌</param>
    private sealed record TransportInvocation(
        IIntegrationEvent IntegrationEvent,
        CancellationToken CancellationToken);
}
