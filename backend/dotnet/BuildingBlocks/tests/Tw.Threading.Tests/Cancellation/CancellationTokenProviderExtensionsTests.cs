using AwesomeAssertions;
using Tw.Threading;
using Xunit;

namespace Tw.Threading.Tests.Cancellation;

public class CancellationTokenProviderExtensionsTests
{
    private static NullCancellationTokenProvider CreateProvider()
    {
        return new NullCancellationTokenProvider(new AsyncLocalCancellationTokenScopeProvider());
    }

    [Fact]
    public void FallbackToProvider_ReturnsExplicitToken_WhenProvided()
    {
        var provider = CreateProvider();
        using var explicitCts = new CancellationTokenSource();

        var result = provider.FallbackToProvider(explicitCts.Token);

        result.Should().Be(explicitCts.Token);
    }

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
