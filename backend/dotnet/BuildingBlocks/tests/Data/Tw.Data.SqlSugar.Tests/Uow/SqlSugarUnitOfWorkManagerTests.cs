using AwesomeAssertions;
using Tw.Data.SqlSugar.Connection;
using Tw.Data.SqlSugar.Uow;
using Tw.Uow;
using Xunit;

namespace Tw.Data.SqlSugar.Tests.Uow;

public sealed class SqlSugarUnitOfWorkManagerTests
{
    [Fact]
    public async Task BeginAsync_SetsCurrentUnitOfWork()
    {
        var manager = new SqlSugarUnitOfWorkManager(new FakeSqlSugarClientFactory());

        await using var uow = await manager.BeginAsync(UnitOfWorkOptions.Default);

        manager.Current.Should().BeSameAs(uow);
    }

    [Fact]
    public async Task DisposeAsync_ClearsCurrentUnitOfWork()
    {
        var manager = new SqlSugarUnitOfWorkManager(new FakeSqlSugarClientFactory());

        var uow = await manager.BeginAsync(UnitOfWorkOptions.Default);
        await uow.DisposeAsync();

        manager.Current.Should().BeNull();
    }

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

    private sealed class FakeSqlSugarClientFactory : ISqlSugarClientFactory
    {
        public object CreateClient(CancellationToken cancellationToken)
        {
            return new object();
        }
    }
}
