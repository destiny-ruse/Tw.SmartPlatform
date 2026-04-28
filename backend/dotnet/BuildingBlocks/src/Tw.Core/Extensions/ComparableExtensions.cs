namespace Tw.Core.Extensions;

/// <summary>Provides extension methods for comparable values.</summary>
public static class ComparableExtensions
{
    /// <summary>Returns whether a value is within an inclusive range.</summary>
    /// <typeparam name="T">The comparable value type.</typeparam>
    /// <param name="value">The value to compare.</param>
    /// <param name="minInclusiveValue">The inclusive lower bound.</param>
    /// <param name="maxInclusiveValue">The inclusive upper bound.</param>
    /// <returns><see langword="true"/> when <paramref name="value"/> is between the bounds; otherwise, <see langword="false"/>.</returns>
    public static bool IsBetween<T>(this T value, T minInclusiveValue, T maxInclusiveValue)
        where T : IComparable<T>
    {
        return value.CompareTo(minInclusiveValue) >= 0 && value.CompareTo(maxInclusiveValue) <= 0;
    }
}
