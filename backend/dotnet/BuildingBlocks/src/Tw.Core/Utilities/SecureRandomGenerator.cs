using System.Security.Cryptography;
using Tw.Core;

namespace Tw.Core.Utilities;

/// <summary>
/// Provides cryptographically secure random values and collection helpers.
/// </summary>
public static class SecureRandomGenerator
{
    private const string LowercaseChars = "abcdefghijklmnopqrstuvwxyz";
    private const string UppercaseChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private const string NumericChars = "0123456789";
    private const string AlphaChars = LowercaseChars + UppercaseChars;
    private const string AlphanumericChars = AlphaChars + NumericChars;
    private const string SpecialChars = "!@#$%^&*()_+-=[]{}|;:,.<>?";

    /// <summary>
    /// Returns a random integer in the half-open range [<paramref name="minValue"/>, <paramref name="maxValue"/>).
    /// </summary>
    /// <param name="minValue">The inclusive lower bound.</param>
    /// <param name="maxValue">The exclusive upper bound.</param>
    /// <returns>A random integer within the requested range.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="minValue"/> is greater than or equal to <paramref name="maxValue"/>.</exception>
    public static int GetInt(int minValue, int maxValue)
    {
        EnsureMinLessThanMax(minValue, maxValue, nameof(maxValue));

        return RandomNumberGenerator.GetInt32(minValue, maxValue);
    }

    /// <summary>
    /// Returns a random integer in the half-open range [0, <paramref name="maxValue"/>).
    /// </summary>
    /// <param name="maxValue">The exclusive upper bound.</param>
    /// <returns>A random integer within the requested range.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="maxValue"/> is less than or equal to zero.</exception>
    public static int GetInt(int maxValue)
    {
        Check.Positive(maxValue);

        return RandomNumberGenerator.GetInt32(maxValue);
    }

    /// <summary>
    /// Returns a random long integer in the half-open range [<paramref name="minValue"/>, <paramref name="maxValue"/>).
    /// </summary>
    /// <param name="minValue">The inclusive lower bound.</param>
    /// <param name="maxValue">The exclusive upper bound.</param>
    /// <returns>A random long integer within the requested range.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="minValue"/> is greater than or equal to <paramref name="maxValue"/>.</exception>
    /// <remarks>Uses rejection sampling to avoid modulo bias across the requested range.</remarks>
    public static long GetLong(long minValue, long maxValue)
    {
        EnsureMinLessThanMax(minValue, maxValue, nameof(maxValue));

        var range = unchecked((ulong)(maxValue - minValue));
        var threshold = unchecked(0UL - range) % range;
        Span<byte> bytes = stackalloc byte[sizeof(ulong)];

        while (true)
        {
            RandomNumberGenerator.Fill(bytes);
            var candidate = BitConverter.ToUInt64(bytes);

            if (candidate >= threshold)
            {
                var offset = candidate % range;
                return unchecked(minValue + (long)offset);
            }
        }
    }

    /// <summary>
    /// Returns a random double in the half-open range [0.0, 1.0).
    /// </summary>
    /// <returns>A random double within the requested range.</returns>
    public static double GetDouble()
    {
        Span<byte> bytes = stackalloc byte[sizeof(ulong)];
        RandomNumberGenerator.Fill(bytes);

        var randomBits = BitConverter.ToUInt64(bytes) >> 11;
        return randomBits / (double)(1UL << 53);
    }

    /// <summary>
    /// Returns a random double in the half-open range [<paramref name="minValue"/>, <paramref name="maxValue"/>).
    /// </summary>
    /// <param name="minValue">The inclusive lower bound.</param>
    /// <param name="maxValue">The exclusive upper bound.</param>
    /// <returns>A random double within the requested range.</returns>
    /// <exception cref="ArgumentException">Thrown when a bound is not finite, the lower bound is greater than or equal to the upper bound, or the range span is not finite.</exception>
    public static double GetDouble(double minValue, double maxValue)
    {
        EnsureFinite(minValue, nameof(minValue));
        EnsureFinite(maxValue, nameof(maxValue));
        EnsureMinLessThanMax(minValue, maxValue, nameof(maxValue));

        var range = maxValue - minValue;
        EnsureFinite(range, nameof(maxValue));

        var result = minValue + (GetDouble() * range);
        if (result < maxValue)
        {
            return result;
        }

        var clamped = Math.BitDecrement(maxValue);
        return clamped < minValue ? minValue : clamped;
    }

    /// <summary>
    /// Returns a random Boolean value.
    /// </summary>
    /// <returns>A random Boolean value.</returns>
    public static bool GetBool()
    {
        return GetInt(2) == 0;
    }

    /// <summary>
    /// Returns a byte array filled with cryptographically secure random bytes.
    /// </summary>
    /// <param name="length">The requested byte count.</param>
    /// <returns>A new byte array with the requested length.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="length"/> is negative.</exception>
    public static byte[] GetBytes(int length)
    {
        Check.NonNegative(length);

        if (length == 0)
        {
            return [];
        }

        var bytes = new byte[length];
        RandomNumberGenerator.Fill(bytes);
        return bytes;
    }

    /// <summary>
    /// Returns a random string using the supplied character source or the default alphanumeric source.
    /// </summary>
    /// <param name="length">The requested string length.</param>
    /// <param name="chars">The optional character source. When omitted, alphanumeric characters are used.</param>
    /// <returns>A random string with the requested length.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="length"/> is negative.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="chars"/> is empty.</exception>
    public static string GetString(int length, string? chars = null)
    {
        Check.NonNegative(length);

        var source = chars ?? AlphanumericChars;
        if (source.Length == 0)
        {
            throw new ArgumentException("Character source cannot be empty.", nameof(chars));
        }

        return new string(Enumerable.Range(0, length)
            .Select(_ => source[GetInt(source.Length)])
            .ToArray());
    }

