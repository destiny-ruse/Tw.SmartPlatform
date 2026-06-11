using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Tw.AspNetCore.Mvc.Context;
using Tw.AspNetCore.Mvc.DynamicProxy;
using Tw.DependencyInjection.DynamicProxy;

namespace Tw.AspNetCore.Mvc;

/// <summary>
/// 为 <see cref="IServiceCollection"/> 提供 MVC 集成能力注册扩展
/// </summary>
public static class MvcIntegrationServiceCollectionExtensions
{
    /// <summary>
    /// 注册 MVC 集成能力，包括请求取消令牌 provider 和 action 拦截 filter
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
            if (!options.Filters.Any(IsActionInterceptionFilter))
            {
                options.Filters.Add<TwActionInterceptionFilter>();
            }
        });

        return services;
    }

    private static bool IsActionInterceptionFilter(IFilterMetadata filter) =>
        filter is TypeFilterAttribute typeFilter
        && typeFilter.ImplementationType == typeof(TwActionInterceptionFilter);
}
