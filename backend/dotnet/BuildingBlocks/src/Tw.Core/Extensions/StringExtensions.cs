using System.Text;
using System.Text.RegularExpressions;

namespace Tw.Core.Extensions;

/// <summary>Provides extension methods for strings.</summary>
public static class StringExtensions
{
    /// <summary>Returns whether a string is <see langword="null"/> or empty.</summary>
    public static bool IsNullOrEmpty(this string? source) => string.IsNullOrEmpty(source);

    /// <summary>Returns whether a string is <see langword="null"/>, empty, or whitespace.</summary>
    public static bool IsNullOrWhiteSpace(this string? source) => string.IsNullOrWhiteSpace(source);

    /// <summary>Converts the first character of a string to uppercase.</summary>
    public static string? ToPascalCase(this string? source)
    {
        return ChangeFirstCharacterCase(source, char.ToUpperInvariant);
    }

    /// <summary>Converts the first character of a string to lowercase.</summary>
    public static string? ToCamelCase(this string? source)
    {
        return ChangeFirstCharacterCase(source, char.ToLowerInvariant);
    }

    /// <summary>Converts a string to snake_case.</summary>
    public static string? ToSnakeCase(this string? source)
    {
        if (string.IsNullOrEmpty(source))
        {
            return source;
        }

        var builder = new StringBuilder(source.Length + 8);
        var previousWasSeparator = false;

        for (var index = 0; index < source.Length; index++)
        {
            var current = source[index];
            if (char.IsWhiteSpace(current) || current is '-' or '_')
            {
                AppendSeparator(builder, ref previousWasSeparator);
                continue;
            }

            if (char.IsUpper(current) && builder.Length > 0 && !previousWasSeparator)
            {
                var previous = source[index - 1];
                var nextIsLower = index + 1 < source.Length && char.IsLower(source[index + 1]);
                if (char.IsLower(previous) || char.IsDigit(previous) || nextIsLower)
                {
                    AppendSeparator(builder, ref previousWasSeparator);
                }
            }

            builder.Append(char.ToLowerInvariant(current));
            previousWasSeparator = false;
        }

        return builder.ToString().Trim('_');
    }

    /// <summary>Ensures a string ends with a character.</summary>
    public static string EnsureEndsWith(this string? source, char end)
    {
        return string.IsNullOrEmpty(source)
            ? end.ToString()
            : source.EndsWith(end) ? source : source + end;
    }

    /// <summary>Ensures a string starts with a character.</summary>
    public static string EnsureStartsWith(this string? source, char start)
    {
        return string.IsNullOrEmpty(source)
            ? start.ToString()
            : source.StartsWith(start) ? source : start + source;
    }

    /// <summary>Returns the leftmost characters from a string.</summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="length"/> is negative.</exception>
    public static string? Left(this string? source, int length)
    {
        Check.NonNegative(length);
        return source is null || source.Length <= length ? source : source[..length];
    }

    /// <summary>Returns the rightmost characters from a string.</summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="length"/> is negative.</exception>
    public static string? Right(this string? source, int length)
    {
        Check.NonNegative(length);
        return source is null || source.Length <= length ? source : source[^length..];
    }

    /// <summary>Normalizes all line endings to a specified value.</summary>
    public static string? NormalizeLineEndings(this string? source, string lineEnding = "\n")
    {
        return source?.Replace("\r\n", "\n").Replace("\r", "\n").Replace("\n", lineEnding);
    }

    /// <summary>Finds the zero-based index of the nth occurrence of a string.</summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="n"/> is less than or equal to zero.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value"/> is empty.</exception>
    public static int NthIndexOf(this string? source, string value, int n, StringComparison comparisonType = StringComparison.Ordinal)
    {
        if (string.IsNullOrEmpty(source))
        {
            return -1;
        }

        Check.NotNullOrEmpty(value);
        Check.Positive(n);

        var index = -1;
        for (var count = 0; count < n; count++)
        {
            index = source.IndexOf(value, index + 1, comparisonType);
            if (index < 0)
            {
                return -1;
            }
        }

        return index;
    }

    /// <summary>Removes a matching postfix from a string.</summary>
    public static string? RemovePostFix(this string? source, string postfix)
    {
        return source.RemovePostFix(StringComparison.Ordinal, postfix);
    }

