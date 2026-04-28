using System.Runtime.CompilerServices;

namespace Tw.Core;

/// <summary>
/// 提供用于验证方法参数的守卫辅助方法
/// </summary>
public static class Check
{
    /// <summary>
    /// 当给定值不是 <see langword="null"/> 时返回该值
    /// </summary>
    /// <typeparam name="T">值类型</typeparam>
    /// <param name="value">要验证的值</param>
    /// <param name="parameterName">调用方参数名</param>
    /// <returns>通过验证的值</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="value"/> 为 <see langword="null"/> 时抛出</exception>
    public static T NotNull<T>(
        T? value,
        [CallerArgumentExpression(nameof(value))] string? parameterName = null)
    {
        return value ?? throw new ArgumentNullException(parameterName, "值不能为 null。");
    }

    /// <summary>
    /// 当给定字符串不是 <see langword="null"/>、空字符串或空白字符串时返回该字符串
    /// </summary>
    /// <param name="value">要验证的字符串</param>
    /// <param name="parameterName">调用方参数名</param>
    /// <returns>通过验证的字符串</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="value"/> 为 <see langword="null"/> 时抛出</exception>
    /// <exception cref="ArgumentException">当 <paramref name="value"/> 为空字符串或空白字符串时抛出</exception>
    public static string NotNullOrWhiteSpace(
        string? value,
        [CallerArgumentExpression(nameof(value))] string? parameterName = null)
    {
        NotNull(value, parameterName);

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("值不能是空字符串或空白字符串。", parameterName);
        }

