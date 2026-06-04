using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Tw.AspNetCore.Context;
using Tw.Context;
using Xunit;

namespace Tw.AspNetCore.Tests.DependencyInjection;

public class WebIntegrationServiceCollectionExtensionsTests
{
    [Fact]
    public void AddWebIntegration_RegistersHttpContextProvider()
    {
        var services = new ServiceCollection();

        services.AddWebIntegration();

        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<ICancellationTokenProvider>()
            .Should().BeOfType<HttpContextCancellationTokenProvider>();
    }

    [Fact]
    public void AddWebIntegration_RegistersHttpContextAccessor()
    {
        var services = new ServiceCollection();

        services.AddWebIntegration();

        using var provider = services.BuildServiceProvider();
        provider.GetService<IHttpContextAccessor>().Should().NotBeNull();
    }

    [Fact]
    public void AddWebIntegration_ReturnsSameServices_ForChaining()
    {
        var services = new ServiceCollection();

        var result = services.AddWebIntegration();

        result.Should().BeSameAs(services);
    }
}
