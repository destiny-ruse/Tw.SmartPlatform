using System.Text.Json.Nodes;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Tw.AspNetCore.Swashbuckle;

/// <summary>
/// 封装长整型标识架构过滤器相关的数据和行为
/// </summary>
public sealed class LongIdSchemaFilter : ISchemaFilter
{
    /// <summary>
    /// 将当前过滤器规则应用到 OpenAPI 文档上下文
    /// </summary>
    /// <param name="schema">当前正在生成或调整的 OpenAPI schema</param>
    /// <param name="context">当前调用携带的上下文信息</param>
    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        if (context.Type == typeof(long) || context.Type == typeof(long?))
        {
            if (schema is not OpenApiSchema openApiSchema)
            {
                return;
            }

            openApiSchema.Type = JsonSchemaType.String;
            openApiSchema.Format = "int64";
            openApiSchema.Extensions ??= new Dictionary<string, IOpenApiExtension>(StringComparer.Ordinal);
            openApiSchema.Extensions["x-tw-id"] = new JsonNodeExtension(JsonValue.Create(true)!);
        }
    }
}
