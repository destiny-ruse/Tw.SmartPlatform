using AwesomeAssertions;
using Tw.Uow;
using Xunit;

namespace Tw.Uow.Tests;

/// <summary>验证 UnitOfWorkOptionsTests 相关行为</summary>
public sealed class UnitOfWorkOptionsTests
{
    /// <summary>验证 DefaultOptions_UseRequiredTransactionalBehavior 场景</summary>
    [Fact]
    public void DefaultOptions_UseRequiredTransactionalBehavior()
    {
        var options = UnitOfWorkOptions.Default;

        options.Scope.Should().Be(UnitOfWorkScope.Required);
        options.TransactionBehavior.Should().Be(UnitOfWorkTransactionBehavior.Transactional);
    }
}
