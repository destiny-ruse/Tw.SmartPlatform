using AwesomeAssertions;
using Tw.TextTemplating;
using Tw.TextTemplating.Scriban;
using Xunit;

namespace Tw.TextTemplating.Scriban.Tests;

public sealed class ScribanTemplateRendererTests
{
    [Fact]
    public async Task RenderAsync_StringTemplate_UsesRequestVariables()
    {
        var renderer = new ScribanTemplateRenderer();
        var request = new TemplateRenderRequest(
            TemplateSourceKind.String,
            "你好 {{ name }}",
            new Dictionary<string, object?> { ["name"] = "天问" });

        var result = await renderer.RenderAsync(request, TestContext.Current.CancellationToken);

        result.Success.Should().BeTrue();
        result.Content.Should().Be("你好 天问");
    }
}
