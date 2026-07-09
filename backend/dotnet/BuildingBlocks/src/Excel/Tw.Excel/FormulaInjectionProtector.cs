namespace Tw.Excel;

/// <summary>
/// Excel 公式注入保护器
/// </summary>
public static class FormulaInjectionProtector
{
    /// <summary>表示 FormulaPrefixes 字段</summary>
    private static readonly char[] FormulaPrefixes = ['=', '+', '-', '@'];

    /// <summary>
    /// 保护用户文本，避免被 Excel 当作公式执行
    /// </summary>
    /// <param name="value">用户文本</param>
    /// <returns>受保护的用户文本</returns>
    public static string Protect(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return FormulaPrefixes.Contains(value[0]) ? "'" + value : value;
    }
}
