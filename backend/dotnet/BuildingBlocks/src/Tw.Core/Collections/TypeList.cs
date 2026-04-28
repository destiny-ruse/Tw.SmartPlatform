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

    /// <inheritdoc />
    public Type this[int index]
    {
        get => items[index];
        set => items[index] = Check.AssignableTo<TBaseType>(value);
    }

    /// <inheritdoc />
    public int Count => items.Count;

    /// <inheritdoc />
    public bool IsReadOnly => false;

    /// <inheritdoc />
    public void Add(Type type)
    {
        items.Add(Check.AssignableTo<TBaseType>(type));
    }

    /// <inheritdoc />
    public void Add<T>()
        where T : TBaseType
    {
        Add(typeof(T));
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
    public bool Contains<T>()
        where T : TBaseType
    {
        return Contains(typeof(T));
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
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

    /// <inheritdoc />
    public int IndexOf(Type item)
    {
        return items.IndexOf(item);
    }

    /// <inheritdoc />
    public void Insert(int index, Type item)
    {
        items.Insert(index, Check.AssignableTo<TBaseType>(item));
    }

    /// <inheritdoc />
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
