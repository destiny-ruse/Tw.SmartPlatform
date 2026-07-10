using AwesomeAssertions;
using Tw.Threading;
using Xunit;

namespace Tw.Threading.Tests.Cancellation;

/// <summary>
/// 覆盖Cancellation令牌提供器Extensions的核心行为和边界条件
/// </summary>
public class CancellationTokenProviderExtensionsTests
{
    /// <summary>
    /// 创建提供器测试对象
    /// </summary>
    /// <returns>方法完成后返回给调用方的结果对象</returns>
    private static NullCancellationTokenProvider CreateProvider()
    {
        return new NullCancellationTokenProvider(new AsyncLocalCancellationTokenScopeProvider());
    }

    /// <summary>
    /// 验证回退到提供器返回Explicit令牌当Provided
    /// </summary>
    [Fact]
    public void FallbackToProvider_ReturnsExplicitToken_WhenProvided()
    {
        var provider = CreateProvider();
        using var explicitCts = new CancellationTokenSource();

        var result = provider.FallbackToProvider(explicitCts.Token);

        result.Should().Be(explicitCts.Token);
    }

    /// <summary>
    /// 验证回退到提供器返回提供器令牌当Explicit令牌Is默认
    /// </summary>
    [Fact]
    public void FallbackToProvider_ReturnsProviderToken_WhenExplicitTokenIsDefault()
    {
        var provider = CreateProvider();
        var scopedToken = TestContext.Current.CancellationToken;

        using (provider.Use(scopedToken))
        {
#pragma warning disable xUnit1051 // 本用例验证缺省 cancellationToken 会回退到 provider
            var result = provider.FallbackToProvider();
#pragma warning restore xUnit1051

            result.Should().Be(scopedToken);
        }
    }

    /// <summary>
    /// 验证回退到提供器返回提供器令牌当Explicit令牌IsNone
    /// </summary>
    [Fact]
    public void FallbackToProvider_ReturnsProviderToken_WhenExplicitTokenIsNone()
    {
        var provider = CreateProvider();
        var scopedToken = TestContext.Current.CancellationToken;

        using (provider.Use(scopedToken))
        {
            var result = provider.FallbackToProvider(CancellationToken.None);

            result.Should().Be(scopedToken);
        }
    }
}
