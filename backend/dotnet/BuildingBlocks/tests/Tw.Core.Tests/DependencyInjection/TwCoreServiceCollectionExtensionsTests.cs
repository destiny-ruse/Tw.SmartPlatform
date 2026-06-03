using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Tw.Context;
using Xunit;

namespace Tw.Core.Tests.DependencyInjection;

public class TwCoreServiceCollectionExtensionsTests
{
    [Fact]
    public void AddTwCore_RegistersNullProvider_AsDefault()
    {
        var services = new ServiceCollection();

        services.AddTwCore();

        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<ICancellationTokenProvider>()
            .Should().BeOfType<NullCancellationTokenProvider>();
    }

    [Fact]
    public void AddTwCore_RegistersScopeProvider_AsSingleton()
    {
        var services = new ServiceCollection();

        services.AddTwCore();

        using var provider = services.BuildServiceProvider();
        var first = provider.GetRequiredService<AsyncLocalCancellationTokenScopeProvider>();
        var second = provider.GetRequiredService<AsyncLocalCancellationTokenScopeProvider>();
        first.Should().BeSameAs(second);
    }
}
