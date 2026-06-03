using FluentAssertions;
using Tw.Context;
using Xunit;

namespace Tw.Core.Tests.Context;

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
        using var scopeCts = new CancellationTokenSource();

        using (provider.Use(scopeCts.Token))
        {
            var result = provider.FallbackToProvider();

            result.Should().Be(scopeCts.Token);
        }
    }

    [Fact]
    public void FallbackToProvider_ReturnsProviderToken_WhenExplicitTokenIsNone()
    {
        var provider = CreateProvider();
        using var scopeCts = new CancellationTokenSource();

        using (provider.Use(scopeCts.Token))
        {
            var result = provider.FallbackToProvider(CancellationToken.None);

            result.Should().Be(scopeCts.Token);
        }
    }
}
