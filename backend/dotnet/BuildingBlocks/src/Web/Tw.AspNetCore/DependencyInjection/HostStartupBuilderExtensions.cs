using Autofac;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Tw.DependencyInjection.Autofac;

namespace Tw.AspNetCore;

/// <summary>
/// ASP.NET Core 宿主级启动能力聚合入口
/// </summary>
public static class HostStartupBuilderExtensions
{
    /// <summary>
    /// 使用 Autofac 接管宿主容器，并在 Autofac 原生注册路径启用服务自动注册
    /// </summary>
    /// <param name="builder">ASP.NET Core 应用构建器</param>
    /// <returns>同一应用构建器，便于链式调用</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> 为 <see langword="null"/> 时抛出</exception>
    public static WebApplicationBuilder UseWebIntegration(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Host.UseAutofac();
        builder.Host.ConfigureContainer<ContainerBuilder>(
            containerBuilder => containerBuilder.AddServiceRegistration(builder.Configuration));

        return builder;
    }
}
