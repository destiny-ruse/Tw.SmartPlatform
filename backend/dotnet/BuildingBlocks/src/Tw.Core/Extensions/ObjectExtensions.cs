namespace Tw.Core.Extensions;

/// <summary>Provides extension methods for general object values.</summary>
public static class ObjectExtensions
{
    /// <summary>Casts an object using the C# <c>as</c> operator.</summary>
    public static T As<T>(this object obj)
        where T : class
    {
        return (obj as T)!;
    }

    /// <summary>Converts an object to a value type.</summary>
    /// <exception cref="InvalidCastException">Thrown when conversion is not supported.</exception>
    /// <exception cref="FormatException">Thrown when conversion input is not in a valid format.</exception>
    public static T To<T>(this object obj)
        where T : struct
    {
        var targetType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
        return (T)Convert.ChangeType(obj, targetType);
    }

    /// <summary>Returns whether an item is contained in the provided values.</summary>
    public static bool IsIn<T>(this T item, params T[] list)
    {
        return item.IsIn((IEnumerable<T>)Check.NotNull(list));
    }

    /// <summary>Returns whether an item is contained in the provided enumerable.</summary>
    public static bool IsIn<T>(this T item, IEnumerable<T> items)
    {
        return Check.NotNull(items).Contains(item);
    }

    /// <summary>Transforms a value when a condition is true.</summary>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="func"/> is <see langword="null"/>.</exception>
    public static T If<T>(this T obj, bool condition, Func<T, T> func)
    {
        Check.NotNull(func);
        return condition ? func(obj) : obj;
    }

    /// <summary>Invokes an action for a value when a condition is true and returns the original value.</summary>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="action"/> is <see langword="null"/>.</exception>
    public static T If<T>(this T obj, bool condition, Action<T> action)
    {
        Check.NotNull(action);

        if (condition)
        {
            action(obj);
        }

        return obj;
    }
}
