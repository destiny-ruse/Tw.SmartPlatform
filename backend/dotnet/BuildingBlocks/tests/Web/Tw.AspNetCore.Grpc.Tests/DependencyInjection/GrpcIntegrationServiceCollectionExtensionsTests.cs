using AwesomeAssertions;
using Grpc.AspNetCore.Server;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Tw.AspNetCore.Grpc;
using Xunit;

namespace Tw.AspNetCore.Grpc.Tests.DependencyInjection;

public class GrpcIntegrationServiceCollectionExtensionsTests
{
    [Fact]
    public void AddGrpcIntegration_RegistersGrpcServices()
    {
        var services = new ServiceCollection();

        var result = services.AddGrpcIntegration();

        result.Should().BeSameAs(services);
        services.Should().Contain(descriptor =>
            descriptor.ServiceType == typeof(IConfigureOptions<GrpcServiceOptions>));
        services.Should().Contain(descriptor =>
            descriptor.ServiceType.IsGenericType &&
            descriptor.ServiceType.GetGenericTypeDefinition() == typeof(IGrpcServiceActivator<>));
        services.Should().Contain(descriptor =>
            descriptor.ServiceType.IsGenericType &&
            descriptor.ServiceType.GetGenericTypeDefinition() == typeof(IGrpcInterceptorActivator<>));
    }

    [Fact]
    public void AddGrpcIntegration_ThrowsArgumentNullException_WhenServicesIsNull()
    {
        var act = () => ((IServiceCollection)null!).AddGrpcIntegration();

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("services");
    }
}
