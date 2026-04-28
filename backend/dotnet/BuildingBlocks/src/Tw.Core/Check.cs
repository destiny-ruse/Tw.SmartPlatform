using System.Runtime.CompilerServices;

namespace Tw.Core;

/// <summary>
/// Provides guard helpers for validating method arguments.
/// </summary>
public static class Check
{
    /// <summary>
    /// Returns the supplied value when it is not <see langword="null"/>.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="value">The value to validate.</param>
    /// <param name="parameterName">The caller argument name.</param>
    /// <returns>The validated value.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <see langword="null"/>.</exception>
    public static T NotNull<T>(
        T? value,
        [CallerArgumentExpression(nameof(value))] string? parameterName = null)
    {
        return value ?? throw new ArgumentNullException(parameterName);
    }

    /// <summary>
    /// Returns the supplied string when it is not <see langword="null"/>, empty, or whitespace.
    /// </summary>
    /// <param name="value">The string to validate.</param>
    /// <param name="parameterName">The caller argument name.</param>
    /// <returns>The validated string.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value"/> is empty or whitespace.</exception>
    public static string NotNullOrWhiteSpace(
        string? value,
        [CallerArgumentExpression(nameof(value))] string? parameterName = null)
    {
        NotNull(value, parameterName);

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value cannot be empty or whitespace.", parameterName);
        }

        return value;
    }

    /// <summary>
    /// Returns the supplied string when it is not <see langword="null"/> or empty.
    /// </summary>
    /// <param name="value">The string to validate.</param>
    /// <param name="parameterName">The caller argument name.</param>
    /// <returns>The validated string.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value"/> is empty.</exception>
    public static string NotNullOrEmpty(
        string? value,
        [CallerArgumentExpression(nameof(value))] string? parameterName = null)
    {
        var validatedValue = NotNull(value, parameterName);

        if (validatedValue.Length == 0)
        {
            throw new ArgumentException("Value cannot be empty.", parameterName);
        }

        return validatedValue;
    }

    /// <summary>
    /// Returns the supplied enumerable when it is not <see langword="null"/> or empty.
    /// </summary>
    /// <typeparam name="T">The item type.</typeparam>
    /// <param name="value">The enumerable to validate.</param>
    /// <param name="parameterName">The caller argument name.</param>
    /// <returns>The validated enumerable.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value"/> is empty.</exception>
    public static IEnumerable<T> NotNullOrEmpty<T>(
        IEnumerable<T>? value,
        [CallerArgumentExpression(nameof(value))] string? parameterName = null)
    {
        var validatedValue = NotNull(value, parameterName);

        if (!validatedValue.Any())
        {
            throw new ArgumentException("Collection cannot be empty.", parameterName);
        }

        return validatedValue;
    }

    /// <summary>
    /// Returns the supplied collection when it is not <see langword="null"/> or empty.
    /// </summary>
    /// <typeparam name="T">The item type.</typeparam>
    /// <param name="value">The collection to validate.</param>
    /// <param name="parameterName">The caller argument name.</param>
    /// <returns>The validated collection.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value"/> is empty.</exception>
    public static ICollection<T> NotNullOrEmpty<T>(
        ICollection<T>? value,
        [CallerArgumentExpression(nameof(value))] string? parameterName = null)
    {
        var validatedValue = NotNull(value, parameterName);

        if (validatedValue.Count == 0)
        {
            throw new ArgumentException("Collection cannot be empty.", parameterName);
        }

        return validatedValue;
    }

    /// <summary>
    /// Returns the supplied integer when it is greater than zero.
    /// </summary>
    /// <param name="value">The integer to validate.</param>
    /// <param name="parameterName">The caller argument name.</param>
    /// <returns>The validated integer.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="value"/> is less than or equal to zero.</exception>
    public static int Positive(
        int value,
        [CallerArgumentExpression(nameof(value))] string? parameterName = null)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(parameterName, value, "Value must be greater than zero.");
        }

        return value;
    }

    /// <summary>
    /// Returns the supplied long integer when it is greater than zero.
    /// </summary>
    /// <param name="value">The long integer to validate.</param>
    /// <param name="parameterName">The caller argument name.</param>
    /// <returns>The validated long integer.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="value"/> is less than or equal to zero.</exception>
    public static long Positive(
        long value,
        [CallerArgumentExpression(nameof(value))] string? parameterName = null)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(parameterName, value, "Value must be greater than zero.");
        }

        return value;
    }

    /// <summary>
    /// Returns the supplied integer when it is greater than or equal to zero.
    /// </summary>
    /// <param name="value">The integer to validate.</param>
    /// <param name="parameterName">The caller argument name.</param>
    /// <returns>The validated integer.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="value"/> is less than zero.</exception>
    public static int NonNegative(
        int value,
        [CallerArgumentExpression(nameof(value))] string? parameterName = null)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(parameterName, value, "Value must be greater than or equal to zero.");
        }

        return value;
    }

    /// <summary>
    /// Returns the supplied integer when it falls within the inclusive range.
    /// </summary>
    /// <param name="value">The integer to validate.</param>
    /// <param name="min">The inclusive lower bound.</param>
    /// <param name="max">The inclusive upper bound.</param>
    /// <param name="parameterName">The caller argument name.</param>
    /// <returns>The validated integer.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="value"/> is outside the inclusive range.</exception>
    public static int InRange(
        int value,
        int min,
        int max,
        [CallerArgumentExpression(nameof(value))] string? parameterName = null)
    {
        if (value < min || value > max)
        {
            throw new ArgumentOutOfRangeException(parameterName, value, $"Value must be between {min} and {max}.");
        }

        return value;
    }

    /// <summary>
    /// Returns the supplied type when it is assignable to the requested base type.
    /// </summary>
    /// <typeparam name="TBaseType">The required base type.</typeparam>
    /// <param name="type">The type to validate.</param>
    /// <param name="parameterName">The caller argument name.</param>
    /// <returns>The validated type.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="type"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="type"/> is not assignable to <typeparamref name="TBaseType"/>.</exception>
    public static Type AssignableTo<TBaseType>(
        Type? type,
        [CallerArgumentExpression(nameof(type))] string? parameterName = null)
    {
        return AssignableTo(type, typeof(TBaseType), parameterName);
    }

    /// <summary>
    /// Returns the supplied type when it is assignable to the required base type.
    /// </summary>
    /// <param name="type">The type to validate.</param>
    /// <param name="baseType">The required base type.</param>
    /// <param name="parameterName">The caller argument name.</param>
    /// <returns>The validated type.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="type"/> or <paramref name="baseType"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="type"/> is not assignable to <paramref name="baseType"/>.</exception>
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
                $"Type {validatedType.FullName} is not assignable to {validatedBaseType.FullName}.",
                parameterName);
        }

        return validatedType;
    }
}
