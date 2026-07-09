using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;

namespace Tw.AspNetCore.Swashbuckle;

public static class OpenApiServiceCollectionExtensions
{
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
