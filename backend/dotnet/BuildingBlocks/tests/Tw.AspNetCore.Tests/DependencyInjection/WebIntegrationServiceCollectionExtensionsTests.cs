using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Tw.AspNetCore.Tests.DependencyInjection;

public class WebIntegrationServiceCollectionExtensionsTests
{
    [Fact]
    public void AddWebIntegration_ReturnsSameServices_ForChaining()
    {
        var services = new ServiceCollection();

        var result = services.AddWebIntegration();

        result.Should().BeSameAs(services);
    }

    [Fact]
    public void AddWebIntegration_DoesNotRegisterHttpContextAccessor()
    {
        var services = new ServiceCollection();

        services.AddWebIntegration();

        using var provider = services.BuildServiceProvider();
        provider.GetService<Microsoft.AspNetCore.Http.IHttpContextAccessor>().Should().BeNull();
    }
}
