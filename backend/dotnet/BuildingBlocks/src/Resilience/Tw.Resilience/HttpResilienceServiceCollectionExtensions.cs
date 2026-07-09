using Microsoft.Extensions.DependencyInjection;

namespace Tw.Resilience;

/// <summary>表示 HttpResilienceServiceCollectionExtensions 类型</summary>
public static class HttpResilienceServiceCollectionExtensions
{
    /// <summary>执行 AddTwHttpResilience 操作</summary>
    /// <param name="services">services 参数</param>
    /// <returns>AddTwHttpResilience 的执行结果</returns>
    public static IServiceCollection AddTwHttpResilience(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        return services;
    }
}
