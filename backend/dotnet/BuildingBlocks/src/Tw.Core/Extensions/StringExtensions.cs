using System.Text;
using System.Text.RegularExpressions;

namespace Tw.Core.Extensions;

/// <summary>提供字符串扩展方法</summary>
public static class StringExtensions
{
    /// <summary>返回字符串是否为 <see langword="null"/> 或空字符串</summary>
    public static bool IsNullOrEmpty(this string? str) => string.IsNullOrEmpty(str);

    /// <summary>返回字符串是否为 <see langword="null"/>、空字符串或空白字符串</summary>
    public static bool IsNullOrWhiteSpace(this string? str) => string.IsNullOrWhiteSpace(str);

    /// <summary>使用基础字符大小写规则将字符串的第一个 UTF-16 字符转换为大写</summary>
    public static string? ToPascalCase(this string? str)
    {
        return ChangeFirstCharacterCase(str, char.ToUpperInvariant);
    }

    /// <summary>使用基础字符大小写规则将字符串的第一个 UTF-16 字符转换为小写</summary>
    public static string? ToCamelCase(this string? str)
    {
        return ChangeFirstCharacterCase(str, char.ToLowerInvariant);
    }

    /// <summary>使用基础 UTF-16 字符大小写规则将字符串转换为 snake_case</summary>
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

    /// <summary>确保字符串以指定字符结尾</summary>
    /// <param name="str">源字符串</param>
    /// <param name="c">要求的尾随字符</param>
    /// <param name="comparisonType">用于检查现有后缀的比较方式</param>
    /// <returns>已以 <paramref name="c"/> 结尾时返回原始字符串；否则返回追加了 <paramref name="c"/> 的字符串</returns>
    public static string EnsureEndsWith(this string? str, char c, StringComparison comparisonType = StringComparison.Ordinal)
    {
        return string.IsNullOrEmpty(str)
            ? c.ToString()
            : str.EndsWith(c.ToString(), comparisonType) ? str : str + c;
    }

    /// <summary>确保字符串以指定字符开头</summary>
    /// <param name="str">源字符串</param>
    /// <param name="c">要求的前导字符</param>
    /// <param name="comparisonType">用于检查现有前缀的比较方式</param>
    /// <returns>已以 <paramref name="c"/> 开头时返回原始字符串；否则返回前置了 <paramref name="c"/> 的字符串</returns>
    public static string EnsureStartsWith(this string? str, char c, StringComparison comparisonType = StringComparison.Ordinal)
    {
        return string.IsNullOrEmpty(str)
            ? c.ToString()
            : str.StartsWith(c.ToString(), comparisonType) ? str : c + str;
    }

    /// <summary>从字符串返回最左侧字符</summary>
    /// <param name="str">源字符串</param>
    /// <param name="len">要返回的最大字符数</param>
    /// <returns>最左侧字符；字符串短于 <paramref name="len"/> 时返回整个字符串</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="str"/> 为 <see langword="null"/> 时抛出</exception>
    /// <exception cref="ArgumentOutOfRangeException">当 <paramref name="len"/> 为负数时抛出</exception>
    public static string Left(this string str, int len)
    {
        Check.NotNull(str);
        Check.NonNegative(len);
        return str.Length <= len ? str : str[..len];
    }

    /// <summary>从字符串返回最右侧字符</summary>
    /// <param name="str">源字符串</param>
    /// <param name="len">要返回的最大字符数</param>
    /// <returns>最右侧字符；字符串短于 <paramref name="len"/> 时返回整个字符串</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="str"/> 为 <see langword="null"/> 时抛出</exception>
    /// <exception cref="ArgumentOutOfRangeException">当 <paramref name="len"/> 为负数时抛出</exception>
    public static string Right(this string str, int len)
    {
        Check.NotNull(str);
        Check.NonNegative(len);
        return str.Length <= len ? str : str[^len..];
    }

    /// <summary>将所有换行符规范化为换行字符</summary>
    public static string? NormalizeLineEndings(this string? str)
    {
        return str?.Replace("\r\n", "\n").Replace("\r", "\n");
    }

    /// <summary>查找字符第 n 次出现时从零开始的索引</summary>
    /// <exception cref="ArgumentOutOfRangeException">当 <paramref name="n"/> 小于或等于零时抛出</exception>
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

    /// <summary>从字符串中移除第一个匹配后缀</summary>
    public static string? RemovePostFix(this string? str, params string[] postFixes)
    {
        return str.RemovePostFix(StringComparison.Ordinal, postFixes);
    }

