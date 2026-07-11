using Microsoft.Extensions.DependencyInjection;

namespace Tw.AspNetCore.Mvc;

/// <summary>
/// 为 <see cref="IServiceCollection"/> 提供 MVC 集成能力注册扩展
/// </summary>
public static class MvcIntegrationServiceCollectionExtensions
{
    /// <summary>
    /// 验证服务集合并保留 MVC 集成扩展入口
    /// </summary>
    /// <param name="services">服务容器</param>
    /// <returns>同一 <see cref="IServiceCollection"/> 实例，便于链式调用</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="services"/> 为 <see langword="null"/> 时抛出</exception>
    public static IServiceCollection AddMvcIntegration(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        return services;
    }
}
