using System.Text;
using System.Text.RegularExpressions;

namespace Tw.Core.Extensions;

/// <summary>Provides extension methods for strings.</summary>
public static class StringExtensions
{
    /// <summary>Returns whether a string is <see langword="null"/> or empty.</summary>
    public static bool IsNullOrEmpty(this string? str) => string.IsNullOrEmpty(str);

    /// <summary>Returns whether a string is <see langword="null"/>, empty, or whitespace.</summary>
    public static bool IsNullOrWhiteSpace(this string? str) => string.IsNullOrWhiteSpace(str);

    /// <summary>Converts the first UTF-16 character of a string to uppercase using basic character casing.</summary>
    public static string? ToPascalCase(this string? str)
    {
        return ChangeFirstCharacterCase(str, char.ToUpperInvariant);
    }

    /// <summary>Converts the first UTF-16 character of a string to lowercase using basic character casing.</summary>
    public static string? ToCamelCase(this string? str)
    {
        return ChangeFirstCharacterCase(str, char.ToLowerInvariant);
    }

    /// <summary>Converts a string to snake_case using basic UTF-16 character casing.</summary>
    public static string? ToSnakeCase(this string? str)
    {
        if (string.IsNullOrEmpty(str))
        {
            return str;
        }

        var builder = new StringBuilder(str.Length + 8);
        var previousWasSeparator = false;

        for (var index = 0; index < str.Length; index++)
        {
            var current = str[index];
            if (char.IsWhiteSpace(current) || current is '-' or '_')
            {
                AppendSeparator(builder, ref previousWasSeparator);
                continue;
            }

            if (char.IsUpper(current) && builder.Length > 0 && !previousWasSeparator)
            {
                var previous = str[index - 1];
                var nextIsLower = index + 1 < str.Length && char.IsLower(str[index + 1]);
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
    /// <param name="str">The source string.</param>
    /// <param name="c">The required trailing character.</param>
    /// <param name="comparisonType">The comparison used to check the existing suffix.</param>
    /// <returns>The original string when it already ends with <paramref name="c"/>; otherwise, the string with <paramref name="c"/> appended.</returns>
    public static string EnsureEndsWith(this string? str, char c, StringComparison comparisonType = StringComparison.Ordinal)
    {
        return string.IsNullOrEmpty(str)
            ? c.ToString()
            : str.EndsWith(c.ToString(), comparisonType) ? str : str + c;
    }

    /// <summary>Ensures a string starts with a character.</summary>
    /// <param name="str">The source string.</param>
    /// <param name="c">The required leading character.</param>
    /// <param name="comparisonType">The comparison used to check the existing prefix.</param>
    /// <returns>The original string when it already starts with <paramref name="c"/>; otherwise, the string with <paramref name="c"/> prepended.</returns>
    public static string EnsureStartsWith(this string? str, char c, StringComparison comparisonType = StringComparison.Ordinal)
    {
        return string.IsNullOrEmpty(str)
            ? c.ToString()
            : str.StartsWith(c.ToString(), comparisonType) ? str : c + str;
    }

    /// <summary>Returns the leftmost characters from a string.</summary>
    /// <param name="str">The source string.</param>
    /// <param name="len">The maximum number of characters to return.</param>
    /// <returns>The leftmost characters, or the whole string when shorter than <paramref name="len"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="str"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="len"/> is negative.</exception>
    public static string Left(this string str, int len)
    {
        Check.NotNull(str);
        Check.NonNegative(len);
        return str.Length <= len ? str : str[..len];
    }

    /// <summary>Returns the rightmost characters from a string.</summary>
    /// <param name="str">The source string.</param>
    /// <param name="len">The maximum number of characters to return.</param>
    /// <returns>The rightmost characters, or the whole string when shorter than <paramref name="len"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="str"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="len"/> is negative.</exception>
    public static string Right(this string str, int len)
    {
        Check.NotNull(str);
        Check.NonNegative(len);
        return str.Length <= len ? str : str[^len..];
    }

    /// <summary>Normalizes all line endings to line feed characters.</summary>
    public static string? NormalizeLineEndings(this string? str)
    {
        return str?.Replace("\r\n", "\n").Replace("\r", "\n");
    }

    /// <summary>Finds the zero-based index of the nth occurrence of a character.</summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="n"/> is less than or equal to zero.</exception>
    public static int NthIndexOf(this string? str, char c, int n)
    {
        Check.Positive(n);

        if (string.IsNullOrEmpty(str))
        {
            return -1;
        }

        var index = -1;
        for (var count = 0; count < n; count++)
        {
            index = str.IndexOf(c, index + 1);
            if (index < 0)
            {
                return -1;
            }
        }

        return index;
    }

    /// <summary>Removes the first matching postfix from a string.</summary>
    public static string? RemovePostFix(this string? str, params string[] postFixes)
    {
        return str.RemovePostFix(StringComparison.Ordinal, postFixes);
    }

    /// <summary>Removes the first matching postfix from a string using a comparison option.</summary>
    public static string? RemovePostFix(this string? str, StringComparison comparisonType, params string[] postFixes)
    {
        if (string.IsNullOrEmpty(str) || postFixes.Length == 0)
        {
            return str;
        }

        foreach (var postfix in postFixes.Where(postfix => !string.IsNullOrEmpty(postfix)))
        {
            if (str.EndsWith(postfix, comparisonType))
            {
                return str[..^postfix.Length];
            }
        }

        return str;
    }

    /// <summary>Removes the first matching prefix from a string.</summary>
    public static string? RemovePreFix(this string? str, params string[] preFixes)
    {
        return str.RemovePreFix(StringComparison.Ordinal, preFixes);
    }

    /// <summary>Removes the first matching prefix from a string using a comparison option.</summary>
    public static string? RemovePreFix(this string? str, StringComparison comparisonType, params string[] preFixes)
    {
        if (string.IsNullOrEmpty(str) || preFixes.Length == 0)
        {
            return str;
        }

        foreach (var prefix in preFixes.Where(prefix => !string.IsNullOrEmpty(prefix)))
        {
            if (str.StartsWith(prefix, comparisonType))
            {
                return str[prefix.Length..];
            }
        }

        return str;
    }

    /// <summary>Replaces the first occurrence of a string.</summary>
    public static string? ReplaceFirst(this string? str, string search, string replace, StringComparison comparisonType = StringComparison.Ordinal)
    {
        if (string.IsNullOrEmpty(str))
        {
            return str;
        }

        Check.NotNullOrEmpty(search);
        replace ??= string.Empty;

        var index = str.IndexOf(search, comparisonType);
        return index < 0 ? str : str.Remove(index, search.Length).Insert(index, replace);
    }

    /// <summary>Splits a string by a string separator.</summary>
    public static string[] Split(this string? str, string separator)
    {
        return Split(str, separator, StringSplitOptions.None);
    }

    /// <summary>Splits a string by a string separator.</summary>
    public static string[] Split(this string? str, string separator, StringSplitOptions options)
    {
        return string.IsNullOrEmpty(str) ? [] : str.Split([separator], options);
    }

    /// <summary>Splits a string into lines.</summary>
    public static string[] SplitToLines(this string? str)
    {
        return SplitToLines(str, StringSplitOptions.None);
    }

    /// <summary>Splits a string into lines.</summary>
    public static string[] SplitToLines(this string? str, StringSplitOptions options)
    {
        return str.NormalizeLineEndings()?.Split('\n', options) ?? [];
    }

    /// <summary>Encodes a string as UTF-8 bytes.</summary>
    public static byte[] GetBytes(this string? str)
    {
        return str.GetBytes(Encoding.UTF8);
    }

    /// <summary>Encodes a string using the supplied encoding.</summary>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="encoding"/> is <see langword="null"/>.</exception>
    public static byte[] GetBytes(this string? str, Encoding encoding)
    {
        return str is null ? [] : Check.NotNull(encoding).GetBytes(str);
    }

    /// <summary>Truncates a string from the end.</summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="maxLength"/> is negative.</exception>
    public static string? Truncate(this string? str, int maxLength)
    {
        Check.NonNegative(maxLength);
        return str is null || str.Length <= maxLength ? str : str[..maxLength];
    }

    /// <summary>Truncates a string from the beginning.</summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="maxLength"/> is negative.</exception>
    public static string? TruncateFromBeginning(this string? str, int maxLength)
    {
        Check.NonNegative(maxLength);
        return str is null || str.Length <= maxLength ? str : str[^maxLength..];
    }

    /// <summary>Truncates a string and appends an ellipsis postfix.</summary>
    public static string? TruncateWithPostfix(this string? str, int maxLength)
    {
        return str.TruncateWithPostfix(maxLength, "...");
    }

    /// <summary>Truncates a string and appends a custom postfix.</summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="maxLength"/> is negative.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="postfix"/> is longer than <paramref name="maxLength"/>.</exception>
    public static string? TruncateWithPostfix(this string? str, int maxLength, string postfix)
    {
        Check.NonNegative(maxLength);
        postfix ??= string.Empty;

        if (str is null || str.Length <= maxLength)
        {
            return str;
        }

        if (postfix.Length > maxLength)
        {
            throw new ArgumentException("Postfix length cannot exceed maximum length.", nameof(postfix));
        }

        return str[..(maxLength - postfix.Length)] + postfix;
    }

    /// <summary>Splits a string into fixed-size chunks.</summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="chunkSize"/> is less than or equal to zero.</exception>
    public static IEnumerable<string> Chunk(this string? value, int chunkSize)
    {
        Check.Positive(chunkSize);

        if (string.IsNullOrEmpty(value))
        {
            yield break;
        }

        for (var index = 0; index < value.Length; index += chunkSize)
        {
            yield return value.Substring(index, Math.Min(chunkSize, value.Length - index));
        }
    }

    /// <summary>Formats a string using <see cref="string.Format(string, object?[])"/>.</summary>
    public static string? FormatWith(this string? template, params object?[] args)
    {
        return template is null ? null : string.Format(template, args);
    }

    /// <summary>Removes all whitespace characters from a string.</summary>
    public static string? RemoveWhiteSpace(this string? value)
    {
        return value is null ? null : Regex.Replace(value, @"\s+", string.Empty);
    }

    /// <summary>Reverses a string.</summary>
    public static string? Reverse(this string? value)
    {
        if (value is null)
        {
            return null;
        }

        var characters = value.ToCharArray();
        Array.Reverse(characters);
        return new string(characters);
    }

    /// <summary>Encodes a string as Base64 using the supplied encoding or UTF-8.</summary>
    public static string? ToBase64(this string? value, Encoding? encoding = null)
    {
        return value is null ? null : Convert.ToBase64String(value.GetBytes(encoding ?? Encoding.UTF8));
    }

    /// <summary>Decodes a Base64 string using the supplied encoding or UTF-8.</summary>
    /// <exception cref="FormatException">Thrown when <paramref name="value"/> is not valid Base64 text.</exception>
    public static string? FromBase64(this string? value, Encoding? encoding = null)
    {
        return value is null ? null : (encoding ?? Encoding.UTF8).GetString(Convert.FromBase64String(value));
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