        return value;
    }

    /// <summary>
    /// 当给定字符串不是 <see langword="null"/> 或空字符串时返回该字符串
    /// </summary>
    /// <param name="value">要验证的字符串</param>
    /// <param name="parameterName">调用方参数名</param>
    /// <returns>通过验证的字符串</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="value"/> 为 <see langword="null"/> 时抛出</exception>
    /// <exception cref="ArgumentException">当 <paramref name="value"/> 为空字符串时抛出</exception>
    public static string NotNullOrEmpty(
        string? value,
        [CallerArgumentExpression(nameof(value))] string? parameterName = null)
    {
        var validatedValue = NotNull(value, parameterName);

        if (validatedValue.Length == 0)
        {
            throw new ArgumentException("值不能为空。", parameterName);
        }

        return validatedValue;
    }

    /// <summary>
    /// 当给定枚举不是 <see langword="null"/> 或空集合时返回该枚举
    /// </summary>
    /// <typeparam name="T">元素类型</typeparam>
    /// <param name="value">要验证的枚举</param>
    /// <param name="parameterName">调用方参数名</param>
    /// <returns>通过验证的枚举</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="value"/> 为 <see langword="null"/> 时抛出</exception>
    /// <exception cref="ArgumentException">当 <paramref name="value"/> 为空集合时抛出</exception>
    public static IEnumerable<T> NotNullOrEmpty<T>(
        IEnumerable<T>? value,
        [CallerArgumentExpression(nameof(value))] string? parameterName = null)
    {
        var validatedValue = NotNull(value, parameterName);

        if (!validatedValue.Any())
        {
            throw new ArgumentException("集合不能为空。", parameterName);
        }

        return validatedValue;
    }

    /// <summary>
    /// 当给定集合不是 <see langword="null"/> 或空集合时返回该集合
    /// </summary>
    /// <typeparam name="T">元素类型</typeparam>
    /// <param name="value">要验证的集合</param>
    /// <param name="parameterName">调用方参数名</param>
    /// <returns>通过验证的集合</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="value"/> 为 <see langword="null"/> 时抛出</exception>
    /// <exception cref="ArgumentException">当 <paramref name="value"/> 为空集合时抛出</exception>
    public static ICollection<T> NotNullOrEmpty<T>(
        ICollection<T>? value,
        [CallerArgumentExpression(nameof(value))] string? parameterName = null)
    {
        var validatedValue = NotNull(value, parameterName);

        if (validatedValue.Count == 0)
        {
            throw new ArgumentException("集合不能为空。", parameterName);
        }

        return validatedValue;
    }

    /// <summary>
    /// 当给定整数大于零时返回该整数
    /// </summary>
    /// <param name="value">要验证的整数</param>
    /// <param name="parameterName">调用方参数名</param>
    /// <returns>通过验证的整数</returns>
    /// <exception cref="ArgumentOutOfRangeException">当 <paramref name="value"/> 小于或等于零时抛出</exception>
    public static int Positive(
        int value,
        [CallerArgumentExpression(nameof(value))] string? parameterName = null)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(parameterName, value, "值必须大于零。");
        }

        return value;
    }

    /// <summary>
    /// 当给定长整数大于零时返回该长整数
    /// </summary>
    /// <param name="value">要验证的长整数</param>
    /// <param name="parameterName">调用方参数名</param>
    /// <returns>通过验证的长整数</returns>
    /// <exception cref="ArgumentOutOfRangeException">当 <paramref name="value"/> 小于或等于零时抛出</exception>
    public static long Positive(
        long value,
        [CallerArgumentExpression(nameof(value))] string? parameterName = null)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(parameterName, value, "值必须大于零。");
        }

        return value;
    }

    /// <summary>
    /// 当给定整数大于或等于零时返回该整数
    /// </summary>
    /// <param name="value">要验证的整数</param>
    /// <param name="parameterName">调用方参数名</param>
    /// <returns>通过验证的整数</returns>
    /// <exception cref="ArgumentOutOfRangeException">当 <paramref name="value"/> 小于零时抛出</exception>
    public static int NonNegative(
        int value,
        [CallerArgumentExpression(nameof(value))] string? parameterName = null)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(parameterName, value, "值必须大于或等于零。");
        }

        return value;
    }

    /// <summary>
    /// 当给定整数落在闭区间内时返回该整数
    /// </summary>
    /// <param name="value">要验证的整数</param>
    /// <param name="min">闭区间下界</param>
    /// <param name="max">闭区间上界</param>
    /// <param name="parameterName">调用方参数名</param>
    /// <returns>通过验证的整数</returns>
    /// <exception cref="ArgumentOutOfRangeException">当 <paramref name="value"/> 位于闭区间之外时抛出</exception>
    public static int InRange(
        int value,
        int min,
        int max,
        [CallerArgumentExpression(nameof(value))] string? parameterName = null)
    {
        if (value < min || value > max)
        {
            throw new ArgumentOutOfRangeException(parameterName, value, $"值必须位于 {min} 和 {max} 之间。");
        }

        return value;
    }

    /// <summary>
    /// 当给定类型可赋值给请求的基类型时返回该类型
    /// </summary>
    /// <typeparam name="TBaseType">要求的基类型</typeparam>
    /// <param name="type">要验证的类型</param>
    /// <param name="parameterName">调用方参数名</param>
    /// <returns>通过验证的类型</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="type"/> 为 <see langword="null"/> 时抛出</exception>
    /// <exception cref="ArgumentException">当 <paramref name="type"/> 不能赋值给 <typeparamref name="TBaseType"/> 时抛出</exception>
    public static Type AssignableTo<TBaseType>(
        Type? type,
        [CallerArgumentExpression(nameof(type))] string? parameterName = null)
    {
        return AssignableTo(type, typeof(TBaseType), parameterName);
    }

    /// <summary>
    /// 当给定类型可赋值给要求的基类型时返回该类型
    /// </summary>
    /// <param name="type">要验证的类型</param>
    /// <param name="baseType">要求的基类型</param>
    /// <param name="parameterName">调用方参数名</param>
    /// <returns>通过验证的类型</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="type"/> 或 <paramref name="baseType"/> 为 <see langword="null"/> 时抛出</exception>
    /// <exception cref="ArgumentException">当 <paramref name="type"/> 不能赋值给 <paramref name="baseType"/> 时抛出</exception>
    public static Type AssignableTo(
        Type? type,
        Type baseType,
        [CallerArgumentExpression(nameof(type))] string? parameterName = null)
    {
        var validatedType = NotNull(type, parameterName);
        var validatedBaseType = NotNull(baseType, nameof(baseType));

        if (!validatedBaseType.IsAssignableFrom(validatedType))
        {
            throw new ArgumentException(
                $"类型 {validatedType.FullName} 不能赋值给 {validatedBaseType.FullName}。",
                parameterName);
        }

        return validatedType;
    }
}
