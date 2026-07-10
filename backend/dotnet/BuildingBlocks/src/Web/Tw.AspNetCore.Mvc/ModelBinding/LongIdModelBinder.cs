using System.Globalization;

namespace Tw.AspNetCore.Mvc.ModelBinding;

/// <summary>
/// 封装长整型标识模型绑定器相关的数据和行为
/// </summary>
public sealed class LongIdModelBinder
{
    /// <summary>
    /// 尝试将输入文本解析为长整型标识
    /// </summary>
    /// <param name="value">用于转换、回显或断言的输入值</param>
    /// <param name="id">解析得到的长整型标识</param>
    /// <returns>条件满足时返回 <see langword="true"/></returns>
    public static bool TryParse(string? value, out long id)
    {
        return long.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out id);
    }
}
