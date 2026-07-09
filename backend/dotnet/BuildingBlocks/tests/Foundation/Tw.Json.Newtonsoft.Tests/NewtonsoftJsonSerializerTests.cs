using AwesomeAssertions;
using Tw.Json.Newtonsoft;
using Xunit;

namespace Tw.Json.Newtonsoft.Tests;

/// <summary>验证 NewtonsoftJsonSerializerTests 相关行为</summary>
public sealed class NewtonsoftJsonSerializerTests
{
    /// <summary>表示 Sample 声明</summary>
    private sealed record Sample(long Id);

    /// <summary>验证 Serialize_WritesLongIdAsString 场景</summary>
    [Fact]
    public void Serialize_WritesLongIdAsString()
    {
        var serializer = new NewtonsoftJsonSerializer();

        var json = serializer.Serialize(new Sample(9007199254740993L));

        json.Should().Contain("\"id\":\"9007199254740993\"");
    }
}
