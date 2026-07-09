using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Tw.AspNetCore.Swashbuckle;

public sealed class ApiResponseOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        operation.Responses ??= new OpenApiResponses();
        operation.Responses.TryAdd("400", new OpenApiResponse { Description = "Bad request" });
        operation.Responses.TryAdd("409", new OpenApiResponse { Description = "Conflict" });
        operation.Responses.TryAdd("500", new OpenApiResponse { Description = "Server error" });
    }
}
