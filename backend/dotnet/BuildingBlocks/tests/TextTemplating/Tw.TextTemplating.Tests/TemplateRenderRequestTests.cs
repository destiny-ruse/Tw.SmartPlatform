using AwesomeAssertions;
using Tw.TextTemplating;
using Xunit;

namespace Tw.TextTemplating.Tests;

/// <summary>
/// 覆盖模板Render请求的核心行为和边界条件
/// </summary>
public sealed class TemplateRenderRequestTests
{
    /// <summary>
    /// 验证创建File模板StoresSource和Variables
    /// </summary>
    [Fact]
    public void Create_FileTemplate_StoresSourceAndVariables()
    {
        var request = new TemplateRenderRequest(
            TemplateSourceKind.File,
            "invoices/monthly.sbn",
            new Dictionary<string, object?> { ["tenantId"] = "tenant-a" });

        request.SourceKind.Should().Be(TemplateSourceKind.File);
        request.Source.Should().Be("invoices/monthly.sbn");
        request.Variables["tenantId"].Should().Be("tenant-a");
    }
}
