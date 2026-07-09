using AwesomeAssertions;
using Tw.Excel.MiniExcel;
using Xunit;

namespace Tw.Excel.MiniExcel.Tests;

public sealed class MiniExcelExporterTests
{
    [Fact]
    public async Task ExportBlankTemplateAsync_WritesWorkbookStream()
    {
        var exporter = new MiniExcelExporter();
        var template = ExcelTemplateDefinition.Create(
            "invoice",
            [new ExcelColumnDefinition("name", "名称", "string", Required: true)],
            maxDynamicColumns: 100);
        using var stream = new MemoryStream();

        await exporter.ExportBlankTemplateAsync(stream, template, TestContext.Current.CancellationToken);

        stream.Length.Should().BeGreaterThan(0);
        stream.Position = 0;
        stream.ReadByte().Should().Be('P');
        stream.ReadByte().Should().Be('K');
    }
}
