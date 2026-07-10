using AwesomeAssertions;
using Tw.TextTemplating;
using Tw.TextTemplating.Scriban;
using Xunit;

namespace Tw.TextTemplating.Scriban.Tests;

/// <summary>
/// 覆盖Scriban模板Renderer的核心行为和边界条件
/// </summary>
public sealed class ScribanTemplateRendererTests
{
    /// <summary>
    /// 验证Render异步String模板Uses请求Variables
    /// </summary>
    /// <returns>表示异步流程完成状态的任务</returns>
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
