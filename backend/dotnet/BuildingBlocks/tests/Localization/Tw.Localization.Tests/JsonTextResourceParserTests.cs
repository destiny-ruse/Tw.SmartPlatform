using AwesomeAssertions;
using System.Text.Json;
using Tw.Localization.Json;
using Xunit;

namespace Tw.Localization.Tests;

/// <summary>
/// 覆盖JSONText资源Parser的核心行为和边界条件
/// </summary>
public class JsonTextResourceParserTests
{
    /// <summary>
    /// 验证ParseFlattensNestedObjects
    /// </summary>
    [Fact]
    public void Parse_FlattensNestedObjects()
    {
        const string json = """
        {
          "culture": "zh-Hans",
          "texts": {
            "Menu": { "Dashboard": "控制台" },
            "Validation__Required": "必填"
          }
        }
        """;

        var resource = JsonTextResourceParser.Parse("App", "app.zh-Hans.json", json);

        resource.CultureName.Should().Be("zh-Hans");
        resource.Texts["Menu__Dashboard"].Should().Be("控制台");
        resource.Texts["Validation__Required"].Should().Be("必填");
    }

    /// <summary>
    /// 验证Parse拒绝NonStringLeaf
    /// </summary>
    [Fact]
    public void Parse_RejectsNonStringLeaf()
    {
        const string json = """{ "culture": "zh-Hans", "texts": { "Count": 1 } }""";

        var act = () => JsonTextResourceParser.Parse("App", "bad.json", json);

        act.Should().Throw<LocalizationConfigurationException>();
    }

    /// <summary>
    /// 验证畸形 JSON 保留底层解析异常作为诊断上下文
    /// </summary>
    [Fact]
    public void Parse_WithMalformedJson_PreservesJsonExceptionAsInnerException()
    {
        const string json = """{ "culture": "zh-Hans", "texts": { "Greeting": "你好" }""";
        var act = () => JsonTextResourceParser.Parse("App", "malformed.json", json);

        var exception = act.Should().Throw<LocalizationConfigurationException>().Which;

        exception.InnerException.Should().BeAssignableTo<JsonException>();
    }
}
