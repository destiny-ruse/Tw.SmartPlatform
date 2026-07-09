using Microsoft.Extensions.DependencyInjection;

namespace Tw.Gateway.Yarp;

/// <summary>表示 YarpGatewayBuilderExtensions 类型</summary>
public static class YarpGatewayBuilderExtensions
{
    /// <summary>执行 AddTwYarpGateway 操作</summary>
    /// <param name="services">services 参数</param>
    /// <returns>AddTwYarpGateway 的执行结果</returns>
    public static IServiceCollection AddTwYarpGateway(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        return services;
    }
}
