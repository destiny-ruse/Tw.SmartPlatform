using AwesomeAssertions;
using Tw.Data.SqlSugar.Connection;
using Tw.Data.SqlSugar.Uow;
using Tw.Uow;
using Xunit;

namespace Tw.Data.SqlSugar.Tests.Uow;

/// <summary>验证 SqlSugarUnitOfWorkManagerTests 相关行为</summary>
public sealed class SqlSugarUnitOfWorkManagerTests
{
    /// <summary>验证 BeginAsync_SetsCurrentUnitOfWork 场景</summary>
    /// <returns>BeginAsync_SetsCurrentUnitOfWork 的执行结果</returns>
    [Fact]
    public async Task BeginAsync_SetsCurrentUnitOfWork()
    {
        var manager = new SqlSugarUnitOfWorkManager(new FakeSqlSugarClientFactory());

        await using var uow = await manager.BeginAsync(UnitOfWorkOptions.Default);

        manager.Current.Should().BeSameAs(uow);
    }

    /// <summary>验证 DisposeAsync_ClearsCurrentUnitOfWork 场景</summary>
    /// <returns>DisposeAsync_ClearsCurrentUnitOfWork 的执行结果</returns>
    [Fact]
    public async Task DisposeAsync_ClearsCurrentUnitOfWork()
    {
        var manager = new SqlSugarUnitOfWorkManager(new FakeSqlSugarClientFactory());

        var uow = await manager.BeginAsync(UnitOfWorkOptions.Default);
        await uow.DisposeAsync();

        manager.Current.Should().BeNull();
    }

    /// <summary>验证 CommitAsync_MarksOutboxBoundaryAsCommitted 场景</summary>
    /// <returns>CommitAsync_MarksOutboxBoundaryAsCommitted 的执行结果</returns>
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

    /// <summary>验证 FakeSqlSugarClientFactory 相关行为</summary>
    private sealed class FakeSqlSugarClientFactory : ISqlSugarClientFactory
    {
        /// <summary>验证 CreateClient 场景</summary>
        /// <param name="cancellationToken">cancellationToken 参数</param>
        /// <returns>CreateClient 的执行结果</returns>
        public object CreateClient(CancellationToken cancellationToken)
        {
            return new object();
        }
    }
}
