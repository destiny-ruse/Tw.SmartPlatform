using AwesomeAssertions;
using Tw.Threading;
using Xunit;

namespace Tw.Threading.Tests.Cancellation;

/// <summary>
/// 覆盖空值Cancellation令牌提供器的核心行为和边界条件
/// </summary>
public class NullCancellationTokenProviderTests
{
    /// <summary>
    /// 创建Sut测试对象
    /// </summary>
    /// <returns>方法完成后返回给调用方的结果对象</returns>
    private static NullCancellationTokenProvider CreateSut()
    {
        return new NullCancellationTokenProvider(new AsyncLocalCancellationTokenScopeProvider());
    }

    /// <summary>
    /// 验证令牌IsNone当NoOverride
    /// </summary>
    [Fact]
    public void Token_IsNone_WhenNoOverride()
    {
        var sut = CreateSut();

        sut.Token.Should().Be(CancellationToken.None);
    }

    /// <summary>
    /// 验证令牌返回OverrideWithin作用域
    /// </summary>
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

    /// <summary>
    /// 验证令牌RestoresNoneAfter作用域Disposed
    /// </summary>
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

    /// <summary>
    /// 验证令牌RestoresOuter令牌AfterNested作用域Disposed
    /// </summary>
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

    /// <summary>
    /// 验证令牌ReadsOverrideAfterAwait边界
    /// </summary>
    /// <returns>表示异步流程完成状态的任务</returns>
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
