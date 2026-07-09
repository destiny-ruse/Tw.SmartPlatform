using AwesomeAssertions;
using Tw.Threading;
using Xunit;

namespace Tw.Threading.Tests.Cancellation;

/// <summary>验证 AsyncLocalCancellationTokenScopeProviderTests 相关行为</summary>
public class AsyncLocalCancellationTokenScopeProviderTests
{
    /// <summary>验证 Current_IsNull_WhenNoScope 场景</summary>
    [Fact]
    public void Current_IsNull_WhenNoScope()
    {
        var sut = new AsyncLocalCancellationTokenScopeProvider();

        sut.Current.Should().BeNull();
    }

    /// <summary>验证 BeginScope_SetsCurrent_AndRestoresOnDispose 场景</summary>
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

    /// <summary>验证 BeginScope_RestoresOuterScope_AfterNestedDispose 场景</summary>
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

    /// <summary>验证 Current_FlowsAcross_AwaitBoundary 场景</summary>
    /// <returns>Current_FlowsAcross_AwaitBoundary 的执行结果</returns>
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
