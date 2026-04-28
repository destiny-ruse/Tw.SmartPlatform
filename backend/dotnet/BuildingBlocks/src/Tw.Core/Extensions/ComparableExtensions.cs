namespace Tw.Core.Extensions;

/// <summary>提供可比较值的扩展方法</summary>
public static class ComparableExtensions
{
    /// <summary>返回值是否位于闭区间内</summary>
    /// <typeparam name="T">可比较值类型</typeparam>
    /// <param name="value">要比较的值</param>
    /// <param name="minInclusiveValue">闭区间下界</param>
    /// <param name="maxInclusiveValue">闭区间上界</param>
    /// <returns><paramref name="value"/> 位于边界之间时返回 <see langword="true"/>；否则返回 <see langword="false"/></returns>
    public static bool IsBetween<T>(this T value, T minInclusiveValue, T maxInclusiveValue)
        where T : IComparable<T>
    {
        return value.CompareTo(minInclusiveValue) >= 0 && value.CompareTo(maxInclusiveValue) <= 0;
    }
}
