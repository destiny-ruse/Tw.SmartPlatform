using Microsoft.Extensions.DependencyInjection;

namespace Tw.Resilience;

/// <summary>
/// 封装HttpResilience服务CollectionExtensions相关的数据和行为
/// </summary>
public static class HttpResilienceServiceCollectionExtensions
{
    /// <summary>
    /// 注册TwHttpResilience所需服务
    /// </summary>
    /// <param name="services">需要注册组件依赖的服务集合</param>
    /// <returns>方法完成后返回给调用方的结果对象</returns>
    public static IServiceCollection AddTwHttpResilience(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        return services;
    }
}