    /// <summary>使用比较选项从字符串中移除第一个匹配后缀</summary>
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

    /// <summary>从字符串中移除第一个匹配前缀</summary>
    public static string? RemovePreFix(this string? str, params string[] preFixes)
    {
        return str.RemovePreFix(StringComparison.Ordinal, preFixes);
    }

    /// <summary>使用比较选项从字符串中移除第一个匹配前缀</summary>
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

    /// <summary>替换字符串中第一次出现的内容</summary>
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

    /// <summary>按字符串分隔符拆分字符串</summary>
    public static string[] Split(this string? str, string separator)
    {
        return Split(str, separator, StringSplitOptions.None);
    }

    /// <summary>按字符串分隔符拆分字符串</summary>
    public static string[] Split(this string? str, string separator, StringSplitOptions options)
    {
        return string.IsNullOrEmpty(str) ? [] : str.Split([separator], options);
    }

    /// <summary>将字符串拆分为多行</summary>
    public static string[] SplitToLines(this string? str)
    {
        return SplitToLines(str, StringSplitOptions.None);
    }

    /// <summary>将字符串拆分为多行</summary>
    public static string[] SplitToLines(this string? str, StringSplitOptions options)
    {
        return str.NormalizeLineEndings()?.Split('\n', options) ?? [];
    }

    /// <summary>将字符串编码为 UTF-8 字节</summary>
    public static byte[] GetBytes(this string? str)
    {
        return str.GetBytes(Encoding.UTF8);
    }

    /// <summary>使用给定编码对字符串进行编码</summary>
    /// <exception cref="ArgumentNullException">当 <paramref name="encoding"/> 为 <see langword="null"/> 时抛出</exception>
    public static byte[] GetBytes(this string? str, Encoding encoding)
    {
        return str is null ? [] : Check.NotNull(encoding).GetBytes(str);
    }

    /// <summary>从末尾截断字符串</summary>
    /// <exception cref="ArgumentOutOfRangeException">当 <paramref name="maxLength"/> 为负数时抛出</exception>
    public static string? Truncate(this string? str, int maxLength)
    {
        Check.NonNegative(maxLength);
        return str is null || str.Length <= maxLength ? str : str[..maxLength];
    }

    /// <summary>从开头截断字符串</summary>
    /// <exception cref="ArgumentOutOfRangeException">当 <paramref name="maxLength"/> 为负数时抛出</exception>
    public static string? TruncateFromBeginning(this string? str, int maxLength)
    {
        Check.NonNegative(maxLength);
        return str is null || str.Length <= maxLength ? str : str[^maxLength..];
    }

    /// <summary>截断字符串并追加省略号后缀</summary>
    public static string? TruncateWithPostfix(this string? str, int maxLength)
    {
        return str.TruncateWithPostfix(maxLength, "...");
    }

    /// <summary>截断字符串并追加自定义后缀</summary>
    /// <exception cref="ArgumentOutOfRangeException">当 <paramref name="maxLength"/> 为负数时抛出</exception>
    /// <exception cref="ArgumentException">当 <paramref name="postfix"/> 长于 <paramref name="maxLength"/> 时抛出</exception>
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
            throw new ArgumentException("后缀长度不能超过最大长度。", nameof(postfix));
        }

        return str[..(maxLength - postfix.Length)] + postfix;
    }

    /// <summary>将字符串拆分为固定大小的片段</summary>
    /// <exception cref="ArgumentOutOfRangeException">当 <paramref name="chunkSize"/> 小于或等于零时抛出</exception>
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

    /// <summary>使用 <see cref="string.Format(string, object?[])"/> 格式化字符串</summary>
    public static string? FormatWith(this string? template, params object?[] args)
    {
        return template is null ? null : string.Format(template, args);
    }

    /// <summary>从字符串中移除所有空白字符</summary>
    public static string? RemoveWhiteSpace(this string? value)
    {
        return value is null ? null : Regex.Replace(value, @"\s+", string.Empty);
    }

    /// <summary>反转字符串</summary>
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

    /// <summary>使用给定编码或 UTF-8 将字符串编码为 Base64</summary>
    public static string? ToBase64(this string? value, Encoding? encoding = null)
    {
        return value is null ? null : Convert.ToBase64String(value.GetBytes(encoding ?? Encoding.UTF8));
    }

    /// <summary>使用给定编码或 UTF-8 解码 Base64 字符串</summary>
    /// <exception cref="FormatException">当 <paramref name="value"/> 不是有效 Base64 文本时抛出</exception>
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
