using AwesomeAssertions;
using Tw.TextTemplating;
using Tw.TextTemplating.Scriban;
using Xunit;

namespace Tw.TextTemplating.Scriban.Tests;

/// <summary>验证 ScribanTemplateRendererTests 相关行为</summary>
public sealed class ScribanTemplateRendererTests
{
    /// <summary>验证 RenderAsync_StringTemplate_UsesRequestVariables 场景</summary>
    /// <returns>RenderAsync_StringTemplate_UsesRequestVariables 的执行结果</returns>
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
