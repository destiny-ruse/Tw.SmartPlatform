namespace Tw.Core.Extensions;

/// <summary>提供 GUID 值的扩展方法</summary>
public static class GuidExtensions
{
    /// <summary>返回可空 GUID 是否为 <see langword="null"/> 或空值</summary>
    /// <param name="value">可空 GUID 值</param>
    /// <returns>当 <paramref name="value"/> 为 <see langword="null"/> 或 <see cref="Guid.Empty"/> 时返回 <see langword="true"/></returns>
    public static bool IsNullOrEmpty(this Guid? value)
    {
        return value is null || value.Value == Guid.Empty;
    }

    /// <summary>使用紧凑的 N 格式格式化 GUID</summary>
    /// <param name="value">GUID 值</param>
    /// <returns>不带分隔符的 GUID</returns>
    public static string ToNString(this Guid value)
    {
        return value.ToString("N");
    }
}
