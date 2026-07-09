namespace Tw.Excel;

/// <summary>
/// Excel 模板定义异常
/// </summary>
public sealed class ExcelTemplateException : Exception
{
    /// <summary>
    /// 创建 Excel 模板定义异常
    /// </summary>
    /// <param name="message">异常消息</param>
    public ExcelTemplateException(string message)
        : base(message)
    {
    }
}

/// <summary>
/// Excel 模板定义
/// </summary>
/// <param name="Name">模板名称</param>
/// <param name="Columns">列定义集合</param>
public sealed record ExcelTemplateDefinition(string Name, IReadOnlyList<ExcelColumnDefinition> Columns)
{
    /// <summary>
    /// 创建 Excel 模板定义
    /// </summary>
    /// <param name="name">模板名称</param>
    /// <param name="columns">列定义集合</param>
    /// <param name="maxDynamicColumns">动态列数量上限</param>
    /// <returns>Excel 模板定义</returns>
    /// <exception cref="ArgumentNullException">columns 为 null 时抛出</exception>
    /// <exception cref="ExcelTemplateException">动态列数量超过上限时抛出</exception>
    public static ExcelTemplateDefinition Create(
        string name,
        IReadOnlyList<ExcelColumnDefinition> columns,
        int maxDynamicColumns)
    {
        ArgumentNullException.ThrowIfNull(columns);
        if (columns.Count(column => column.IsDynamic) > maxDynamicColumns)
        {
            throw new ExcelTemplateException("动态列数量超过配置上限");
        }

        return new ExcelTemplateDefinition(name, columns);
    }
}
