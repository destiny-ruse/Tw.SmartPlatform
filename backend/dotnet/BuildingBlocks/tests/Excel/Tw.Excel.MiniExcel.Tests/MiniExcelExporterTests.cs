using AwesomeAssertions;
using Tw.Excel.MiniExcel;
using Xunit;

namespace Tw.Excel.MiniExcel.Tests;

/// <summary>
/// 覆盖MiniExcelExporter的核心行为和边界条件
/// </summary>
public sealed class MiniExcelExporterTests
{
    /// <summary>
    /// 验证ExportBlank模板异步写回WorkbookStream
    /// </summary>
    /// <returns>表示异步流程完成状态的任务</returns>
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
