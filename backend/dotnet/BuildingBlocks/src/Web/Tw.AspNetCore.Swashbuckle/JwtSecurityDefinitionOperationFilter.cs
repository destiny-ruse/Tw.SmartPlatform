using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Tw.AspNetCore.Swashbuckle;

/// <summary>表示 JwtSecurityDefinitionOperationFilter 类型</summary>
public sealed class JwtSecurityDefinitionOperationFilter : IOperationFilter
{
    /// <summary>执行 Apply 操作</summary>
    /// <param name="operation">operation 参数</param>
    /// <param name="context">context 参数</param>
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        operation.Security ??= [];
    }
}
