using AwesomeAssertions;
using Tw.Json.Newtonsoft;
using Xunit;

namespace Tw.Json.Newtonsoft.Tests;

/// <summary>
/// 覆盖NewtonsoftJSONSerializer的核心行为和边界条件
/// </summary>
public sealed class NewtonsoftJsonSerializerTests
{
    /// <summary>
    /// 封装示例相关的数据和行为
    /// </summary>
    private sealed record Sample(long Id);

    /// <summary>
    /// 验证Serialize写回长整型标识作为String
    /// </summary>
    [Fact]
    public void Serialize_WritesLongIdAsString()
    {
        var serializer = new NewtonsoftJsonSerializer();

        var json = serializer.Serialize(new Sample(9007199254740993L));

        json.Should().Contain("\"id\":\"9007199254740993\"");
    }
}
