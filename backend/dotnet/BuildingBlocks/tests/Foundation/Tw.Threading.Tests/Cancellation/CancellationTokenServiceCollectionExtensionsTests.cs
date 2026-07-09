using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Tw.Threading;
using Xunit;

namespace Tw.Threading.Tests.Cancellation;

/// <summary>验证 CancellationTokenServiceCollectionExtensionsTests 相关行为</summary>
public class CancellationTokenServiceCollectionExtensionsTests
{
    /// <summary>验证 AddCancellationTokenProvider_RegistersNullProvider_AsDefault 场景</summary>
    [Fact]
    public void AddCancellationTokenProvider_RegistersNullProvider_AsDefault()
    {
        var services = new ServiceCollection();

        services.AddCancellationTokenProvider();

        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<ICancellationTokenProvider>()
            .Should().BeOfType<NullCancellationTokenProvider>();
    }

    /// <summary>验证 AddCancellationTokenProvider_RegistersScopeProvider_AsSingleton 场景</summary>
    [Fact]
    public void AddCancellationTokenProvider_RegistersScopeProvider_AsSingleton()
    {
        var services = new ServiceCollection();

        services.AddCancellationTokenProvider();

        using var provider = services.BuildServiceProvider();
        var first = provider.GetRequiredService<AsyncLocalCancellationTokenScopeProvider>();
        var second = provider.GetRequiredService<AsyncLocalCancellationTokenScopeProvider>();
        first.Should().BeSameAs(second);
    }

    /// <summary>验证 AddCancellationTokenProvider_DoesNotOverride_ExistingProvider 场景</summary>
    [Fact]
    public void AddCancellationTokenProvider_DoesNotOverride_ExistingProvider()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ICancellationTokenProvider>(
            new NullCancellationTokenProvider(new AsyncLocalCancellationTokenScopeProvider()));
        var sentinel = services.Single(d => d.ServiceType == typeof(ICancellationTokenProvider));

        services.AddCancellationTokenProvider();

        services.Single(d => d.ServiceType == typeof(ICancellationTokenProvider))
            .Should().BeSameAs(sentinel);
    }
}
