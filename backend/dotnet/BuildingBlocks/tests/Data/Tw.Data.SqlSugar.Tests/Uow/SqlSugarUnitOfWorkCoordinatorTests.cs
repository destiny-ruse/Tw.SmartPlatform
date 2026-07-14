using AwesomeAssertions;
using Tw.Data.SqlSugar.Connection;
using Tw.Data.SqlSugar.Uow;
using Tw.Data.Uow;
using Xunit;

namespace Tw.Data.SqlSugar.Tests.Uow;

/// <summary>
/// 验证 SqlSugar 工作单元协调器的作用域与事务边界行为
/// </summary>
public sealed class SqlSugarUnitOfWorkCoordinatorTests
{
    /// <summary>
    /// 创建工作单元后将其暴露为当前作用域
    /// </summary>
    /// <returns>测试异步操作</returns>
    [Fact]
    public async Task BeginAsync_SetsCurrentUnitOfWork()
    {
        var coordinator = new SqlSugarUnitOfWorkCoordinator(new FakeSqlSugarClientFactory());

        await using var unitOfWork = await coordinator.BeginAsync(
            UnitOfWorkOptions.Default,
            TestContext.Current.CancellationToken);

        coordinator.Current.Should().BeSameAs(unitOfWork);
    }

    /// <summary>
    /// 释放工作单元后恢复先前的当前作用域
    /// </summary>
    /// <returns>测试异步操作</returns>
    [Fact]
    public async Task DisposeAsync_ClearsCurrentUnitOfWork()
    {
        var coordinator = new SqlSugarUnitOfWorkCoordinator(new FakeSqlSugarClientFactory());

        var unitOfWork = await coordinator.BeginAsync(
            UnitOfWorkOptions.Default,
            TestContext.Current.CancellationToken);
        await unitOfWork.DisposeAsync();

        coordinator.Current.Should().BeNull();
        unitOfWork.Should().BeAssignableTo<IOutboxTransactionBoundary>()
            .Subject.CanWriteOutbox.Should().BeFalse();
    }

    /// <summary>
    /// 提交工作单元后将 Outbox 事务边界标记为完成
    /// </summary>
    /// <returns>测试异步操作</returns>
    [Fact]
    public async Task CommitAsync_MarksOutboxBoundaryAsCompleted()
    {
        var coordinator = new SqlSugarUnitOfWorkCoordinator(new FakeSqlSugarClientFactory());

        await using var unitOfWork = await coordinator.BeginAsync(
            UnitOfWorkOptions.Default,
            TestContext.Current.CancellationToken);
        await unitOfWork.CommitAsync(TestContext.Current.CancellationToken);

        var boundary = unitOfWork.Should().BeAssignableTo<IOutboxTransactionBoundary>().Subject;
        boundary.CanWriteOutbox.Should().BeFalse();
        boundary.IsCompleted.Should().BeTrue();
    }

    /// <summary>
    /// 回滚工作单元后关闭 Outbox 边界并保留回滚完成状态
    /// </summary>
    /// <returns>测试异步操作</returns>
    [Fact]
    public async Task RollbackAsync_ClosesOutboxBoundaryAndMarksRollback()
    {
        var coordinator = new SqlSugarUnitOfWorkCoordinator(new FakeSqlSugarClientFactory());

        await using var unitOfWork = await coordinator.BeginAsync(
            UnitOfWorkOptions.Default,
            TestContext.Current.CancellationToken);
        await unitOfWork.RollbackAsync(TestContext.Current.CancellationToken);

        var sqlSugarUnitOfWork = unitOfWork.Should().BeOfType<SqlSugarUnitOfWork>().Subject;
        sqlSugarUnitOfWork.CanWriteOutbox.Should().BeFalse();
        sqlSugarUnitOfWork.IsCompleted.Should().BeTrue();
        sqlSugarUnitOfWork.IsRolledBack.Should().BeTrue();
    }

