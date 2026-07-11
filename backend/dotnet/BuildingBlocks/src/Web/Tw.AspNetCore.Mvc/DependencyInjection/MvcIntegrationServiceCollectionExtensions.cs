using Microsoft.Extensions.DependencyInjection;
using Tw.AspNetCore.Mvc.Context;

namespace Tw.AspNetCore.Mvc;

/// <summary>
/// 为 <see cref="IServiceCollection"/> 提供 MVC 集成能力注册扩展
/// </summary>
public static class MvcIntegrationServiceCollectionExtensions
{
    /// <summary>
    /// 注册 MVC 集成能力，包括请求取消令牌 provider
    /// </summary>
    /// <param name="services">服务容器</param>
    /// <returns>同一 <see cref="IServiceCollection"/> 实例，便于链式调用</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="services"/> 为 <see langword="null"/> 时抛出</exception>
    public static IServiceCollection AddMvcIntegration(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddHttpContextCancellationTokenProvider();

        return services;
    }
}
