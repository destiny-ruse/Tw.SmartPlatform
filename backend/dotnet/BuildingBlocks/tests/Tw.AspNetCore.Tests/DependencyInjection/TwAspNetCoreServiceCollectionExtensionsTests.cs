using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Tw.AspNetCore.Context;
using Tw.Context;
using Xunit;

namespace Tw.AspNetCore.Tests.DependencyInjection;

public class TwAspNetCoreServiceCollectionExtensionsTests
{
    [Fact]
    public void AddTwAspNetCore_ReplacesProvider_WithHttpContextProvider()
    {
        var services = new ServiceCollection();

        services.AddTwAspNetCore();

        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<ICancellationTokenProvider>()
            .Should().BeOfType<HttpContextCancellationTokenProvider>();
    }

    [Fact]
    public void AddTwAspNetCore_RegistersHttpContextAccessor()
    {
        var services = new ServiceCollection();

        services.AddTwAspNetCore();

        using var provider = services.BuildServiceProvider();
        provider.GetService<IHttpContextAccessor>().Should().NotBeNull();
    }
}
