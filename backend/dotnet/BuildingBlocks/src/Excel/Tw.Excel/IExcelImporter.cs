namespace Tw.Excel;

/// <summary>
/// Excel 导入器
/// </summary>
public interface IExcelImporter
{
    /// <summary>
    /// 根据模板校验 Excel 输入流
    /// </summary>
    /// <param name="stream">Excel 输入流</param>
    /// <param name="template">模板定义</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>导入校验错误集合</returns>
    Task<IReadOnlyList<ExcelImportError>> ValidateAsync(
        Stream stream,
        ExcelTemplateDefinition template,
        CancellationToken cancellationToken);
}
