using AwesomeAssertions;
using Tw.Exceptions;
using Tw.Localization.Json;
using Xunit;

namespace Tw.Localization.Tests;

public class JsonTextResourceParserTests
{
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

    [Fact]
    public void Parse_RejectsNonStringLeaf()
    {
        const string json = """{ "culture": "zh-Hans", "texts": { "Count": 1 } }""";

        var act = () => JsonTextResourceParser.Parse("App", "bad.json", json);

        act.Should().Throw<TwConfigurationException>();
    }
}
