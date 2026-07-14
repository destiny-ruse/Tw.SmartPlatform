using System.Reflection;
using AwesomeAssertions;
using Tw.DistributedLocking;
using Xunit;

namespace Tw.DistributedLocking.Tests;

/// <summary>
/// 验证分布式锁公开接口的异步句柄与取消 ABI
/// </summary>
public sealed class DistributedLockContractTests
{
    /// <summary>
    /// 获取锁契约返回可空异步句柄，并把句柄释放与取消所有权交给调用方
    /// </summary>
    [Fact]
    public void TryAcquireAsync_ExposesNullableHandleAndCallerOwnedCancellation()
    {
        var method = typeof(IDistributedLock).GetMethod(nameof(IDistributedLock.TryAcquireAsync));

        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(Task<IAsyncDisposable>));

        var returnNullability = new NullabilityInfoContext().Create(method.ReturnParameter);
        returnNullability.ReadState.Should().Be(NullabilityState.NotNull);
        returnNullability.GenericTypeArguments.Should().ContainSingle();
        returnNullability.GenericTypeArguments[0].Type.Should().Be(typeof(IAsyncDisposable));
        returnNullability.GenericTypeArguments[0].ReadState.Should().Be(NullabilityState.Nullable);

        var parameters = method.GetParameters();
        parameters.Select(parameter => parameter.ParameterType).Should().Equal(
            typeof(DistributedLockKey),
            typeof(TimeSpan),
            typeof(CancellationToken));
        parameters[2].Name.Should().Be("cancellationToken");
        parameters[2].HasDefaultValue.Should().BeTrue();
        parameters[2].DefaultValue.Should().BeNull();
    }
}
