using AwesomeAssertions;
using Newtonsoft.Json;
using Tw.AspNetCore.Mvc.NewtonsoftJson;
using Xunit;

namespace Tw.AspNetCore.Mvc.NewtonsoftJson.Tests;

/// <summary>
/// 覆盖 long 标识 JSON 转换器的核心行为和边界条件
/// </summary>
public sealed class LongIdJsonConverterTests
{
    /// <summary>
    /// 封装示例相关的数据和行为
    /// </summary>
    private sealed record Sample(long Id);

    /// <summary>
    /// 验证Serialize写回长整型作为String
    /// </summary>
    [Fact]
    public void Serialize_WritesLongAsString()
    {
        var settings = new JsonSerializerSettings();
        settings.Converters.Add(new LongIdJsonConverter());

        var json = JsonConvert.SerializeObject(new Sample(9007199254740993L), settings);

        json.Should().Contain("\"Id\":\"9007199254740993\"");
    }
}
