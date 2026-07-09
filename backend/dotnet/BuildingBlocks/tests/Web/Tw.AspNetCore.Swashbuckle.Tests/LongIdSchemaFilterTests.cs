using AwesomeAssertions;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using Tw.AspNetCore.Swashbuckle;
using Xunit;

namespace Tw.AspNetCore.Swashbuckle.Tests;

/// <summary>验证 LongIdSchemaFilterTests 相关行为</summary>
public sealed class LongIdSchemaFilterTests
{
    /// <summary>验证 Apply_MapsLongToStringInt64 场景</summary>
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
