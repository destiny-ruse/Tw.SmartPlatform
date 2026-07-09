using AwesomeAssertions;
using Tw.Threading;
using Xunit;

namespace Tw.Threading.Tests.Cancellation;

/// <summary>验证 CancellationTokenProviderExtensionsTests 相关行为</summary>
public class CancellationTokenProviderExtensionsTests
{
    /// <summary>验证 CreateProvider 场景</summary>
    /// <returns>CreateProvider 的执行结果</returns>
    private static NullCancellationTokenProvider CreateProvider()
    {
        return new NullCancellationTokenProvider(new AsyncLocalCancellationTokenScopeProvider());
    }

    /// <summary>验证 FallbackToProvider_ReturnsExplicitToken_WhenProvided 场景</summary>
    [Fact]
    public void FallbackToProvider_ReturnsExplicitToken_WhenProvided()
    {
        var provider = CreateProvider();
        using var explicitCts = new CancellationTokenSource();

        var result = provider.FallbackToProvider(explicitCts.Token);

        result.Should().Be(explicitCts.Token);
    }

    /// <summary>验证 FallbackToProvider_ReturnsProviderToken_WhenExplicitTokenIsDefault 场景</summary>
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

    /// <summary>验证 FallbackToProvider_ReturnsProviderToken_WhenExplicitTokenIsNone 场景</summary>
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
