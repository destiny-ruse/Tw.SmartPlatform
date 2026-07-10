using Microsoft.Extensions.DependencyInjection;

namespace Tw.Gateway.Yarp;

/// <summary>
/// 封装YarpGateway构建器Extensions相关的数据和行为
/// </summary>
public static class YarpGatewayBuilderExtensions
{
    /// <summary>
    /// 注册TwYarpGateway所需服务
    /// </summary>
    /// <param name="services">需要注册组件依赖的服务集合</param>
    /// <returns>方法完成后返回给调用方的结果对象</returns>
    public static IServiceCollection AddTwYarpGateway(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        return services;
    }
}
