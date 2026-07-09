using AwesomeAssertions;
using Tw.Threading;
using Xunit;

namespace Tw.Threading.Tests.Cancellation;

public class NullCancellationTokenProviderTests
{
    private static NullCancellationTokenProvider CreateSut()
    {
        return new NullCancellationTokenProvider(new AsyncLocalCancellationTokenScopeProvider());
    }

    [Fact]
    public void Token_IsNone_WhenNoOverride()
    {
        var sut = CreateSut();

        sut.Token.Should().Be(CancellationToken.None);
    }

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
