using AwesomeAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Tw.AspNetCore.Mvc.Context;
using Tw.Threading;
using Xunit;

namespace Tw.AspNetCore.Mvc.Tests.Context;

/// <summary>验证 CancellationTokenServiceCollectionExtensionsTests 相关行为</summary>
public class CancellationTokenServiceCollectionExtensionsTests
{
    /// <summary>验证 AddHttpContextCancellationTokenProvider_ReplacesProvider_WithHttpContextProvider 场景</summary>
    [Fact]
    public void AddHttpContextCancellationTokenProvider_ReplacesProvider_WithHttpContextProvider()
    {
        var services = new ServiceCollection();

        services.AddHttpContextCancellationTokenProvider();

        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<ICancellationTokenProvider>()
            .Should().BeOfType<HttpContextCancellationTokenProvider>();
    }

    /// <summary>验证 AddHttpContextCancellationTokenProvider_RegistersHttpContextAccessor 场景</summary>
    [Fact]
    public void AddHttpContextCancellationTokenProvider_RegistersHttpContextAccessor()
    {
        var services = new ServiceCollection();

        services.AddHttpContextCancellationTokenProvider();

        using var provider = services.BuildServiceProvider();
        provider.GetService<IHttpContextAccessor>().Should().NotBeNull();
    }

    /// <summary>验证 AddHttpContextCancellationTokenProvider_RegistersScopeProvider_AsSingleton 场景</summary>
    [Fact]
    public void AddHttpContextCancellationTokenProvider_RegistersScopeProvider_AsSingleton()
    {
        var services = new ServiceCollection();

        services.AddHttpContextCancellationTokenProvider();

        using var provider = services.BuildServiceProvider();
        var first = provider.GetRequiredService<AsyncLocalCancellationTokenScopeProvider>();
        var second = provider.GetRequiredService<AsyncLocalCancellationTokenScopeProvider>();
        first.Should().BeSameAs(second);
    }
}
