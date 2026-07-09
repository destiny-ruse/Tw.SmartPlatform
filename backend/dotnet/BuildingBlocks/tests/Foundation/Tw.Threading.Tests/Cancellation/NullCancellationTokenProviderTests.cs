using AwesomeAssertions;
using Tw.Threading;
using Xunit;

namespace Tw.Threading.Tests.Cancellation;

/// <summary>验证 NullCancellationTokenProviderTests 相关行为</summary>
public class NullCancellationTokenProviderTests
{
    /// <summary>验证 CreateSut 场景</summary>
    /// <returns>CreateSut 的执行结果</returns>
    private static NullCancellationTokenProvider CreateSut()
    {
        return new NullCancellationTokenProvider(new AsyncLocalCancellationTokenScopeProvider());
    }

    /// <summary>验证 Token_IsNone_WhenNoOverride 场景</summary>
    [Fact]
    public void Token_IsNone_WhenNoOverride()
    {
        var sut = CreateSut();

        sut.Token.Should().Be(CancellationToken.None);
    }

    /// <summary>验证 Token_ReturnsOverride_WithinScope 场景</summary>
    [Fact]
    public void Token_ReturnsOverride_WithinScope()
    {
        var sut = CreateSut();
        using var cts = new CancellationTokenSource();

        using (sut.Use(cts.Token))
        {
            sut.Token.Should().Be(cts.Token);
        }
    }

    /// <summary>验证 Token_RestoresNone_AfterScopeDisposed 场景</summary>
    [Fact]
    public void Token_RestoresNone_AfterScopeDisposed()
    {
        var sut = CreateSut();
        using var cts = new CancellationTokenSource();

        using (sut.Use(cts.Token))
        {
        }

        sut.Token.Should().Be(CancellationToken.None);
    }

    /// <summary>验证 Token_RestoresOuterToken_AfterNestedScopeDisposed 场景</summary>
    [Fact]
    public void Token_RestoresOuterToken_AfterNestedScopeDisposed()
    {
        var sut = CreateSut();
        using var outer = new CancellationTokenSource();
        using var inner = new CancellationTokenSource();

        using (sut.Use(outer.Token))
        {
            using (sut.Use(inner.Token))
            {
                sut.Token.Should().Be(inner.Token);
            }

            sut.Token.Should().Be(outer.Token);
        }
    }

    /// <summary>验证 Token_ReadsOverride_AfterAwaitBoundary 场景</summary>
    /// <returns>Token_ReadsOverride_AfterAwaitBoundary 的执行结果</returns>
    [Fact]
    public async Task Token_ReadsOverride_AfterAwaitBoundary()
    {
        var sut = CreateSut();
        using var cts = new CancellationTokenSource();

        using (sut.Use(cts.Token))
        {
            await Task.Yield();
            sut.Token.Should().Be(cts.Token);
        }
    }
}
