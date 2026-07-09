using AwesomeAssertions;
using Tw.Context;
using Xunit;

namespace Tw.Core.Tests.Context;

public class AsyncLocalCancellationTokenScopeProviderTests
{
    [Fact]
    public void Current_IsNull_WhenNoScope()
    {
        var sut = new AsyncLocalCancellationTokenScopeProvider();

        sut.Current.Should().BeNull();
    }

    [Fact]
    public void BeginScope_SetsCurrent_AndRestoresOnDispose()
    {
        var sut = new AsyncLocalCancellationTokenScopeProvider();
        using var cts = new CancellationTokenSource();

        using (sut.BeginScope(cts.Token))
        {
            sut.Current.Should().NotBeNull();
            sut.Current!.CancellationToken.Should().Be(cts.Token);
        }

        sut.Current.Should().BeNull();
    }

    [Fact]
    public void BeginScope_RestoresOuterScope_AfterNestedDispose()
    {
        var sut = new AsyncLocalCancellationTokenScopeProvider();
        using var outer = new CancellationTokenSource();
        using var inner = new CancellationTokenSource();

        using (sut.BeginScope(outer.Token))
        {
            using (sut.BeginScope(inner.Token))
            {
                sut.Current!.CancellationToken.Should().Be(inner.Token);
            }

            sut.Current!.CancellationToken.Should().Be(outer.Token);
        }
    }

    [Fact]
    public async Task Current_FlowsAcross_AwaitBoundary()
    {
        var sut = new AsyncLocalCancellationTokenScopeProvider();
        using var cts = new CancellationTokenSource();

        using (sut.BeginScope(cts.Token))
        {
            await Task.Yield();
            sut.Current!.CancellationToken.Should().Be(cts.Token);
        }

        sut.Current.Should().BeNull();
    }
}
