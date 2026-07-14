using AwesomeAssertions;
using Tw.Data.Uow;
using Tw.EventBus.Abstractions;
using Tw.EventBus.Cap;
using Tw.EventBus.Cap.Outbox;
using Xunit;

namespace Tw.EventBus.Cap.Tests;

/// <summary>
/// 验证 CAP 事件传输对当前工作单元事务边界的约束
/// </summary>
public sealed class CapEventTransportTests
{
    /// <summary>
    /// 当前不存在工作单元时拒绝写入 Outbox
    /// </summary>
    /// <returns>测试异步操作</returns>
    [Fact]
    public async Task PublishAsync_Throws_WhenCurrentUnitOfWorkIsMissing()
    {
        var transport = new CapEventTransport(new NullUnitOfWorkCoordinator(), new RecordingOutboxWriter());

        var act = () => transport.PublishAsync(new SampleEvent("event-1"), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("CAP Outbox 写入要求当前存在活动工作单元事务。");
    }

    /// <summary>
    /// 当前工作单元无法覆盖 Outbox 时拒绝发布
    /// </summary>
    /// <returns>测试异步操作</returns>
    [Fact]
    public async Task PublishAsync_Throws_WhenCurrentUnitOfWorkCannotCoverOutbox()
    {
        var transport = new CapEventTransport(
            new ActiveUnitOfWorkCoordinator(new RecordingUnitOfWork(canWriteOutbox: false)),
            new RecordingOutboxWriter());

        var act = () => transport.PublishAsync(new SampleEvent("event-1"), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("当前工作单元无法同时覆盖业务写入与 CAP Outbox 写入。");
    }

    /// <summary>
    /// 当前工作单元允许 Outbox 时通过同一事务边界写入事件
    /// </summary>
    /// <returns>测试异步操作</returns>
    [Fact]
    public async Task PublishAsync_WritesOutboxThroughCurrentUnitOfWork()
    {
        var unitOfWork = new RecordingUnitOfWork(canWriteOutbox: true);
        var outboxWriter = new RecordingOutboxWriter();
        var transport = new CapEventTransport(new ActiveUnitOfWorkCoordinator(unitOfWork), outboxWriter);
        var integrationEvent = new SampleEvent("event-2");

        await transport.PublishAsync(integrationEvent, CancellationToken.None);

        outboxWriter.Writes.Should().ContainSingle()
            .Which.Should().Be(new OutboxWrite(unitOfWork, integrationEvent));
    }

    /// <summary>
    /// 提供传输测试使用的集成事件
    /// </summary>
    /// <param name="EventId">事件唯一标识</param>
    private sealed record SampleEvent(string EventId) : IIntegrationEvent;

    /// <summary>
    /// 表示当前没有活动工作单元的测试协调器
    /// </summary>
    private sealed class NullUnitOfWorkCoordinator : IUnitOfWorkCoordinator
    {
        /// <summary>
        /// 当前活动工作单元为空
        /// </summary>
        public IUnitOfWork? Current => null;

        /// <summary>
        /// 阻止缺少工作单元的场景意外创建新事务
        /// </summary>
        /// <param name="options">未使用的工作单元选项</param>
        /// <param name="cancellationToken">未使用的取消令牌</param>
        /// <returns>此测试替身不会返回工作单元</returns>
        /// <exception cref="InvalidOperationException">测试流程意外尝试创建工作单元</exception>
        public Task<IUnitOfWork> BeginAsync(
            UnitOfWorkOptions options,
            CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("缺少工作单元的测试不得创建新的 CAP 事务。");
        }
    }

    /// <summary>
    /// 公开指定活动工作单元的测试协调器
    /// </summary>
    /// <param name="current">需要公开给传输组件的工作单元</param>
    private sealed class ActiveUnitOfWorkCoordinator(IUnitOfWork current) : IUnitOfWorkCoordinator
    {
        /// <summary>
        /// 测试场景指定的活动工作单元
        /// </summary>
        public IUnitOfWork? Current => current;

        /// <summary>
        /// 返回测试场景指定的活动工作单元
        /// </summary>
        /// <param name="options">未使用的工作单元选项</param>
        /// <param name="cancellationToken">未使用的取消令牌</param>
        /// <returns>测试场景指定的工作单元</returns>
        public Task<IUnitOfWork> BeginAsync(
            UnitOfWorkOptions options,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(current);
        }
    }

    /// <summary>
    /// 记录事务完成状态并声明 Outbox 写入资格的测试工作单元
    /// </summary>
    /// <param name="canWriteOutbox">当前事务边界是否允许写入 Outbox</param>
    private sealed class RecordingUnitOfWork(bool canWriteOutbox) : IUnitOfWork, IOutboxTransactionBoundary
    {
        /// <summary>
        /// 测试工作单元不关联取消请求
        /// </summary>
        public CancellationToken CancellationToken => CancellationToken.None;

        /// <summary>
        /// 当前事务边界是否允许写入 Outbox
        /// </summary>
        public bool CanWriteOutbox => canWriteOutbox;

        /// <summary>
        /// 当前事务边界是否已经提交或回滚
        /// </summary>
        public bool IsCompleted { get; private set; }

        /// <summary>
        /// 将测试事务边界标记为完成
        /// </summary>
        /// <param name="cancellationToken">未使用的取消令牌</param>
        /// <returns>提交完成任务</returns>
        public Task CommitAsync(CancellationToken cancellationToken = default)
        {
            IsCompleted = true;
            return Task.CompletedTask;
        }

        /// <summary>
        /// 将测试事务边界标记为完成
        /// </summary>
        /// <param name="cancellationToken">未使用的取消令牌</param>
        /// <returns>回滚完成任务</returns>
        public Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            IsCompleted = true;
            return Task.CompletedTask;
        }

        /// <summary>
        /// 完成测试工作单元的异步释放
        /// </summary>
        /// <returns>释放完成状态</returns>
        public ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }
    }

    /// <summary>
    /// 记录通过当前工作单元写入的集成事件
    /// </summary>
    private sealed class RecordingOutboxWriter : IOutboxWriter
    {
        /// <summary>
        /// 已接收的 Outbox 写入记录
        /// </summary>
        public List<OutboxWrite> Writes { get; } = [];

        /// <summary>
        /// 保存工作单元与集成事件的关联记录
        /// </summary>
        /// <param name="unitOfWork">覆盖当前 Outbox 写入的工作单元</param>
        /// <param name="integrationEvent">当前写入的集成事件</param>
        /// <param name="cancellationToken">未使用的取消令牌</param>
        /// <returns>记录完成任务</returns>
        public Task WriteAsync(
            IUnitOfWork unitOfWork,
            IIntegrationEvent integrationEvent,
            CancellationToken cancellationToken)
        {
            Writes.Add(new OutboxWrite(unitOfWork, integrationEvent));
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// 关联一次 Outbox 写入使用的工作单元和集成事件
    /// </summary>
    /// <param name="UnitOfWork">覆盖 Outbox 写入的工作单元</param>
    /// <param name="IntegrationEvent">写入的集成事件</param>
    private sealed record OutboxWrite(IUnitOfWork UnitOfWork, IIntegrationEvent IntegrationEvent);
}
