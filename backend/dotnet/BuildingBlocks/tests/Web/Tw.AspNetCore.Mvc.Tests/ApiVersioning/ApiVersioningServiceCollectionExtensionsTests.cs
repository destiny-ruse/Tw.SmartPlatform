using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Tw.AspNetCore.Mvc.ApiVersioning;
using Xunit;

namespace Tw.AspNetCore.Mvc.Tests.ApiVersioning;

/// <summary>验证 ApiVersioningServiceCollectionExtensionsTests 相关行为</summary>
public sealed class ApiVersioningServiceCollectionExtensionsTests
{
    /// <summary>验证 AddApiVersioningIntegration_RegistersUrlSegmentVersioning 场景</summary>
    [Fact]
    public void AddApiVersioningIntegration_RegistersUrlSegmentVersioning()
    {
        var services = new ServiceCollection();

        services.AddApiVersioningIntegration();

        services.Should().Contain(descriptor =>
            descriptor.ServiceType.FullName!.Contains("IApiVersionReader", StringComparison.Ordinal));
    }
}
