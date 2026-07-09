using Asp.Versioning;
using Microsoft.Extensions.DependencyInjection;

namespace Tw.AspNetCore.Mvc.ApiVersioning;

public static class ApiVersioningServiceCollectionExtensions
{
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
