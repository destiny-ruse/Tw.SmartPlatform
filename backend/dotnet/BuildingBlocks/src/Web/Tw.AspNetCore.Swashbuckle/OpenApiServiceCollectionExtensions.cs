using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;

namespace Tw.AspNetCore.Swashbuckle;

/// <summary>
/// 封装OpenApi服务CollectionExtensions相关的数据和行为
/// </summary>
public static class OpenApiServiceCollectionExtensions
{
    /// <summary>
    /// 注册OpenApiIntegration所需服务
    /// </summary>
    /// <param name="services">需要注册组件依赖的服务集合</param>
    /// <param name="options">用于配置当前组件行为的选项</param>
    /// <returns>方法完成后返回给调用方的结果对象</returns>
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
