using Asp.Versioning;
using Microsoft.Extensions.DependencyInjection;

namespace Tw.AspNetCore.Mvc.ApiVersioning;

/// <summary>
/// 封装ApiVersioning服务CollectionExtensions相关的数据和行为
/// </summary>
public static class ApiVersioningServiceCollectionExtensions
{
    /// <summary>
    /// 注册ApiVersioningIntegration所需服务
    /// </summary>
    /// <param name="services">需要注册组件依赖的服务集合</param>
    /// <returns>方法完成后返回给调用方的结果对象</returns>
    public static IServiceCollection AddApiVersioningIntegration(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services
            .AddApiVersioning(options =>
            {
                options.DefaultApiVersion = new ApiVersion(1, 0);
                options.AssumeDefaultVersionWhenUnspecified = false;
                options.ReportApiVersions = true;
                options.ApiVersionReader = new UrlSegmentApiVersionReader();
            })
            .AddMvc()
            .AddApiExplorer(options =>
            {
                options.GroupNameFormat = "'v'VVV";
                options.SubstituteApiVersionInUrl = true;
            });

        return services;
    }
}
