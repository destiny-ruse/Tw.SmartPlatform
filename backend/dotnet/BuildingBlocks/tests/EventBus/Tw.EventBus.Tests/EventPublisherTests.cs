using AwesomeAssertions;
using Tw.EventBus;
using Tw.EventBus.Abstractions;
using Xunit;

namespace Tw.EventBus.Tests;

/// <summary>
/// 覆盖事件Publisher的核心行为和边界条件
/// </summary>
public sealed class EventPublisherTests
{
    /// <summary>
    /// 验证Publish异步Delegates到Transport
    /// </summary>
    /// <returns>表示异步流程完成状态的任务</returns>
    [Fact]
    public async Task PublishAsync_DelegatesToTransport()
    {
        var transport = new RecordingEventTransport();
        var publisher = new EventPublisher(transport);
        var integrationEvent = new SampleEvent("event-1");

        await publisher.PublishAsync(integrationEvent, CancellationToken.None);

        transport.Published.Should().ContainSingle().Which.Should().Be(integrationEvent);
    }

    /// <summary>
    /// 封装示例事件相关的数据和行为
    /// </summary>
    private sealed record SampleEvent(string EventId) : IIntegrationEvent;

    /// <summary>
    /// 覆盖Recording事件Transport的核心行为和边界条件
    /// </summary>
    private sealed class RecordingEventTransport : IEventTransport
    {
        /// <summary>
        /// Published在当前对象中的业务含义
        /// </summary>
        public List<IIntegrationEvent> Published { get; } = [];

        /// <summary>
        /// 发布集成事件到测试事件总线
        /// </summary>
        /// <param name="integrationEvent">用于提供ntegrationEvent</param>
        /// <param name="cancellationToken">用于传播调用方取消请求的令牌</param>
        /// <returns>表示异步流程完成状态的任务</returns>
        public Task PublishAsync(IIntegrationEvent integrationEvent, CancellationToken cancellationToken)
        {
            Published.Add(integrationEvent);
            return Task.CompletedTask;
        }
    }
}
