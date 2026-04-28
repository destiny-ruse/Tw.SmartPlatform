namespace Tw.Core.Extensions;

/// <summary>Provides extension methods for <see cref="Type"/> values.</summary>
public static class TypeExtensions
{
    /// <summary>Returns the full type name followed by the assembly name.</summary>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="type"/> is <see langword="null"/>.</exception>
    public static string GetFullNameWithAssemblyName(this Type type)
    {
        var checkedType = Check.NotNull(type);
        return $"{checkedType.FullName}, {checkedType.Assembly.GetName().Name}";
    }

    /// <summary>Returns whether a type is assignable to the target type.</summary>
    /// <typeparam name="TTarget">The target type.</typeparam>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="type"/> is <see langword="null"/>.</exception>
    public static bool IsAssignableTo<TTarget>(this Type type)
    {
        return IsAssignableTo(type, typeof(TTarget));
    }

    /// <summary>Returns whether a type is assignable to the target type.</summary>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="type"/> or <paramref name="targetType"/> is <see langword="null"/>.</exception>
    public static bool IsAssignableTo(this Type type, Type targetType)
    {
        return Check.NotNull(targetType).IsAssignableFrom(Check.NotNull(type));
    }

    /// <summary>Gets the base classes for a type.</summary>
    /// <param name="type">The source type.</param>
    /// <param name="includeObject">Whether to include <see cref="object"/>.</param>
    /// <returns>The base classes from nearest to farthest.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="type"/> is <see langword="null"/>.</exception>
    public static Type[] GetBaseClasses(this Type type, bool includeObject = true)
    {
        return type.GetBaseClasses(stoppingType: null!, includeObject);
    }

    /// <summary>Gets the base classes for a type, stopping before a specified type.</summary>
    /// <param name="type">The source type.</param>
    /// <param name="stoppingType">The base type where traversal should stop before yielding it.</param>
    /// <param name="includeObject">Whether to include <see cref="object"/>.</param>
    /// <returns>The base classes from nearest to farthest.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="type"/> is <see langword="null"/>.</exception>
    public static Type[] GetBaseClasses(this Type type, Type stoppingType, bool includeObject = true)
    {
        Check.NotNull(type);

        var baseClasses = new List<Type>();
        var current = type.BaseType;
        while (current is not null)
        {
            if (current == stoppingType || (!includeObject && current == typeof(object)))
            {
                break;
            }

            baseClasses.Add(current);
            current = current.BaseType;
        }

        return baseClasses.ToArray();
    }
}
