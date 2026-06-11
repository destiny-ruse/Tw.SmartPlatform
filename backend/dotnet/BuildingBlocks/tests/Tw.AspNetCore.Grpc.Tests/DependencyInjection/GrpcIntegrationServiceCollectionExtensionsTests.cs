using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Tw.AspNetCore.Grpc;
using Xunit;

namespace Tw.AspNetCore.Grpc.Tests.DependencyInjection;

public class GrpcIntegrationServiceCollectionExtensionsTests
{
    [Fact]
    public void AddGrpcIntegration_RegistersGrpcServices()
    {
        var services = new ServiceCollection();

        services.AddGrpcIntegration();

        services.Should().Contain(descriptor =>
            descriptor.ServiceType.FullName!.Contains("Grpc", StringComparison.Ordinal));
    }
}
