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
        boundary.CanWriteOutbox.Should().BeTrue();
        boundary.IsCompleted.Should().BeTrue();
    }

    /// <summary>
    /// 为协调器提供不依赖真实数据库的客户端
    /// </summary>
    private sealed class FakeSqlSugarClientFactory : ISqlSugarClientFactory
    {
        /// <summary>
        /// 创建当前测试作用域使用的客户端占位对象
        /// </summary>
        /// <param name="cancellationToken">工作单元创建调用传入的取消令牌</param>
        /// <returns>客户端占位对象</returns>
        public object CreateClient(CancellationToken cancellationToken)
        {
            return new object();
        }
    }
}
