namespace Tw.Excel;

/// <summary>
/// Excel 导出器
/// </summary>
public interface IExcelExporter
{
    /// <summary>
    /// 导出空白 Excel 模板
    /// </summary>
    /// <param name="stream">输出流</param>
    /// <param name="template">模板定义</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>异步导出任务</returns>
    Task ExportBlankTemplateAsync(
        Stream stream,
        ExcelTemplateDefinition template,
        CancellationToken cancellationToken);
}
