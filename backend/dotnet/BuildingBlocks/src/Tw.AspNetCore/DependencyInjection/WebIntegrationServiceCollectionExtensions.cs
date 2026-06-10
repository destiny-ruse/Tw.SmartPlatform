using Microsoft.Extensions.DependencyInjection;
using Tw;

namespace Tw.AspNetCore;

/// <summary>
/// 为 <see cref="IServiceCollection"/> 提供 <c>Tw.AspNetCore</c> 宿主级 Web 集成兼容入口
/// </summary>
/// <remarks>
/// 当前入口保留宿主级聚合调用面，不注册 MVC、Razor Pages 或其他协议专属能力。
/// MVC 专属能力由 <c>Tw.AspNetCore.Mvc</c> 包提供。
/// </remarks>
public static class WebIntegrationServiceCollectionExtensions
{
    /// <summary>
    /// 保留宿主级 Web 集成兼容入口
    /// </summary>
    /// <param name="services">服务容器</param>
    /// <returns>同一 <see cref="IServiceCollection"/> 实例，便于链式调用</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="services"/> 为 <see langword="null"/> 时抛出</exception>
    /// <remarks>当前不注册协议专属能力；MVC 请求相关能力已迁移到 <c>Tw.AspNetCore.Mvc</c>。</remarks>
    public static IServiceCollection AddWebIntegration(this IServiceCollection services)
    {
        Check.NotNull(services);

        return services;
    }
}
