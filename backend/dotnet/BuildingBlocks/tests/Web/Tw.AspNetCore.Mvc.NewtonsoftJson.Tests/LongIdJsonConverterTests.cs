using AwesomeAssertions;
using Newtonsoft.Json;
using Tw.AspNetCore.Mvc.NewtonsoftJson;
using Xunit;

namespace Tw.AspNetCore.Mvc.NewtonsoftJson.Tests;

/// <summary>验证 LongIdJsonConverterTests 相关行为</summary>
public sealed class LongIdJsonConverterTests
{
    /// <summary>表示 Sample 声明</summary>
    private sealed record Sample(long Id);

    /// <summary>验证 Serialize_WritesLongAsString 场景</summary>
    [Fact]
    public void Serialize_WritesLongAsString()
    {
        var settings = new JsonSerializerSettings();
        settings.Converters.Add(new LongIdJsonConverter());

        var json = JsonConvert.SerializeObject(new Sample(9007199254740993L), settings);

        json.Should().Contain("\"Id\":\"9007199254740993\"");
    }
}
