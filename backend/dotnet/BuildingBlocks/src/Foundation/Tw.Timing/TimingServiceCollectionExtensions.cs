using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Tw.Timing;

/// <summary>
/// 为 <see cref="IServiceCollection"/> 提供时间能力注册入口
/// </summary>
public static class TimingServiceCollectionExtensions
{
    /// <summary>
    /// 注册默认时间能力
    /// </summary>
    /// <param name="services">服务容器</param>
    /// <returns>同一 <see cref="IServiceCollection"/> 实例，便于链式调用</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="services"/> 为 <see langword="null"/> 时抛出</exception>
    public static IServiceCollection AddTiming(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddSingleton<IClock, SystemClock>();
        return services;
    }
}