    /// <summary>Removes the first matching postfix from a string.</summary>
    public static string? RemovePostFix(this string? source, params string[] postfixes)
    {
        return source.RemovePostFix(StringComparison.Ordinal, postfixes);
    }

    /// <summary>Removes the first matching postfix from a string using a comparison option.</summary>
    public static string? RemovePostFix(this string? source, StringComparison comparisonType, params string[] postfixes)
    {
        if (string.IsNullOrEmpty(source) || postfixes.Length == 0)
        {
            return source;
        }

        foreach (var postfix in postfixes.Where(postfix => !string.IsNullOrEmpty(postfix)))
        {
            if (source.EndsWith(postfix, comparisonType))
            {
                return source[..^postfix.Length];
            }
        }

        return source;
    }

    /// <summary>Removes a matching prefix from a string.</summary>
    public static string? RemovePreFix(this string? source, string prefix)
    {
        return source.RemovePreFix(StringComparison.Ordinal, prefix);
    }

    /// <summary>Removes the first matching prefix from a string.</summary>
    public static string? RemovePreFix(this string? source, params string[] prefixes)
    {
        return source.RemovePreFix(StringComparison.Ordinal, prefixes);
    }

    /// <summary>Removes the first matching prefix from a string using a comparison option.</summary>
    public static string? RemovePreFix(this string? source, StringComparison comparisonType, params string[] prefixes)
    {
        if (string.IsNullOrEmpty(source) || prefixes.Length == 0)
        {
            return source;
        }

        foreach (var prefix in prefixes.Where(prefix => !string.IsNullOrEmpty(prefix)))
        {
            if (source.StartsWith(prefix, comparisonType))
            {
                return source[prefix.Length..];
            }
        }

        return source;
    }

    /// <summary>Replaces the first occurrence of a string.</summary>
    public static string? ReplaceFirst(this string? source, string search, string replacement, StringComparison comparisonType = StringComparison.Ordinal)
    {
        if (string.IsNullOrEmpty(source))
        {
            return source;
        }

        Check.NotNullOrEmpty(search);
        replacement ??= string.Empty;

        var index = source.IndexOf(search, comparisonType);
        return index < 0 ? source : source.Remove(index, search.Length).Insert(index, replacement);
    }

    /// <summary>Splits a string by a character separator.</summary>
    public static string[] Split(this string? source, char separator, StringSplitOptions options = StringSplitOptions.None)
    {
        return string.IsNullOrEmpty(source) ? [] : source.Split([separator], options);
    }

    /// <summary>Splits a string by a string separator.</summary>
    public static string[] Split(this string? source, string separator, StringSplitOptions options = StringSplitOptions.None)
    {
        return string.IsNullOrEmpty(source) ? [] : source.Split([separator], options);
    }

    /// <summary>Splits a string by string separators.</summary>
    public static string[] Split(this string? source, string[] separators, StringSplitOptions options = StringSplitOptions.None)
    {
        return string.IsNullOrEmpty(source) ? [] : source.Split(separators, options);
    }

    /// <summary>Splits a string into lines.</summary>
    public static string[] SplitToLines(this string? source, StringSplitOptions options = StringSplitOptions.None)
    {
        return source.NormalizeLineEndings()?.Split('\n', options) ?? [];
    }

    /// <summary>Splits a string into lines after normalizing to a specified line ending.</summary>
    public static string[] SplitToLines(this string? source, string lineEnding, StringSplitOptions options = StringSplitOptions.None)
    {
        return source.NormalizeLineEndings(lineEnding)?.Split(lineEnding, options) ?? [];
    }

    /// <summary>Encodes a string as UTF-8 bytes.</summary>
    public static byte[] GetBytes(this string? source)
    {
        return source.GetBytes(Encoding.UTF8);
    }

    /// <summary>Encodes a string using the supplied encoding.</summary>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="encoding"/> is <see langword="null"/>.</exception>
    public static byte[] GetBytes(this string? source, Encoding encoding)
    {
        return source is null ? [] : Check.NotNull(encoding).GetBytes(source);
    }

    /// <summary>Truncates a string from the end.</summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="maxLength"/> is negative.</exception>
    public static string? Truncate(this string? source, int maxLength)
    {
        Check.NonNegative(maxLength);
        return source is null || source.Length <= maxLength ? source : source[..maxLength];
    }

