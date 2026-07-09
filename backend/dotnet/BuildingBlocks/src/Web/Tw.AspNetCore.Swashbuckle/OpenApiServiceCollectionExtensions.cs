using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;

namespace Tw.AspNetCore.Swashbuckle;

/// <summary>表示 OpenApiServiceCollectionExtensions 类型</summary>
public static class OpenApiServiceCollectionExtensions
{
    /// <summary>执行 AddOpenApiIntegration 操作</summary>
    /// <param name="services">services 参数</param>
    /// <param name="options">options 参数</param>
    /// <returns>AddOpenApiIntegration 的执行结果</returns>
    public static IServiceCollection AddOpenApiIntegration(
        this IServiceCollection services,
        OpenApiRegistrationOptions options)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(options);

        services.AddSwaggerGen(setup =>
        {
            setup.SwaggerDoc(options.DocumentName, new OpenApiInfo { Title = options.Title, Version = options.Version });
            setup.SchemaFilter<LongIdSchemaFilter>();
            setup.OperationFilter<JwtSecurityDefinitionOperationFilter>();
            setup.OperationFilter<ApiResponseOperationFilter>();

            foreach (var xmlCommentFile in options.XmlCommentFiles)
            {
                setup.IncludeXmlComments(xmlCommentFile);
            }
        });

        services.AddSwaggerGenNewtonsoftSupport();
        return services;
    }
}
