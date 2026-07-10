using AwesomeAssertions;
using Grpc.AspNetCore.Server;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Tw.AspNetCore.Grpc;
using Xunit;

namespace Tw.AspNetCore.Grpc.Tests.DependencyInjection;

/// <summary>
/// 覆盖GrpcIntegration服务CollectionExtensions的核心行为和边界条件
/// </summary>
public class GrpcIntegrationServiceCollectionExtensionsTests
{
    /// <summary>
    /// 验证添加GrpcIntegration注册GrpcServices
    /// </summary>
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

    /// <summary>
    /// 验证添加GrpcIntegration抛出异常参数空值异常当ServicesIs空值
    /// </summary>
    [Fact]
    public void AddGrpcIntegration_ThrowsArgumentNullException_WhenServicesIsNull()
    {
        var act = () => ((IServiceCollection)null!).AddGrpcIntegration();

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("services");
    }
}
