using Autofac.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Tw.DependencyInjection.Autofac;

/// <summary>
/// 在宿主构建阶段用 Autofac 接管默认依赖注入容器的扩展
/// </summary>
public static class AutofacHostBuilderExtensions
{
    /// <summary>
    /// 使用 Autofac 接管宿主的服务提供程序工厂
    /// </summary>
    /// <param name="hostBuilder">宿主构建器</param>
    /// <returns>同一宿主构建器，便于链式调用</returns>
    /// <exception cref="ArgumentNullException"><paramref name="hostBuilder"/> 为 <see langword="null"/> 时抛出</exception>
    public static IHostBuilder UseAutofac(this IHostBuilder hostBuilder)
    {
        ArgumentNullException.ThrowIfNull(hostBuilder);
        return hostBuilder.UseServiceProviderFactory(new AutofacServiceProviderFactory());
    }
}
