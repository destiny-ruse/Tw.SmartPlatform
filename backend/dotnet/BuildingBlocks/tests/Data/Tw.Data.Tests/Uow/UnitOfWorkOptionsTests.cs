using AwesomeAssertions;
using Tw.Data.Uow;
using Xunit;

namespace Tw.Data.Tests.Uow;

/// <summary>
/// 验证工作单元创建选项的默认事务语义
/// </summary>
public sealed class UnitOfWorkOptionsTests
{
    /// <summary>
    /// 默认选项复用当前工作单元并启用事务
    /// </summary>
    [Fact]
    public void DefaultOptions_UseRequiredTransactionalBehavior()
    {
        var options = UnitOfWorkOptions.Default;

        options.Scope.Should().Be(UnitOfWorkScope.Required);
        options.TransactionBehavior.Should().Be(UnitOfWorkTransactionBehavior.Transactional);
    }
}
