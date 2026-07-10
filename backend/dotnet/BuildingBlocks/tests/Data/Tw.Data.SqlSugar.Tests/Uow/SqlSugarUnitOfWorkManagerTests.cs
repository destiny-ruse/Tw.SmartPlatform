using AwesomeAssertions;
using Tw.Data.SqlSugar.Connection;
using Tw.Data.SqlSugar.Uow;
using Tw.Uow;
using Xunit;

namespace Tw.Data.SqlSugar.Tests.Uow;

/// <summary>
/// 覆盖SqlSugarUnitOfWorkManager的核心行为和边界条件
/// </summary>
public sealed class SqlSugarUnitOfWorkManagerTests
{
    /// <summary>
    /// 验证Begin异步SetsCurrentUnitOfWork
    /// </summary>
    /// <returns>表示异步流程完成状态的任务</returns>
    [Fact]
    public async Task BeginAsync_SetsCurrentUnitOfWork()
    {
        var manager = new SqlSugarUnitOfWorkManager(new FakeSqlSugarClientFactory());

        await using var uow = await manager.BeginAsync(UnitOfWorkOptions.Default);

        manager.Current.Should().BeSameAs(uow);
    }

    /// <summary>
    /// 验证Dispose异步ClearsCurrentUnitOfWork
    /// </summary>
    /// <returns>表示异步流程完成状态的任务</returns>
    [Fact]
    public async Task DisposeAsync_ClearsCurrentUnitOfWork()
    {
        var manager = new SqlSugarUnitOfWorkManager(new FakeSqlSugarClientFactory());

        var uow = await manager.BeginAsync(UnitOfWorkOptions.Default);
        await uow.DisposeAsync();

        manager.Current.Should().BeNull();
    }

    /// <summary>
    /// 验证Commit异步MarksOutbox边界作为Committed
    /// </summary>
    /// <returns>表示异步流程完成状态的任务</returns>
    [Fact]
    public async Task CommitAsync_MarksOutboxBoundaryAsCommitted()
    {
        var manager = new SqlSugarUnitOfWorkManager(new FakeSqlSugarClientFactory());

        await using var uow = await manager.BeginAsync(UnitOfWorkOptions.Default);
        await uow.CommitAsync();

        var boundary = uow.Should().BeAssignableTo<IOutboxTransactionBoundary>().Subject;
        boundary.CanWriteOutbox.Should().BeTrue();
        boundary.IsCompleted.Should().BeTrue();
    }

    /// <summary>
    /// 覆盖FakeSqlSugarClientFactory的核心行为和边界条件
    /// </summary>
    private sealed class FakeSqlSugarClientFactory : ISqlSugarClientFactory
    {
        /// <summary>
        /// 创建Client测试对象
        /// </summary>
        /// <param name="cancellationToken">用于传播调用方取消请求的令牌</param>
        /// <returns>方法完成后返回给调用方的结果对象</returns>
        public object CreateClient(CancellationToken cancellationToken)
        {
            return new object();
        }
    }
}