    /// <summary>Truncates a string from the beginning.</summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="maxLength"/> is negative.</exception>
    public static string? TruncateFromBeginning(this string? source, int maxLength)
    {
        Check.NonNegative(maxLength);
        return source is null || source.Length <= maxLength ? source : source[^maxLength..];
    }

    /// <summary>Truncates a string and appends an ellipsis postfix.</summary>
    public static string? TruncateWithPostfix(this string? source, int maxLength)
    {
        return source.TruncateWithPostfix(maxLength, "...");
    }

    /// <summary>Truncates a string and appends a custom postfix.</summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="maxLength"/> is negative.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="postfix"/> is longer than <paramref name="maxLength"/>.</exception>
    public static string? TruncateWithPostfix(this string? source, int maxLength, string postfix)
    {
        Check.NonNegative(maxLength);
        postfix ??= string.Empty;

        if (source is null || source.Length <= maxLength)
        {
            return source;
        }

        if (postfix.Length > maxLength)
        {
            throw new ArgumentException("Postfix length cannot exceed maximum length.", nameof(postfix));
        }

        return source[..(maxLength - postfix.Length)] + postfix;
    }

    /// <summary>Truncates a string to a length and appends a postfix.</summary>
    public static string? TruncateWithPostfix(this string? source, string postfix, int maxLength)
    {
        return source.TruncateWithPostfix(maxLength, postfix);
    }

    /// <summary>Splits a string into fixed-size chunks.</summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="chunkSize"/> is less than or equal to zero.</exception>
    public static IEnumerable<string> Chunk(this string? source, int chunkSize)
    {
        Check.Positive(chunkSize);

        if (string.IsNullOrEmpty(source))
        {
            yield break;
        }

        for (var index = 0; index < source.Length; index += chunkSize)
        {
            yield return source.Substring(index, Math.Min(chunkSize, source.Length - index));
        }
    }

    /// <summary>Formats a string using <see cref="string.Format(string, object?[])"/>.</summary>
    public static string? FormatWith(this string? source, params object?[] args)
    {
        return source is null ? null : string.Format(source, args);
    }

    /// <summary>Removes all whitespace characters from a string.</summary>
    public static string? RemoveWhiteSpace(this string? source)
    {
        return source is null ? null : Regex.Replace(source, @"\s+", string.Empty);
    }

    /// <summary>Reverses a string.</summary>
    public static string? Reverse(this string? source)
    {
        if (source is null)
        {
            return null;
        }

        var characters = source.ToCharArray();
        Array.Reverse(characters);
        return new string(characters);
    }

    /// <summary>Encodes a string as Base64 using UTF-8.</summary>
    public static string? ToBase64(this string? source)
    {
        return source.ToBase64(Encoding.UTF8);
    }

    /// <summary>Encodes a string as Base64 using the supplied encoding.</summary>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="encoding"/> is <see langword="null"/>.</exception>
    public static string? ToBase64(this string? source, Encoding encoding)
    {
        return source is null ? null : Convert.ToBase64String(source.GetBytes(encoding));
    }

    /// <summary>Decodes a Base64 string using UTF-8.</summary>
    /// <exception cref="FormatException">Thrown when <paramref name="source"/> is not valid Base64 text.</exception>
    public static string? FromBase64(this string? source)
    {
        return source.FromBase64(Encoding.UTF8);
    }

    /// <summary>Decodes a Base64 string using the supplied encoding.</summary>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="encoding"/> is <see langword="null"/>.</exception>
    /// <exception cref="FormatException">Thrown when <paramref name="source"/> is not valid Base64 text.</exception>
    public static string? FromBase64(this string? source, Encoding encoding)
    {
        return source is null ? null : Check.NotNull(encoding).GetString(Convert.FromBase64String(source));
    }

    private static string? ChangeFirstCharacterCase(string? source, Func<char, char> converter)
    {
        if (string.IsNullOrEmpty(source))
        {
            return source;
        }

        return converter(source[0]) + source[1..];
    }

    private static void AppendSeparator(StringBuilder builder, ref bool previousWasSeparator)
    {
        if (builder.Length > 0 && !previousWasSeparator)
        {
            builder.Append('_');
            previousWasSeparator = true;
        }
    }
}
