using AwesomeAssertions;
using Tw.Uow;
using Xunit;

namespace Tw.Uow.Tests;

public sealed class UnitOfWorkOptionsTests
{
    [Fact]
    public void DefaultOptions_UseRequiredTransactionalBehavior()
    {
        var options = UnitOfWorkOptions.Default;

        options.Scope.Should().Be(UnitOfWorkScope.Required);
        options.TransactionBehavior.Should().Be(UnitOfWorkTransactionBehavior.Transactional);
    }
}