    /// <summary>
    /// Returns a random numeric string.
    /// </summary>
    /// <param name="length">The requested string length.</param>
    /// <returns>A numeric string with the requested length.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="length"/> is negative.</exception>
    public static string GetNumericString(int length)
    {
        return GetString(length, NumericChars);
    }

    /// <summary>
    /// Returns a random alphabetic string.
    /// </summary>
    /// <param name="length">The requested string length.</param>
    /// <param name="upperCase">Whether to use uppercase letters; otherwise lowercase letters are used.</param>
    /// <returns>An alphabetic string with the requested length.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="length"/> is negative.</exception>
    public static string GetAlphaString(int length, bool upperCase = true)
    {
        return GetString(length, upperCase ? UppercaseChars : LowercaseChars);
    }

    /// <summary>
    /// Returns a random alphanumeric string.
    /// </summary>
    /// <param name="length">The requested string length.</param>
    /// <returns>An alphanumeric string with the requested length.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="length"/> is negative.</exception>
    public static string GetAlphanumericString(int length)
    {
        return GetString(length, AlphanumericChars);
    }

    /// <summary>
    /// Returns a random password that contains each required character category.
    /// </summary>
    /// <param name="length">The requested password length.</param>
    /// <param name="includeSpecialChars">Whether to require and allow special characters.</param>
    /// <returns>A password with the requested length and required character categories.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="length"/> is too short for the required categories.</exception>
    public static string GetStrongPassword(int length = 16, bool includeSpecialChars = true)
    {
        var requiredLength = includeSpecialChars ? 4 : 3;
        if (length < requiredLength)
        {
            throw new ArgumentOutOfRangeException(
                nameof(length),
                length,
                $"Length must be at least {requiredLength}.");
        }

        var chars = new List<char>
        {
            GetRandomChar(LowercaseChars),
            GetRandomChar(UppercaseChars),
            GetRandomChar(NumericChars)
        };

        var allowedChars = AlphanumericChars;
        if (includeSpecialChars)
        {
            chars.Add(GetRandomChar(SpecialChars));
            allowedChars += SpecialChars;
        }

        while (chars.Count < length)
        {
            chars.Add(GetRandomChar(allowedChars));
        }

        return new string(Shuffle(chars).ToArray());
    }

    /// <summary>
    /// Returns a new string containing the source characters in random order, or an empty string when the input is empty.
    /// </summary>
    /// <param name="value">The string to shuffle.</param>
    /// <returns>A shuffled copy of <paramref name="value"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <see langword="null"/>.</exception>
    public static string Shuffle(string value)
    {
        Check.NotNull(value);
        if (value.Length == 0)
        {
            return string.Empty;
        }

        return new string(Shuffle(value.ToCharArray()).ToArray());
    }

    /// <summary>
    /// Returns one random element from a collection.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="collection">The source collection.</param>
    /// <returns>A random element from <paramref name="collection"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="collection"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="collection"/> is empty.</exception>
    public static T GetRandomElement<T>(IList<T> collection)
    {
        var source = Check.NotNull(collection);
        if (source.Count == 0)
        {
            throw new ArgumentException("Collection cannot be empty.", nameof(collection));
        }

        return source[GetInt(source.Count)];
    }

    /// <summary>
    /// Returns a random selection of distinct values from a collection without modifying the input.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="collection">The source collection.</param>
    /// <param name="count">The number of distinct values to select.</param>
    /// <returns>A new list containing the selected distinct values.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="collection"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="collection"/> is empty.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="count"/> is negative or greater than the distinct source value count.</exception>
    public static IList<T> GetRandomElements<T>(IList<T> collection, int count)
    {
        var source = Check.NotNull(collection);
        if (source.Count == 0)
        {
            throw new ArgumentException("Collection cannot be empty.", nameof(collection));
        }

        Check.NonNegative(count);

        var distinctValues = source.Distinct(EqualityComparer<T>.Default).ToList();
        if (count > distinctValues.Count)
        {
            throw new ArgumentOutOfRangeException(
                nameof(count),
                count,
                "Count cannot exceed the distinct collection value count.");
        }

        return Shuffle(distinctValues).Take(count).ToList();
    }

    /// <summary>
    /// Returns a new list containing the source elements in random order.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="collection">The source collection.</param>
    /// <returns>A shuffled copy of <paramref name="collection"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="collection"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="collection"/> is empty.</exception>
    public static IList<T> Shuffle<T>(IList<T> collection)
    {
        var result = Check.NotNull(collection).ToList();
        if (result.Count == 0)
        {
            throw new ArgumentException("Collection cannot be empty.", nameof(collection));
        }

        for (var i = result.Count - 1; i > 0; i--)
        {
            var j = GetInt(i + 1);
            (result[i], result[j]) = (result[j], result[i]);
        }

        return result;
    }

    private static void EnsureMinLessThanMax<T>(T minValue, T maxValue, string parameterName)
        where T : IComparable<T>
    {
        if (minValue.CompareTo(maxValue) >= 0)
        {
            throw new ArgumentException("Minimum value must be less than maximum value.", parameterName);
        }
    }

    private static void EnsureFinite(double value, string parameterName)
    {
        if (!double.IsFinite(value))
        {
            throw new ArgumentException("Value must be finite.", parameterName);
        }
    }

    private static char GetRandomChar(string chars)
    {
        return chars[GetInt(chars.Length)];
    }
}
