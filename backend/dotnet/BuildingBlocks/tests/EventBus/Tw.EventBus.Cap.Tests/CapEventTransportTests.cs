using AwesomeAssertions;
using Tw.EventBus.Abstractions;
using Tw.EventBus.Cap;
using Tw.EventBus.Cap.Outbox;
using Tw.Uow;
using Xunit;

namespace Tw.EventBus.Cap.Tests;

/// <summary>
/// 覆盖Cap事件Transport的核心行为和边界条件
/// </summary>
public sealed class CapEventTransportTests
{
    /// <summary>
    /// 验证Publish异步抛出异常当CurrentUnitOfWorkIs缺少
    /// </summary>
    /// <returns>表示异步流程完成状态的任务</returns>
    [Fact]
    public async Task PublishAsync_Throws_WhenCurrentUnitOfWorkIsMissing()
    {
        var transport = new CapEventTransport(new NullUnitOfWorkManager(), new RecordingOutboxWriter());

        var act = () => transport.PublishAsync(new SampleEvent("event-1"), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("CAP Outbox writes require the current unit of work transaction.");
    }

    /// <summary>
    /// 验证Publish异步抛出异常当CurrentUnitOfWorkCannotCoverOutbox
    /// </summary>
    /// <returns>表示异步流程完成状态的任务</returns>
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

    /// <summary>
    /// 验证Publish异步写回OutboxThroughCurrentUnitOfWork
    /// </summary>
    /// <returns>表示异步流程完成状态的任务</returns>
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

    /// <summary>
    /// 封装示例事件相关的数据和行为
    /// </summary>
    private sealed record SampleEvent(string EventId) : IIntegrationEvent;

    /// <summary>
    /// 覆盖空值UnitOfWorkManager的核心行为和边界条件
    /// </summary>
    private sealed class NullUnitOfWorkManager : IUnitOfWorkManager
    {
        /// <summary>
        /// Current在当前对象中的业务含义
        /// </summary>
        public IUnitOfWork? Current => null;

        /// <summary>
        /// 开始测试事务并返回事务上下文
        /// </summary>
        /// <param name="options">用于配置当前组件行为的选项</param>
        /// <param name="cancellationToken">用于传播调用方取消请求的令牌</param>
        /// <returns>异步流程完成后产生的IUnitOfWork</returns>
        public Task<IUnitOfWork> BeginAsync(UnitOfWorkOptions options, CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("The missing-UoW test must not start a new CAP transaction.");
        }
    }

    /// <summary>
    /// 覆盖ActiveUnitOfWorkManager的核心行为和边界条件
    /// </summary>
    private sealed class ActiveUnitOfWorkManager(IUnitOfWork current) : IUnitOfWorkManager
    {
        /// <summary>
        /// Current在当前对象中的业务含义
        /// </summary>
        public IUnitOfWork? Current => current;

        /// <summary>
        /// 开始测试事务并返回事务上下文
        /// </summary>
        /// <param name="options">用于配置当前组件行为的选项</param>
        /// <param name="cancellationToken">用于传播调用方取消请求的令牌</param>
        /// <returns>异步流程完成后产生的IUnitOfWork</returns>
        public Task<IUnitOfWork> BeginAsync(UnitOfWorkOptions options, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(current);
        }
    }

    /// <summary>
    /// 覆盖RecordingUnitOfWork的核心行为和边界条件
    /// </summary>
    private sealed class RecordingUnitOfWork(bool canWriteOutbox) : IUnitOfWork, IOutboxTransactionBoundary
    {
        /// <summary>
        /// Cancellation令牌在当前对象中的业务含义
        /// </summary>
        public CancellationToken CancellationToken => CancellationToken.None;

        /// <summary>
        /// CanWriteOutbox在当前对象中的业务含义
        /// </summary>
        public bool CanWriteOutbox => canWriteOutbox;

        /// <summary>
        /// sCompleted在当前对象中的业务含义
        /// </summary>
        public bool IsCompleted { get; private set; }

        /// <summary>
        /// 提交测试事务上下文
        /// </summary>
        /// <param name="cancellationToken">用于传播调用方取消请求的令牌</param>
        /// <returns>表示异步流程完成状态的任务</returns>
        public Task CommitAsync(CancellationToken cancellationToken = default)
        {
            IsCompleted = true;
            return Task.CompletedTask;
        }

        /// <summary>
        /// 回滚测试事务上下文
        /// </summary>
        /// <param name="cancellationToken">用于传播调用方取消请求的令牌</param>
        /// <returns>表示异步流程完成状态的任务</returns>
        public Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            IsCompleted = true;
            return Task.CompletedTask;
        }

        /// <summary>
        /// 释放测试事务上下文
        /// </summary>
        /// <returns>表示异步流程完成状态的任务</returns>
        public ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }
    }

    /// <summary>
    /// 覆盖RecordingOutboxWriter的核心行为和边界条件
    /// </summary>
    private sealed class RecordingOutboxWriter : IOutboxWriter
    {
        /// <summary>
        /// 写回在当前对象中的业务含义
        /// </summary>
        public List<OutboxWrite> Writes { get; } = [];

        /// <summary>
        /// 写入待发送或待持久化的测试消息
        /// </summary>
        /// <param name="unitOfWork">用于提供unitOfWork</param>
        /// <param name="integrationEvent">用于提供ntegrationEvent</param>
        /// <param name="cancellationToken">用于传播调用方取消请求的令牌</param>
        /// <returns>表示异步流程完成状态的任务</returns>
        public Task WriteAsync(IUnitOfWork unitOfWork, IIntegrationEvent integrationEvent, CancellationToken cancellationToken)
        {
            Writes.Add(new OutboxWrite(unitOfWork, integrationEvent));
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// 封装OutboxWrite相关的数据和行为
    /// </summary>
    private sealed record OutboxWrite(IUnitOfWork UnitOfWork, IIntegrationEvent IntegrationEvent);
}
