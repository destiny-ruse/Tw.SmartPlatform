using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Tw.IdGeneration;

namespace Tw.IdGeneration.Yitter;

/// <summary>
/// ID 生成服务注册扩展
/// </summary>
public static class IdGenerationServiceCollectionExtensions
{
    /// <summary>
    /// 注册基于 Yitter.IdGenerator 的长整型标识生成器
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="workerId">当前节点的 workerId</param>
    /// <returns>原服务集合</returns>
    /// <exception cref="ArgumentNullException">services 为 null 时抛出</exception>
    public static IServiceCollection AddYitterIdGeneration(this IServiceCollection services, ushort workerId)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddSingleton<IIdGenerator>(_ => YitterIdGenerator.CreateForWorker(workerId));
        return services;
    }
}
