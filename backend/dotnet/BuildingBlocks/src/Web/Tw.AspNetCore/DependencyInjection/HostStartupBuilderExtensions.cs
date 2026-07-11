using Microsoft.AspNetCore.Builder;
using Tw.DependencyInjection;

namespace Tw.AspNetCore;

/// <summary>
/// ASP.NET Core 宿主级启动能力聚合入口
/// </summary>
public static class HostStartupBuilderExtensions
{
    /// <summary>
    /// 使用 Microsoft DI 注册自动发现的服务与 Options
    /// </summary>
    /// <param name="builder">ASP.NET Core 应用构建器</param>
    /// <returns>同一应用构建器，便于链式调用</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> 为 <see langword="null"/> 时抛出</exception>
    public static WebApplicationBuilder UseWebIntegration(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddServiceRegistration(builder.Configuration);

        return builder;
    }
}
