using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Localization;
using Tw.Localization;
using CoreLocalizationOptions = Tw.Localization.LocalizationOptions;

namespace Tw.AspNetCore.Localization;

/// <summary>
/// 提供 ASP.NET Core 请求本地化与字符串本地化的服务注册入口
/// </summary>
public static class LocalizationServiceCollectionExtensions
{
    /// <summary>
    /// 注册核心本地化、请求上下文和静态快照字符串本地化适配器
    /// </summary>
    /// <param name="services">接收本地化服务注册的服务集合</param>
    /// <param name="configure">配置默认文化与支持文化范围的委托</param>
    /// <returns>调用方传入的同一服务集合</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="services"/> 或 <paramref name="configure"/> 为 <see langword="null"/> 时抛出
    /// </exception>
    /// <remarks>
    /// 请求上下文访问器、字符串本地化器工厂和开放泛型本地化器使用 Scoped 生命周期
    /// 调用方可在本方法执行前注册自定义实现以覆盖默认适配器
    /// </remarks>
    public static IServiceCollection AddLocalization(
        this IServiceCollection services,
        Action<CoreLocalizationOptions> configure)
    {
        Check.NotNull(services);
        Check.NotNull(configure);

        global::Tw.Localization.LocalizationServiceCollectionExtensions.AddLocalization(
            services,
            configure);

        services.TryAddScoped<ICurrentLocalizationContextAccessor, CurrentLocalizationContextAccessor>();
        services.TryAddScoped<IStringLocalizerFactory, StaticSnapshotStringLocalizerFactory>();
        services.TryAddScoped(typeof(IStringLocalizer<>), typeof(StaticSnapshotStringLocalizer<>));

        return services;
    }
}
