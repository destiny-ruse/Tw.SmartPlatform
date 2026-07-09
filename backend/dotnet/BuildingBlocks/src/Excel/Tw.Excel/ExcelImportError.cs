namespace Tw.Excel;

/// <summary>
/// Excel 导入校验错误
/// </summary>
/// <param name="RowNumber">行号</param>
/// <param name="ColumnName">列名</param>
/// <param name="FieldPath">字段路径</param>
/// <param name="Code">错误编码</param>
/// <param name="Message">错误消息</param>
public sealed record ExcelImportError(
    int RowNumber,
    string ColumnName,
    string FieldPath,
    string Code,
    string Message);
