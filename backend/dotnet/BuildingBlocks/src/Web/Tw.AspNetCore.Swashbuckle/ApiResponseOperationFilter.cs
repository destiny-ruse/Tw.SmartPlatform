using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Tw.AspNetCore.Swashbuckle;

/// <summary>
/// 封装Api响应业务委托过滤器相关的数据和行为
/// </summary>
public sealed class ApiResponseOperationFilter : IOperationFilter
{
    /// <summary>
    /// 将当前过滤器规则应用到 OpenAPI 文档上下文
    /// </summary>
    /// <param name="operation">需要在幂等保护下运行的业务委托</param>
    /// <param name="context">当前调用携带的上下文信息</param>
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        operation.Responses ??= new OpenApiResponses();
        operation.Responses.TryAdd("400", new OpenApiResponse { Description = "Bad request" });
        operation.Responses.TryAdd("409", new OpenApiResponse { Description = "Conflict" });
        operation.Responses.TryAdd("500", new OpenApiResponse { Description = "Server error" });
    }
}
