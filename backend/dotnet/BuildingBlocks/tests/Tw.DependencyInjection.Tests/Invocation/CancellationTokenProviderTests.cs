using FluentAssertions;
using Tw.DependencyInjection.Cancellation;
using Xunit;

namespace Tw.DependencyInjection.Tests.Invocation;

/// <summary>
/// 验证 <see cref="NullCancellationTokenProvider"/> 和 <see cref="CurrentCancellationTokenAccessor"/> 的行为契约。
/// </summary>
public sealed class CancellationTokenProviderTests
{
    [Fact]
    public void NullProvider_Returns_None()
    {
        var provider = new NullCancellationTokenProvider();
        provider.Token.Should().Be(CancellationToken.None);
    }

    [Fact]
    public void Use_Sets_Ambient_Token()
    {
        var accessor = new CurrentCancellationTokenAccessor();
        using var cts = new CancellationTokenSource();

        using (accessor.Use(cts.Token))
        {
            ((ICancellationTokenProvider)accessor).Token.Should().Be(cts.Token);
        }

        ((ICancellationTokenProvider)accessor).Token.Should().Be(CancellationToken.None);
    }

    [Fact]
    public void Nested_Use_Scopes_Restore_Previous_Token()
    {
        var accessor = new CurrentCancellationTokenAccessor();
        using var ctsOuter = new CancellationTokenSource();
        using var ctsInner = new CancellationTokenSource();

        using (accessor.Use(ctsOuter.Token))
        {
            using (accessor.Use(ctsInner.Token))
            {
                ((ICancellationTokenProvider)accessor).Token.Should().Be(ctsInner.Token);
            }

            ((ICancellationTokenProvider)accessor).Token.Should().Be(ctsOuter.Token);
        }

        ((ICancellationTokenProvider)accessor).Token.Should().Be(CancellationToken.None);
    }

    [Fact]
    public async Task Ambient_Token_Crosses_Await()
    {
        var accessor = new CurrentCancellationTokenAccessor();
        using var cts = new CancellationTokenSource();

        using (accessor.Use(cts.Token))
        {
            await Task.Yield();
            ((ICancellationTokenProvider)accessor).Token.Should().Be(cts.Token);
        }
    }

    [Fact]
    public void Dispose_Is_Idempotent()
    {
        var accessor = new CurrentCancellationTokenAccessor();
        using var cts = new CancellationTokenSource();

        var scope = accessor.Use(cts.Token);
        scope.Dispose();
        scope.Dispose(); // 第二次 Dispose 不应改变状态。

        ((ICancellationTokenProvider)accessor).Token.Should().Be(CancellationToken.None);
    }
}
