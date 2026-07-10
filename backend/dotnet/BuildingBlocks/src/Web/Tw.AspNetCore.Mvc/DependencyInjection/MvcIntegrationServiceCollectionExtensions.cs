using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Tw.AspNetCore.Mvc.Context;
using Tw.AspNetCore.Mvc.DynamicProxy;
using Tw.Castle.Core;

namespace Tw.AspNetCore.Mvc;

/// <summary>
/// 为 <see cref="IServiceCollection"/> 提供 MVC 集成能力注册扩展
/// </summary>
public static class MvcIntegrationServiceCollectionExtensions
{
    /// <summary>
    /// 注册 MVC 集成能力，包括请求取消令牌 provider、action 拦截 filter 和 Razor Page handler 拦截 filter
    /// </summary>
    /// <param name="services">服务容器</param>
    /// <returns>同一 <see cref="IServiceCollection"/> 实例，便于链式调用</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="services"/> 为 <see langword="null"/> 时抛出</exception>
    public static IServiceCollection AddMvcIntegration(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddHttpContextCancellationTokenProvider();
        services.TryAddSingleton<IInterceptorSelector, AttributeInterceptorSelector>();
        services.TryAddSingleton<IInterceptorPipeline, InterceptorPipeline>();
        services.Configure<MvcOptions>(options =>
        {
            if (!options.Filters.Any(IsInterceptionFilter<TwActionInterceptionFilter>))
            {
                options.Filters.Add<TwActionInterceptionFilter>();
            }

            if (!options.Filters.Any(IsInterceptionFilter<TwPageInterceptionFilter>))
            {
                options.Filters.Add<TwPageInterceptionFilter>();
            }
        });

        return services;
    }

    /// <summary>
    /// 判断nterception过滤器是否满足条件
    /// </summary>
    /// <typeparam name="TFilter">响应数据的运行时类型</typeparam>
    /// <param name="filter">参与测试的 MVC 或页面过滤器实例</param>
    /// <returns>条件满足时返回 <see langword="true"/></returns>
    private static bool IsInterceptionFilter<TFilter>(IFilterMetadata filter) =>
        filter is TypeFilterAttribute typeFilter
        && typeFilter.ImplementationType == typeof(TFilter);
}
