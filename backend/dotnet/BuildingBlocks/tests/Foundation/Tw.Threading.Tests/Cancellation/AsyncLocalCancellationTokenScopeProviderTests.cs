using AwesomeAssertions;
using Tw.Threading;
using Xunit;

namespace Tw.Threading.Tests.Cancellation;

/// <summary>
/// 覆盖异步LocalCancellation令牌Scope提供器的核心行为和边界条件
/// </summary>
public class AsyncLocalCancellationTokenScopeProviderTests
{
    /// <summary>
    /// 验证CurrentIs空值当No作用域
    /// </summary>
    [Fact]
    public void Current_IsNull_WhenNoScope()
    {
        var sut = new AsyncLocalCancellationTokenScopeProvider();

        sut.Current.Should().BeNull();
    }

    /// <summary>
    /// 验证Begin作用域SetsCurrent和RestoresOnDispose
    /// </summary>
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

    /// <summary>
    /// 验证Begin作用域RestoresOuter作用域AfterNestedDispose
    /// </summary>
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

    /// <summary>
    /// 验证CurrentFlowsAcrossAwait边界
    /// </summary>
    /// <returns>表示异步流程完成状态的任务</returns>
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
