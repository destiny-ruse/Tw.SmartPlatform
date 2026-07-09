using AwesomeAssertions;
using Tw.TextTemplating;
using Xunit;

namespace Tw.TextTemplating.Tests;

public sealed class TemplateRenderRequestTests
{
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
