using System.Globalization;

namespace Tw.Core.Extensions;

/// <summary>Provides extension methods for numeric values.</summary>
public static class NumberExtensions
{
    /// <summary>Returns whether an integer is even.</summary>
    public static bool IsEven(this int source) => source % 2 == 0;

    /// <summary>Returns whether an integer is odd.</summary>
    public static bool IsOdd(this int source) => !source.IsEven();

    /// <summary>Returns whether a long integer is even.</summary>
    public static bool IsEven(this long source) => source % 2 == 0;

    /// <summary>Returns whether a long integer is odd.</summary>
    public static bool IsOdd(this long source) => !source.IsEven();

    /// <summary>Restricts an integer to an inclusive range.</summary>
    /// <exception cref="ArgumentException">Thrown when <paramref name="max"/> is less than <paramref name="min"/>.</exception>
    public static int Clamp(this int source, int min, int max)
    {
        ValidateRange(min, max);
        return Math.Min(Math.Max(source, min), max);
    }

    /// <summary>Restricts a long integer to an inclusive range.</summary>
    /// <exception cref="ArgumentException">Thrown when <paramref name="max"/> is less than <paramref name="min"/>.</exception>
    public static long Clamp(this long source, long min, long max)
    {
        ValidateRange(min, max);
        return Math.Min(Math.Max(source, min), max);
    }

    /// <summary>Restricts a double to an inclusive range.</summary>
    /// <exception cref="ArgumentException">Thrown when <paramref name="max"/> is less than <paramref name="min"/>.</exception>
    public static double Clamp(this double source, double min, double max)
    {
        ValidateRange(min, max);
        return Math.Min(Math.Max(source, min), max);
    }

    /// <summary>Restricts a decimal to an inclusive range.</summary>
    /// <exception cref="ArgumentException">Thrown when <paramref name="max"/> is less than <paramref name="min"/>.</exception>
    public static decimal Clamp(this decimal source, decimal min, decimal max)
    {
        ValidateRange(min, max);
        return Math.Min(Math.Max(source, min), max);
    }

    /// <summary>Formats a byte count using binary file size units.</summary>
    /// <param name="source">The byte count.</param>
    /// <param name="decimalPlaces">The number of decimal places.</param>
    /// <returns>The formatted file size.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="decimalPlaces"/> is negative.</exception>
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

    /// <summary>Rounds a double to the specified number of decimal places.</summary>
    public static double Round(this double source, int decimals = 0) => Math.Round(source, decimals);

    /// <summary>Rounds a decimal to the specified number of decimal places.</summary>
    public static decimal Round(this decimal source, int decimals = 0) => Math.Round(source, decimals);

    /// <summary>Formats a double as a percentage string.</summary>
    /// <param name="source">The source value where 1.0 is 100 percent.</param>
    /// <param name="decimals">The number of decimal places.</param>
    /// <returns>The formatted percentage.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="decimals"/> is negative.</exception>
    public static string ToPercentage(this double source, int decimals = 2)
    {
        Check.NonNegative(decimals);
        return (source * 100).ToString($"F{decimals}", CultureInfo.InvariantCulture) + "%";
    }

    /// <summary>Formats a decimal as a percentage string.</summary>
    /// <param name="source">The source value where 1.0 is 100 percent.</param>
    /// <param name="decimals">The number of decimal places.</param>
    /// <returns>The formatted percentage.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="decimals"/> is negative.</exception>
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
            throw new ArgumentException("Maximum value must be greater than or equal to minimum value.", "max");
        }
    }
}
