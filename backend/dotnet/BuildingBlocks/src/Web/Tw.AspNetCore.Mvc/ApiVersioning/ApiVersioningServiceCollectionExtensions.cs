using Asp.Versioning;
using Microsoft.Extensions.DependencyInjection;

namespace Tw.AspNetCore.Mvc.ApiVersioning;

/// <summary>表示 ApiVersioningServiceCollectionExtensions 类型</summary>
public static class ApiVersioningServiceCollectionExtensions
{
    /// <summary>执行 AddApiVersioningIntegration 操作</summary>
    /// <param name="services">services 参数</param>
    /// <returns>AddApiVersioningIntegration 的执行结果</returns>
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
