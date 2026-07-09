using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Tw.AspNetCore.Swashbuckle;

public sealed class JwtSecurityDefinitionOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        operation.Security ??= [];
    }
}
