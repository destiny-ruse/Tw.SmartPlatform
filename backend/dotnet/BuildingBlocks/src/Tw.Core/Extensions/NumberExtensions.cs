using System.Globalization;

namespace Tw.Core.Extensions;

/// <summary>提供数值扩展方法</summary>
public static class NumberExtensions
{
    /// <summary>返回整数是否为偶数</summary>
    public static bool IsEven(this int source) => source % 2 == 0;

    /// <summary>返回整数是否为奇数</summary>
    public static bool IsOdd(this int source) => !source.IsEven();

    /// <summary>返回长整数是否为偶数</summary>
    public static bool IsEven(this long source) => source % 2 == 0;

    /// <summary>返回长整数是否为奇数</summary>
    public static bool IsOdd(this long source) => !source.IsEven();

    /// <summary>将整数限制在闭区间内</summary>
    /// <exception cref="ArgumentException">当 <paramref name="max"/> 小于 <paramref name="min"/> 时抛出</exception>
    public static int Clamp(this int source, int min, int max)
    {
        ValidateRange(min, max);
        return Math.Min(Math.Max(source, min), max);
    }

    /// <summary>将长整数限制在闭区间内</summary>
    /// <exception cref="ArgumentException">当 <paramref name="max"/> 小于 <paramref name="min"/> 时抛出</exception>
    public static long Clamp(this long source, long min, long max)
    {
        ValidateRange(min, max);
        return Math.Min(Math.Max(source, min), max);
    }

    /// <summary>将双精度浮点数限制在闭区间内</summary>
    /// <exception cref="ArgumentException">当 <paramref name="max"/> 小于 <paramref name="min"/> 时抛出</exception>
    public static double Clamp(this double source, double min, double max)
    {
        ValidateRange(min, max);
        return Math.Min(Math.Max(source, min), max);
    }

    /// <summary>将十进制数限制在闭区间内</summary>
    /// <exception cref="ArgumentException">当 <paramref name="max"/> 小于 <paramref name="min"/> 时抛出</exception>
    public static decimal Clamp(this decimal source, decimal min, decimal max)
    {
        ValidateRange(min, max);
        return Math.Min(Math.Max(source, min), max);
    }

    /// <summary>使用二进制文件大小单位格式化字节数</summary>
    /// <param name="source">字节数</param>
    /// <param name="decimalPlaces">小数位数</param>
    /// <returns>格式化后的文件大小</returns>
    /// <exception cref="ArgumentOutOfRangeException">当 <paramref name="decimalPlaces"/> 为负数时抛出</exception>
    public static string ToFileSize(this long source, int decimalPlaces = 2)
    {
        Check.NonNegative(decimalPlaces);

        string[] units = ["B", "KB", "MB", "GB", "TB", "PB"];
        var size = (double)source;
        var unitIndex = 0;

        while (Math.Abs(size) >= 1024 && unitIndex < units.Length - 1)
        {
            size /= 1024;
            unitIndex++;
        }

        return $"{size.ToString($"F{decimalPlaces}", CultureInfo.InvariantCulture)} {units[unitIndex]}";
    }

    /// <summary>将双精度浮点数舍入到指定小数位数</summary>
    public static double Round(this double source, int decimals = 0) => Math.Round(source, decimals);

    /// <summary>将十进制数舍入到指定小数位数</summary>
    public static decimal Round(this decimal source, int decimals = 0) => Math.Round(source, decimals);

    /// <summary>将双精度浮点数格式化为百分比字符串</summary>
    /// <param name="source">源值，其中 1.0 表示 100%</param>
    /// <param name="decimals">小数位数</param>
    /// <returns>格式化后的百分比</returns>
    /// <exception cref="ArgumentOutOfRangeException">当 <paramref name="decimals"/> 为负数时抛出</exception>
    public static string ToPercentage(this double source, int decimals = 2)
    {
        Check.NonNegative(decimals);
        return (source * 100).ToString($"F{decimals}", CultureInfo.InvariantCulture) + "%";
    }

    /// <summary>将十进制数格式化为百分比字符串</summary>
    /// <param name="source">源值，其中 1.0 表示 100%</param>
    /// <param name="decimals">小数位数</param>
    /// <returns>格式化后的百分比</returns>
    /// <exception cref="ArgumentOutOfRangeException">当 <paramref name="decimals"/> 为负数时抛出</exception>
    public static string ToPercentage(this decimal source, int decimals = 2)
    {
        Check.NonNegative(decimals);
        return (source * 100).ToString($"F{decimals}", CultureInfo.InvariantCulture) + "%";
    }

    private static void ValidateRange<T>(T min, T max)
        where T : IComparable<T>
    {
        if (max.CompareTo(min) < 0)
        {
            throw new ArgumentException("最大值必须大于或等于最小值。", "max");
        }
    }
}
