using AwesomeAssertions;
using Tw.Data.Uow;
using Xunit;

namespace Tw.Data.Tests.Uow;

/// <summary>
/// 验证数据包公开的工作单元契约形状
/// </summary>
public sealed class UnitOfWorkContractTests
{
    /// <summary>
    /// 工作单元相关契约由数据包的 Uow 命名空间公开
    /// </summary>
    [Fact]
    public void Contracts_AreExposedFromDataUowNamespace()
    {
        Type[] contracts =
        [
            typeof(IUnitOfWork),
            typeof(IUnitOfWorkCoordinator),
            typeof(IOutboxTransactionBoundary),
            typeof(UnitOfWorkOptions),
            typeof(UnitOfWorkScope),
            typeof(UnitOfWorkTransactionBehavior)
        ];

        contracts.Should().OnlyContain(contract => contract.Namespace == "Tw.Data.Uow");

        Type[] interfaces =
        [
            typeof(IUnitOfWork),
            typeof(IUnitOfWorkCoordinator),
            typeof(IOutboxTransactionBoundary)
        ];

        interfaces.Should().OnlyContain(contract => contract.IsInterface);
        typeof(UnitOfWorkScope).IsEnum.Should().BeTrue();
        typeof(UnitOfWorkTransactionBehavior).IsEnum.Should().BeTrue();
    }

    /// <summary>
    /// 协调器公开当前作用域并允许调用方取消工作单元创建
    /// </summary>
    [Fact]
    public void Coordinator_ExposesCurrentScopeAndCancelableBegin()
    {
        var currentProperty = typeof(IUnitOfWorkCoordinator).GetProperty(nameof(IUnitOfWorkCoordinator.Current));
        var beginMethod = typeof(IUnitOfWorkCoordinator).GetMethod(nameof(IUnitOfWorkCoordinator.BeginAsync));

        currentProperty.Should().NotBeNull();
        currentProperty!.PropertyType.Should().Be(typeof(IUnitOfWork));
        currentProperty.CanRead.Should().BeTrue();
        currentProperty.CanWrite.Should().BeFalse();

        beginMethod.Should().NotBeNull();
        beginMethod!.ReturnType.Should().Be(typeof(Task<IUnitOfWork>));
        beginMethod.GetParameters().Select(parameter => parameter.ParameterType).Should().Equal(
            typeof(UnitOfWorkOptions),
            typeof(CancellationToken));
        beginMethod.GetParameters()[1].IsOptional.Should().BeTrue();
    }

    /// <summary>
    /// 工作单元要求调用方显式提交、回滚并异步释放作用域
    /// </summary>
    [Fact]
    public void UnitOfWork_RequiresCommitRollbackAndAsyncDisposal()
    {
        typeof(IUnitOfWork).Should().Implement<IAsyncDisposable>();

        var commitMethod = typeof(IUnitOfWork).GetMethod(nameof(IUnitOfWork.CommitAsync));
        var rollbackMethod = typeof(IUnitOfWork).GetMethod(nameof(IUnitOfWork.RollbackAsync));
        var cancellationProperty = typeof(IUnitOfWork).GetProperty(nameof(IUnitOfWork.CancellationToken));

        commitMethod.Should().NotBeNull();
        commitMethod!.ReturnType.Should().Be(typeof(Task));
        commitMethod.GetParameters().Select(parameter => parameter.ParameterType)
            .Should().Equal(typeof(CancellationToken));
        commitMethod.GetParameters()[0].IsOptional.Should().BeTrue();

        rollbackMethod.Should().NotBeNull();
        rollbackMethod!.ReturnType.Should().Be(typeof(Task));
        rollbackMethod.GetParameters().Select(parameter => parameter.ParameterType)
            .Should().Equal(typeof(CancellationToken));
        rollbackMethod.GetParameters()[0].IsOptional.Should().BeTrue();

        cancellationProperty.Should().NotBeNull();
        cancellationProperty!.PropertyType.Should().Be(typeof(CancellationToken));
        cancellationProperty.CanRead.Should().BeTrue();
        cancellationProperty.CanWrite.Should().BeFalse();
    }

    /// <summary>
    /// Outbox 事务边界公开写入资格和事务完成状态
    /// </summary>
    [Fact]
    public void OutboxBoundary_ExposesWriteEligibilityAndCompletion()
    {
        var canWriteProperty = typeof(IOutboxTransactionBoundary)
            .GetProperty(nameof(IOutboxTransactionBoundary.CanWriteOutbox));
        var completionProperty = typeof(IOutboxTransactionBoundary)
            .GetProperty(nameof(IOutboxTransactionBoundary.IsCompleted));

        canWriteProperty.Should().NotBeNull();
        canWriteProperty!.PropertyType.Should().Be(typeof(bool));
        canWriteProperty.CanRead.Should().BeTrue();
        canWriteProperty.CanWrite.Should().BeFalse();

        completionProperty.Should().NotBeNull();
        completionProperty!.PropertyType.Should().Be(typeof(bool));
        completionProperty.CanRead.Should().BeTrue();
        completionProperty.CanWrite.Should().BeFalse();
    }
}
