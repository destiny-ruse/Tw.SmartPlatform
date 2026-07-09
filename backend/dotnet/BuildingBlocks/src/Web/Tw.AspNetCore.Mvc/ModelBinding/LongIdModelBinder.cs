using System.Globalization;

namespace Tw.AspNetCore.Mvc.ModelBinding;

/// <summary>表示 LongIdModelBinder 类型</summary>
public sealed class LongIdModelBinder
{
    /// <summary>执行 TryParse 操作</summary>
    /// <param name="value">value 参数</param>
    /// <param name="id">id 参数</param>
    /// <returns>TryParse 的执行结果</returns>
    public static bool TryParse(string? value, out long id)
    {
        return long.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out id);
    }
}
