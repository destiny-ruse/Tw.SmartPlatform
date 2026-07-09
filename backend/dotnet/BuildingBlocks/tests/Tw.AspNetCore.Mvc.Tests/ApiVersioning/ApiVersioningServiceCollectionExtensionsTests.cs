using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Tw.AspNetCore.Mvc.ApiVersioning;
using Xunit;

namespace Tw.AspNetCore.Mvc.Tests.ApiVersioning;

public sealed class ApiVersioningServiceCollectionExtensionsTests
{
    [Fact]
    public void AddApiVersioningIntegration_RegistersUrlSegmentVersioning()
    {
        var services = new ServiceCollection();

        services.AddApiVersioningIntegration();

        services.Should().Contain(descriptor =>
            descriptor.ServiceType.FullName!.Contains("IApiVersionReader", StringComparison.Ordinal));
    }
}
