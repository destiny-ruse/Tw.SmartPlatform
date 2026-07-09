using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Tw.AspNetCore.Swashbuckle;

/// <summary>表示 ApiResponseOperationFilter 类型</summary>
public sealed class ApiResponseOperationFilter : IOperationFilter
{
    /// <summary>执行 Apply 操作</summary>
    /// <param name="operation">operation 参数</param>
    /// <param name="context">context 参数</param>
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        operation.Responses ??= new OpenApiResponses();
        operation.Responses.TryAdd("400", new OpenApiResponse { Description = "Bad request" });
        operation.Responses.TryAdd("409", new OpenApiResponse { Description = "Conflict" });
        operation.Responses.TryAdd("500", new OpenApiResponse { Description = "Server error" });
    }
}
