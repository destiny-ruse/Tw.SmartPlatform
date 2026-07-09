using AwesomeAssertions;
using Tw.Json.Newtonsoft;
using Xunit;

namespace Tw.Json.Newtonsoft.Tests;

public sealed class NewtonsoftJsonSerializerTests
{
    private sealed record Sample(long Id);

    [Fact]
    public void Serialize_WritesLongIdAsString()
    {
        var serializer = new NewtonsoftJsonSerializer();

        var json = serializer.Serialize(new Sample(9007199254740993L));

        json.Should().Contain("\"id\":\"9007199254740993\"");
    }
}
