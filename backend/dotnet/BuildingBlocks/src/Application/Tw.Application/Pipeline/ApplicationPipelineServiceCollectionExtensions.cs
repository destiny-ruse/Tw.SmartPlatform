using System.Reflection;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Tw.Application.Pipeline;

/// <summary>
/// 应用层用例执行管线的服务注册扩展
/// </summary>
public static class ApplicationPipelineServiceCollectionExtensions
{
    /// <summary>
    /// 注册 MediatR 与应用层固定顺序管线适配器
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="handlerAssemblies">包含 MediatR handler 的程序集集合</param>
    /// <returns>同一服务集合，便于链式调用</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> 或 <paramref name="handlerAssemblies"/> 为 null 时抛出</exception>
    /// <exception cref="ArgumentException"><paramref name="handlerAssemblies"/> 为空时抛出</exception>
    public static IServiceCollection AddApplicationPipeline(
        this IServiceCollection services,
        params Assembly[] handlerAssemblies)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(handlerAssemblies);

        if (handlerAssemblies.Length == 0)
        {
            throw new ArgumentException("至少需要提供一个 MediatR handler 程序集", nameof(handlerAssemblies));
        }

        services.AddMediatR(configuration =>
        {
            configuration.RegisterServicesFromAssemblies(handlerAssemblies);
            configuration.AddOpenBehavior(typeof(MediatRApplicationPipelineBehavior<,>));
        });

        return services;
    }
}
