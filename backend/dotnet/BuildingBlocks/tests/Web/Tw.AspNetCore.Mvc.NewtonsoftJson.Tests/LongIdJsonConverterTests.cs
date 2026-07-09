using AwesomeAssertions;
using Newtonsoft.Json;
using Tw.AspNetCore.Mvc.NewtonsoftJson;
using Xunit;

namespace Tw.AspNetCore.Mvc.NewtonsoftJson.Tests;

public sealed class LongIdJsonConverterTests
{
    private sealed record Sample(long Id);

    [Fact]
    public void Serialize_WritesLongAsString()
    {
        var settings = new JsonSerializerSettings();
        settings.Converters.Add(new LongIdJsonConverter());

        var json = JsonConvert.SerializeObject(new Sample(9007199254740993L), settings);

        json.Should().Contain("\"Id\":\"9007199254740993\"");
    }
}
