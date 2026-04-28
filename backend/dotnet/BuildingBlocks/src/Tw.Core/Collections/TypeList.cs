using System.Collections;

namespace Tw.Core.Collections;

/// <summary>
/// Provides a mutable list of types assignable to <see cref="object"/>.
/// </summary>
public class TypeList : TypeList<object>, ITypeList;

/// <summary>
/// Provides a mutable list of types constrained to a base type.
/// </summary>
/// <typeparam name="TBaseType">The required base type for every item.</typeparam>
public class TypeList<TBaseType> : ITypeList<TBaseType>
{
    private readonly List<Type> items = [];

    /// <summary>
    /// Gets or replaces the type at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index of the type to get or replace.</param>
    /// <returns>The type stored at the specified index.</returns>
    /// <exception cref="ArgumentNullException">Thrown when setting a <see langword="null"/> value.</exception>
    /// <exception cref="ArgumentException">Thrown when the assigned type is not assignable to <typeparamref name="TBaseType"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="index"/> is outside the list bounds.</exception>
    public Type this[int index]
    {
        get => items[index];
        set => items[index] = Check.AssignableTo<TBaseType>(value);
    }

    /// <inheritdoc />
    public int Count => items.Count;

    /// <inheritdoc />
    public bool IsReadOnly => false;

    /// <summary>
    /// Adds a type assignable to <typeparamref name="TBaseType"/>.
    /// </summary>
    /// <param name="type">The type to add.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="type"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="type"/> is not assignable to <typeparamref name="TBaseType"/>.</exception>
    public void Add(Type type)
    {
        items.Add(Check.AssignableTo<TBaseType>(type));
    }

    /// <summary>
    /// Adds the specified type argument to the list.
    /// </summary>
    /// <typeparam name="T">The type to add; it must be assignable to <typeparamref name="TBaseType"/>.</typeparam>
    public void Add<T>()
        where T : TBaseType
    {
        Add(typeof(T));
    }

    /// <summary>
    /// Adds the specified type argument when it is not already present.
    /// </summary>
    /// <typeparam name="T">The type to add; it must be assignable to <typeparamref name="TBaseType"/>.</typeparam>
    /// <returns><see langword="true"/> when the type was added; otherwise, <see langword="false"/>.</returns>
    public bool TryAdd<T>()
        where T : TBaseType
    {
        if (Contains<T>())
        {
            return false;
        }

        Add<T>();

        return true;
    }

    /// <summary>
    /// Returns whether the specified type argument is present by exact type match.
    /// </summary>
    /// <typeparam name="T">The type to locate; it must be assignable to <typeparamref name="TBaseType"/>.</typeparam>
    /// <returns><see langword="true"/> when the exact type is present; otherwise, <see langword="false"/>.</returns>
    public bool Contains<T>()
        where T : TBaseType
    {
        return Contains(typeof(T));
    }

    /// <summary>
    /// Removes the specified type argument by exact type match.
    /// </summary>
    /// <typeparam name="T">The type to remove; it must be assignable to <typeparamref name="TBaseType"/>.</typeparam>
    /// <returns><see langword="true"/> when the exact type was removed; otherwise, <see langword="false"/>.</returns>
    public bool Remove<T>()
        where T : TBaseType
    {
        return Remove(typeof(T));
    }

    /// <inheritdoc />
    public void Clear()
    {
        items.Clear();
    }

    /// <summary>
    /// Returns whether the supplied type is present by exact type match.
    /// </summary>
    /// <param name="item">The type to locate.</param>
    /// <returns><see langword="true"/> when the exact type is present; otherwise, <see langword="false"/>.</returns>
    public bool Contains(Type item)
    {
        return items.Contains(item);
    }

    /// <inheritdoc />
    public void CopyTo(Type[] array, int arrayIndex)
    {
        items.CopyTo(array, arrayIndex);
    }

    /// <inheritdoc />
    public IEnumerator<Type> GetEnumerator()
    {
        return items.GetEnumerator();
    }

    /// <summary>
    /// Returns the zero-based index of the supplied type using exact type matching.
    /// </summary>
    /// <param name="item">The type to locate.</param>
    /// <returns>The zero-based index of <paramref name="item"/>, or -1 when it is absent.</returns>
    public int IndexOf(Type item)
    {
        return items.IndexOf(item);
    }

    /// <summary>
    /// Inserts a type assignable to <typeparamref name="TBaseType"/> at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index at which to insert the type.</param>
    /// <param name="item">The type to insert.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="item"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="item"/> is not assignable to <typeparamref name="TBaseType"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="index"/> is outside the valid insertion range.</exception>
    public void Insert(int index, Type item)
    {
        items.Insert(index, Check.AssignableTo<TBaseType>(item));
    }

    /// <summary>
    /// Removes the first occurrence of the supplied type using exact type matching.
    /// </summary>
    /// <param name="item">The type to remove.</param>
    /// <returns><see langword="true"/> when the exact type was removed; otherwise, <see langword="false"/>.</returns>
    public bool Remove(Type item)
    {
        return items.Remove(item);
    }

    /// <inheritdoc />
    public void RemoveAt(int index)
    {
        items.RemoveAt(index);
    }

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
