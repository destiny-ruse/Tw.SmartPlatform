using System.Security.Cryptography;
using Tw.Core;

namespace Tw.Core.Utilities;

/// <summary>
/// 提供密码学安全的随机值和集合辅助方法
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
    /// 返回半开区间 [<paramref name="minValue"/>, <paramref name="maxValue"/>) 内的随机整数
    /// </summary>
    /// <param name="minValue">闭区间下界</param>
    /// <param name="maxValue">开区间上界</param>
    /// <returns>请求范围内的随机整数</returns>
    /// <exception cref="ArgumentException">当 <paramref name="minValue"/> 大于或等于 <paramref name="maxValue"/> 时抛出</exception>
    public static int GetInt(int minValue, int maxValue)
    {
        EnsureMinLessThanMax(minValue, maxValue, nameof(maxValue));

        return RandomNumberGenerator.GetInt32(minValue, maxValue);
    }

    /// <summary>
    /// 返回半开区间 [0, <paramref name="maxValue"/>) 内的随机整数
    /// </summary>
    /// <param name="maxValue">开区间上界</param>
    /// <returns>请求范围内的随机整数</returns>
    /// <exception cref="ArgumentOutOfRangeException">当 <paramref name="maxValue"/> 小于或等于零时抛出</exception>
    public static int GetInt(int maxValue)
    {
        Check.Positive(maxValue);

        return RandomNumberGenerator.GetInt32(maxValue);
    }

    /// <summary>
    /// 返回半开区间 [<paramref name="minValue"/>, <paramref name="maxValue"/>) 内的随机长整数
    /// </summary>
    /// <param name="minValue">闭区间下界</param>
    /// <param name="maxValue">开区间上界</param>
    /// <returns>请求范围内的随机长整数</returns>
    /// <exception cref="ArgumentException">当 <paramref name="minValue"/> 大于或等于 <paramref name="maxValue"/> 时抛出</exception>
    /// <remarks>使用拒绝采样避免请求范围内的取模偏差</remarks>
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
    /// 返回半开区间 [0.0, 1.0) 内的随机双精度浮点数
    /// </summary>
    /// <returns>请求范围内的随机双精度浮点数</returns>
    public static double GetDouble()
    {
        Span<byte> bytes = stackalloc byte[sizeof(ulong)];
        RandomNumberGenerator.Fill(bytes);

        var randomBits = BitConverter.ToUInt64(bytes) >> 11;
        return randomBits / (double)(1UL << 53);
    }

    /// <summary>
    /// 返回半开区间 [<paramref name="minValue"/>, <paramref name="maxValue"/>) 内的随机双精度浮点数
    /// </summary>
    /// <param name="minValue">闭区间下界</param>
    /// <param name="maxValue">开区间上界</param>
    /// <returns>请求范围内的随机双精度浮点数</returns>
    /// <exception cref="ArgumentException">当边界不是有限数、下界大于或等于上界，或范围跨度不是有限数时抛出</exception>
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
    /// 返回随机布尔值
    /// </summary>
    /// <returns>随机布尔值</returns>
    public static bool GetBool()
    {
        return GetInt(2) == 0;
    }

    /// <summary>
    /// 返回填充了密码学安全随机字节的字节数组
    /// </summary>
    /// <param name="length">请求的字节数</param>
    /// <returns>具有请求长度的新字节数组</returns>
    /// <exception cref="ArgumentOutOfRangeException">当 <paramref name="length"/> 为负数时抛出</exception>
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
    /// 使用给定字符源或默认字母数字源返回随机字符串
    /// </summary>
    /// <param name="length">请求的字符串长度</param>
    /// <param name="chars">可选字符源。省略时使用字母数字字符</param>
    /// <returns>具有请求长度的随机字符串</returns>
    /// <exception cref="ArgumentOutOfRangeException">当 <paramref name="length"/> 为负数时抛出</exception>
    /// <exception cref="ArgumentException">当 <paramref name="chars"/> 为空字符串时抛出</exception>
    public static string GetString(int length, string? chars = null)
    {
        Check.NonNegative(length);

        var source = chars ?? AlphanumericChars;
        if (source.Length == 0)
        {
            throw new ArgumentException("字符源不能为空。", nameof(chars));
        }

        return new string(Enumerable.Range(0, length)
            .Select(_ => source[GetInt(source.Length)])
            .ToArray());
    }

    /// <summary>
    /// 返回随机数字字符串
    /// </summary>
    /// <param name="length">请求的字符串长度</param>
    /// <returns>具有请求长度的数字字符串</returns>
    /// <exception cref="ArgumentOutOfRangeException">当 <paramref name="length"/> 为负数时抛出</exception>
    public static string GetNumericString(int length)
    {
        return GetString(length, NumericChars);
    }

    /// <summary>
    /// 返回随机字母字符串
    /// </summary>
    /// <param name="length">请求的字符串长度</param>
    /// <param name="upperCase">是否使用大写字母；否则使用小写字母</param>
    /// <returns>具有请求长度的字母字符串</returns>
    /// <exception cref="ArgumentOutOfRangeException">当 <paramref name="length"/> 为负数时抛出</exception>
    public static string GetAlphaString(int length, bool upperCase = true)
    {
        return GetString(length, upperCase ? UppercaseChars : LowercaseChars);
    }

    /// <summary>
    /// 返回随机字母数字字符串
    /// </summary>
    /// <param name="length">请求的字符串长度</param>
    /// <returns>具有请求长度的字母数字字符串</returns>
    /// <exception cref="ArgumentOutOfRangeException">当 <paramref name="length"/> 为负数时抛出</exception>
    public static string GetAlphanumericString(int length)
    {
        return GetString(length, AlphanumericChars);
    }

    /// <summary>
    /// 返回包含每个必需字符类别的随机密码
    /// </summary>
    /// <param name="length">请求的密码长度</param>
    /// <param name="includeSpecialChars">是否要求并允许特殊字符</param>
    /// <returns>具有请求长度和必需字符类别的密码</returns>
    /// <exception cref="ArgumentOutOfRangeException">当 <paramref name="length"/> 短于必需类别数量时抛出</exception>
    public static string GetStrongPassword(int length = 16, bool includeSpecialChars = true)
    {
        var requiredLength = includeSpecialChars ? 4 : 3;
        if (length < requiredLength)
        {
            throw new ArgumentOutOfRangeException(
                nameof(length),
                length,
                $"长度必须至少为 {requiredLength}。");
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
    /// 返回包含源字符随机顺序的新字符串；输入为空时返回空字符串
    /// </summary>
    /// <param name="value">要打乱的字符串</param>
    /// <returns><paramref name="value"/> 的随机顺序副本</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="value"/> 为 <see langword="null"/> 时抛出</exception>
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
    /// 从集合中返回一个随机元素
    /// </summary>
    /// <typeparam name="T">元素类型</typeparam>
    /// <param name="collection">源集合</param>
    /// <returns>来自 <paramref name="collection"/> 的随机元素</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="collection"/> 为 <see langword="null"/> 时抛出</exception>
    /// <exception cref="ArgumentException">当 <paramref name="collection"/> 为空集合时抛出</exception>
    public static T GetRandomElement<T>(IList<T> collection)
    {
        var source = Check.NotNull(collection);
        if (source.Count == 0)
        {
            throw new ArgumentException("集合不能为空。", nameof(collection));
        }

        return source[GetInt(source.Count)];
    }

    /// <summary>
    /// 从集合中随机选择不重复值，且不修改输入集合
    /// </summary>
    /// <typeparam name="T">元素类型</typeparam>
    /// <param name="collection">源集合</param>
    /// <param name="count">要选择的不重复值数量</param>
    /// <returns>包含已选择不重复值的新列表</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="collection"/> 为 <see langword="null"/> 时抛出</exception>
    /// <exception cref="ArgumentException">当 <paramref name="collection"/> 为空集合时抛出</exception>
    /// <exception cref="ArgumentOutOfRangeException">当 <paramref name="count"/> 为负数或大于源集合的不重复值数量时抛出</exception>
    public static IList<T> GetRandomElements<T>(IList<T> collection, int count)
    {
        var source = Check.NotNull(collection);
        if (source.Count == 0)
        {
            throw new ArgumentException("集合不能为空。", nameof(collection));
        }

        Check.NonNegative(count);

        var distinctValues = source.Distinct(EqualityComparer<T>.Default).ToList();
        if (count > distinctValues.Count)
        {
            throw new ArgumentOutOfRangeException(
                nameof(count),
                count,
                "数量不能超过集合中的不重复值数量。");
        }

        return Shuffle(distinctValues).Take(count).ToList();
    }

    /// <summary>
    /// 返回包含源元素随机顺序的新列表
    /// </summary>
    /// <typeparam name="T">元素类型</typeparam>
    /// <param name="collection">源集合</param>
    /// <returns><paramref name="collection"/> 的随机顺序副本</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="collection"/> 为 <see langword="null"/> 时抛出</exception>
    /// <exception cref="ArgumentException">当 <paramref name="collection"/> 为空集合时抛出</exception>
    public static IList<T> Shuffle<T>(IList<T> collection)
    {
        var result = Check.NotNull(collection).ToList();
        if (result.Count == 0)
        {
            throw new ArgumentException("集合不能为空。", nameof(collection));
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
            throw new ArgumentException("最小值必须小于最大值。", parameterName);
        }
    }

    private static void EnsureFinite(double value, string parameterName)
    {
        if (!double.IsFinite(value))
        {
            throw new ArgumentException("值必须是有限数。", parameterName);
        }
    }

    private static char GetRandomChar(string chars)
    {
        return chars[GetInt(chars.Length)];
    }
}
