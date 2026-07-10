using AwesomeAssertions;
using Tw.Uow;
using Xunit;

namespace Tw.Uow.Tests;

/// <summary>
/// 覆盖UnitOfWork选项的核心行为和边界条件
/// </summary>
public sealed class UnitOfWorkOptionsTests
{
    /// <summary>
    /// 验证默认选项Use必需Transactional行为
    /// </summary>
    [Fact]
    public void DefaultOptions_UseRequiredTransactionalBehavior()
    {
        var options = UnitOfWorkOptions.Default;

        options.Scope.Should().Be(UnitOfWorkScope.Required);
        options.TransactionBehavior.Should().Be(UnitOfWorkTransactionBehavior.Transactional);
    }
}