    /// <summary>
    /// 预取消请求不得创建工作单元或改变当前作用域
    /// </summary>
    /// <returns>测试异步操作</returns>
    [Fact]
    public async Task BeginAsync_ThrowsBeforeCreatingUnitOfWork_WhenCancellationIsRequested()
    {
        var clientFactory = new FakeSqlSugarClientFactory();
        var coordinator = new SqlSugarUnitOfWorkCoordinator(clientFactory);
        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        var act = () => coordinator.BeginAsync(UnitOfWorkOptions.Default, cancellationTokenSource.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
        clientFactory.CreateCallCount.Should().Be(0);
        coordinator.Current.Should().BeNull();
    }

    /// <summary>
    /// 已存在 Required 作用域时预取消请求不得返回复用实例或改变当前作用域
    /// </summary>
    /// <returns>测试异步操作</returns>
    [Fact]
    public async Task BeginAsync_ThrowsBeforeReusingRequiredUnitOfWork_WhenCancellationIsRequested()
    {
        var clientFactory = new FakeSqlSugarClientFactory();
        var coordinator = new SqlSugarUnitOfWorkCoordinator(clientFactory);
        await using var original = await coordinator.BeginAsync(
            UnitOfWorkOptions.Default,
            TestContext.Current.CancellationToken);
        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        var act = () => coordinator.BeginAsync(UnitOfWorkOptions.Default, cancellationTokenSource.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
        clientFactory.CreateCallCount.Should().Be(1);
        coordinator.Current.Should().BeSameAs(original);
    }

    /// <summary>
    /// Required 作用域复用当前工作单元且不创建额外客户端
    /// </summary>
    /// <returns>测试异步操作</returns>
    [Fact]
    public async Task BeginAsync_ReusesCurrentUnitOfWork_ForRequiredScope()
    {
        var clientFactory = new FakeSqlSugarClientFactory();
        var coordinator = new SqlSugarUnitOfWorkCoordinator(clientFactory);
        await using var original = await coordinator.BeginAsync(
            UnitOfWorkOptions.Default,
            TestContext.Current.CancellationToken);

        var reused = await coordinator.BeginAsync(
            UnitOfWorkOptions.Default,
            TestContext.Current.CancellationToken);

        reused.Should().BeSameAs(original);
        clientFactory.CreateCallCount.Should().Be(1);
        coordinator.Current.Should().BeSameAs(original);
    }

    /// <summary>
    /// RequiresNew 内层作用域释放后恢复外层工作单元
    /// </summary>
    /// <returns>测试异步操作</returns>
    [Fact]
    public async Task DisposeAsync_RestoresOuterUnitOfWork_ForRequiresNewScope()
    {
        var clientFactory = new FakeSqlSugarClientFactory();
        var coordinator = new SqlSugarUnitOfWorkCoordinator(clientFactory);
        await using var outer = await coordinator.BeginAsync(
            UnitOfWorkOptions.Default,
            TestContext.Current.CancellationToken);
        var options = new UnitOfWorkOptions(
            UnitOfWorkScope.RequiresNew,
            UnitOfWorkTransactionBehavior.Transactional);

        var inner = await coordinator.BeginAsync(options, TestContext.Current.CancellationToken);
        coordinator.Current.Should().BeSameAs(inner);

        await inner.DisposeAsync();

        clientFactory.CreateCallCount.Should().Be(2);
        coordinator.Current.Should().BeSameAs(outer);
    }

    /// <summary>
    /// 为协调器提供不依赖真实数据库的客户端
    /// </summary>
    private sealed class FakeSqlSugarClientFactory : ISqlSugarClientFactory
    {
        /// <summary>
        /// 已创建的测试客户端数量
        /// </summary>
        public int CreateCallCount { get; private set; }

        /// <summary>
        /// 创建当前测试作用域使用的客户端占位对象
        /// </summary>
        /// <param name="cancellationToken">工作单元创建调用传入的取消令牌</param>
        /// <returns>客户端占位对象</returns>
        public object CreateClient(CancellationToken cancellationToken)
        {
            CreateCallCount++;
            return new object();
        }
    }
}
