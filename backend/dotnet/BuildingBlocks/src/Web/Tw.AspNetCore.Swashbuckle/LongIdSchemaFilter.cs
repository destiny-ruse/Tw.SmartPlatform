using System.Text.Json.Nodes;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Tw.AspNetCore.Swashbuckle;

/// <summary>表示 LongIdSchemaFilter 类型</summary>
public sealed class LongIdSchemaFilter : ISchemaFilter
{
    /// <summary>执行 Apply 操作</summary>
    /// <param name="schema">schema 参数</param>
    /// <param name="context">context 参数</param>
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
