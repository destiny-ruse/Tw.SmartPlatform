using AwesomeAssertions;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using Tw.AspNetCore.Swashbuckle;
using Xunit;

namespace Tw.AspNetCore.Swashbuckle.Tests;

/// <summary>
/// 覆盖长整型标识架构过滤器的核心行为和边界条件
/// </summary>
public sealed class LongIdSchemaFilterTests
{
    /// <summary>
    /// 验证Apply映射长整型到StringInt64
    /// </summary>
    [Fact]
    public void Apply_MapsLongToStringInt64()
    {
        var schema = new OpenApiSchema { Type = JsonSchemaType.Integer, Format = "int64" };
        var filter = new LongIdSchemaFilter();

        filter.Apply(schema, new SchemaFilterContext(typeof(long), null, null, null));

        schema.Type.Should().Be(JsonSchemaType.String);
        schema.Format.Should().Be("int64");
        schema.Extensions.Should().ContainKey("x-tw-id");
    }
}
