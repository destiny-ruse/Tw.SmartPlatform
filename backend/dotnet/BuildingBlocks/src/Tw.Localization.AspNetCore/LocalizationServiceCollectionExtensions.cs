using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Localization;
using Tw.AspNetCore;
using Tw.AspNetCore.Mvc.Context;
using Tw.Localization;

namespace Tw.Localization.AspNetCore;

/// <summary>
/// 为 <see cref="IServiceCollection"/> 提供 Web 层本地化聚合注册入口，
/// 串联核心本地化能力（<c>Tw.Localization</c>）、宿主级 Web 集成入口（<c>Tw.AspNetCore</c>）、
/// MVC 请求取消令牌能力（<c>Tw.AspNetCore.Mvc</c>）以及 <see cref="IStringLocalizer"/> 适配器，
/// 完成完整的 Web 本地化服务注册。
/// </summary>
/// <remarks>
/// 本类中注册的 <see cref="ICurrentLocalizationContextAccessor"/>、<see cref="IStringLocalizerFactory"/>
/// 和开放泛型 <see cref="IStringLocalizer{T}"/> 均以 <b>Scoped</b> 生命周期注册，确保每个 HTTP 请求
/// 拥有独立的上下文访问器实例，避免单例工厂捕获 Scoped 访问器所引发的俘虏依赖问题。
/// </remarks>
public static class LocalizationServiceCollectionExtensions
{
    /// <summary>
    /// 注册 Web 层本地化全部能力，包括：
    /// <list type="bullet">
    ///   <item><description>核心本地化服务（<see cref="ITextLocalizer"/>、<see cref="IStaticTextSnapshot"/> 等）</description></item>
    ///   <item><description>宿主级 Web 集成入口</description></item>
    ///   <item><description>HTTP 请求取消令牌服务（<see cref="IHttpContextAccessor"/>、请求取消令牌 Provider 等）</description></item>
    ///   <item><description>Scoped 的 <see cref="ICurrentLocalizationContextAccessor"/>（<see cref="CurrentLocalizationContextAccessor"/>）</description></item>
    ///   <item><description>Scoped 的 <see cref="IStringLocalizerFactory"/>（<see cref="TwStringLocalizerFactory"/>）</description></item>
    ///   <item><description>Scoped 的开放泛型 <see cref="IStringLocalizer{T}"/>（<see cref="TwStringLocalizer{TResource}"/>）</description></item>
    /// </list>
    /// </summary>
    /// <param name="services">服务容器</param>
    /// <param name="configure">用于配置 <see cref="LocalizationOptions"/> 的委托</param>
    /// <returns>同一 <see cref="IServiceCollection"/> 实例，便于链式调用</returns>
    /// <exception cref="ArgumentNullException">
    /// 当 <paramref name="services"/> 或 <paramref name="configure"/> 为 <see langword="null"/> 时抛出
    /// </exception>
    /// <remarks>
    /// 使用 <see cref="ServiceCollectionDescriptorExtensions.TryAddScoped"/> 注册适配器服务，
    /// 业务应用可在调用本方法前注册自定义实现以覆盖默认值。
    /// </remarks>
    public static IServiceCollection AddLocalization(
        this IServiceCollection services,
        Action<LocalizationOptions> configure)
    {
        Check.NotNull(services);
        Check.NotNull(configure);

        services.AddWebIntegration();
        services.AddHttpContextCancellationTokenProvider();
        global::Tw.Localization.LocalizationServiceCollectionExtensions.AddLocalization(services, configure);

        services.TryAddScoped<ICurrentLocalizationContextAccessor, CurrentLocalizationContextAccessor>();
        services.TryAddScoped<IStringLocalizerFactory, TwStringLocalizerFactory>();
        services.TryAddScoped(typeof(IStringLocalizer<>), typeof(TwStringLocalizer<>));

        return services;
    }
}
