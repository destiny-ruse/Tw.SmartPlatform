using Microsoft.Extensions.DependencyInjection;
using Tw.DependencyInjection.Abstractions;

namespace Tw.DependencyInjection.Registration;

/// <summary>
/// 将 <see cref="DependencyLifetime"/> 映射为 Microsoft DI 的 <see cref="ServiceLifetime"/>
/// </summary>
internal static class DependencyLifetimeMapper
{
    /// <summary>
    /// 将框架生命周期枚举映射为 <see cref="ServiceLifetime"/>
    /// </summary>
    /// <param name="lifetime">框架定义的服务生命周期</param>
    /// <returns>对应的 <see cref="ServiceLifetime"/> 值</returns>
    /// <exception cref="ArgumentOutOfRangeException">传入未知枚举值时抛出</exception>
    public static ServiceLifetime Map(DependencyLifetime lifetime) =>
        lifetime switch
        {
            DependencyLifetime.Transient => ServiceLifetime.Transient,
            DependencyLifetime.Scoped => ServiceLifetime.Scoped,
            DependencyLifetime.Singleton => ServiceLifetime.Singleton,
            _ => throw new ArgumentOutOfRangeException(nameof(lifetime), lifetime, "未知服务生命周期"),
        };
}
