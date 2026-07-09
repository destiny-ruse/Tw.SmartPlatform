using AwesomeAssertions;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using Tw.AspNetCore.Swashbuckle;
using Xunit;

namespace Tw.AspNetCore.Swashbuckle.Tests;

public sealed class LongIdSchemaFilterTests
{
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
