using AwesomeAssertions;
using Tw.TextTemplating;
using Xunit;

namespace Tw.TextTemplating.Tests;

/// <summary>验证 TemplateRenderRequestTests 相关行为</summary>
public sealed class TemplateRenderRequestTests
{
    /// <summary>验证 Create_FileTemplate_StoresSourceAndVariables 场景</summary>
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
