using DocumentFormat.OpenXml.Packaging;
using MiniExcelLibs;

namespace Tw.Excel.MiniExcel;

/// <summary>
/// 基于 MiniExcel 和 OpenXML 的 Excel 导出器
/// </summary>
public sealed class MiniExcelExporter : IExcelExporter
{
    /// <inheritdoc />
    public async Task ExportBlankTemplateAsync(
        Stream stream,
        ExcelTemplateDefinition template,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(template);
        cancellationToken.ThrowIfCancellationRequested();

        var rows = new[]
        {
            template.Columns.ToDictionary(
                column => column.HeaderPath,
                _ => (object?)string.Empty),
        };

        await MiniExcelLibs.MiniExcel.SaveAsAsync(
                stream,
                rows,
                true,
                template.Name,
                ExcelType.XLSX,
                null,
                cancellationToken)
            .ConfigureAwait(false);

        PostProcessBlankTemplate(stream);
    }

    /// <summary>执行 PostProcessBlankTemplate 操作</summary>
    /// <param name="stream">stream 参数</param>
    private static void PostProcessBlankTemplate(Stream stream)
    {
        if (!stream.CanSeek)
        {
            return;
        }

        stream.Position = 0;
        using var document = SpreadsheetDocument.Open(stream, true);
        if (document.WorkbookPart?.Workbook is { } workbook)
        {
            workbook.Save();
        }

        stream.Position = 0;
    }
}
