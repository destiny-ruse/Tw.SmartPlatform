namespace Tw.Excel;

/// <summary>
/// Excel 模板列定义
/// </summary>
/// <param name="FieldName">字段名</param>
/// <param name="HeaderPath">表头路径</param>
/// <param name="DataType">数据类型</param>
/// <param name="Required">是否必填</param>
/// <param name="IsDynamic">是否动态列</param>
public sealed record ExcelColumnDefinition(
    string FieldName,
    string HeaderPath,
    string DataType,
    bool Required = false,
    bool IsDynamic = false);
