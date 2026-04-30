using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Tw.AspNetCore.Cancellation;
using Tw.AspNetCore.Mvc;
using Tw.DependencyInjection.Cancellation;
using Tw.DependencyInjection.Registration;

namespace Tw.AspNetCore;

/// <summary>
/// ASP.NET Core 基础设施组合入口
/// </summary>
public static class AddTwAspNetCoreInfrastructureExtensions
{
    /// <summary>
    /// 添加 Tw ASP.NET Core 基础设施、Autofac 容器和 MVC Entry 拦截
    /// </summary>
    /// <param name="builder">WebApplication 构建器</param>
    /// <param name="configure">自动注册配置回调</param>
    public static WebApplicationBuilder AddTwAspNetCoreInfrastructure(
        this WebApplicationBuilder builder,
        Action<AutoRegistrationOptions>? configure = null)
    {
        builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());
        builder.Services.AddAutoRegistration(builder.Configuration, configure);
        builder.Host.ConfigureContainer<ContainerBuilder>(
            containerBuilder => containerBuilder.UseAutoRegistration(builder.Services));

        builder.Services.AddHttpContextAccessor();
        builder.Services.RemoveAll<ICancellationTokenProvider>();
        builder.Services.AddScoped<ICancellationTokenProvider, HttpContextCancellationTokenProvider>();
        builder.Services.TryAddScoped<TwEntryInterceptorFilter>();
        builder.Services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IConfigureOptions<Microsoft.AspNetCore.Mvc.MvcOptions>, TwMvcOptionsSetup>());

        return builder;
    }
}
