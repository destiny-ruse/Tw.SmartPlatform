using System.Collections;

namespace Tw.Core.Collections;

/// <summary>
/// 提供可赋值给 <see cref="object"/> 的可变类型列表
/// </summary>
public class TypeList : TypeList<object>, ITypeList;

/// <summary>
/// 提供受基类型约束的可变类型列表
/// </summary>
/// <typeparam name="TBaseType">每个元素要求的基类型</typeparam>
public class TypeList<TBaseType> : ITypeList<TBaseType>
{
    private readonly List<Type> items = [];

    /// <summary>
    /// 获取或替换指定索引处的类型
    /// </summary>
    /// <param name="index">要获取或替换的类型的从零开始索引</param>
    /// <returns>存储在指定索引处的类型</returns>
    /// <exception cref="ArgumentNullException">当设置 <see langword="null"/> 值时抛出</exception>
    /// <exception cref="ArgumentException">当分配的类型不能赋值给 <typeparamref name="TBaseType"/> 时抛出</exception>
    /// <exception cref="ArgumentOutOfRangeException">当 <paramref name="index"/> 超出列表边界时抛出</exception>
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
    /// 添加可赋值给 <typeparamref name="TBaseType"/> 的类型
    /// </summary>
    /// <param name="type">要添加的类型</param>
    /// <exception cref="ArgumentNullException">当 <paramref name="type"/> 为 <see langword="null"/> 时抛出</exception>
    /// <exception cref="ArgumentException">当 <paramref name="type"/> 不能赋值给 <typeparamref name="TBaseType"/> 时抛出</exception>
    public void Add(Type type)
    {
        items.Add(Check.AssignableTo<TBaseType>(type));
    }

    /// <summary>
    /// 将指定类型参数添加到列表
    /// </summary>
    /// <typeparam name="T">要添加的类型，且必须可赋值给 <typeparamref name="TBaseType"/></typeparam>
    public void Add<T>()
        where T : TBaseType
    {
        Add(typeof(T));
    }

    /// <summary>
    /// 当指定类型参数尚不存在时添加该类型
    /// </summary>
    /// <typeparam name="T">要添加的类型，且必须可赋值给 <typeparamref name="TBaseType"/></typeparam>
    /// <returns>添加成功时返回 <see langword="true"/>；否则返回 <see langword="false"/></returns>
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
    /// 按精确类型匹配返回指定类型参数是否存在
    /// </summary>
    /// <typeparam name="T">要查找的类型，且必须可赋值给 <typeparamref name="TBaseType"/></typeparam>
    /// <returns>精确类型存在时返回 <see langword="true"/>；否则返回 <see langword="false"/></returns>
    public bool Contains<T>()
        where T : TBaseType
    {
        return Contains(typeof(T));
    }

    /// <summary>
    /// 按精确类型匹配移除指定类型参数
    /// </summary>
    /// <typeparam name="T">要移除的类型，且必须可赋值给 <typeparamref name="TBaseType"/></typeparam>
    /// <returns>精确类型移除成功时返回 <see langword="true"/>；否则返回 <see langword="false"/></returns>
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
    /// 按精确类型匹配返回给定类型是否存在
    /// </summary>
    /// <param name="item">要查找的类型</param>
    /// <returns>精确类型存在时返回 <see langword="true"/>；否则返回 <see langword="false"/></returns>
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
    /// 使用精确类型匹配返回给定类型的从零开始索引
    /// </summary>
    /// <param name="item">要查找的类型</param>
    /// <returns><paramref name="item"/> 的从零开始索引；不存在时返回 -1</returns>
    public int IndexOf(Type item)
    {
        return items.IndexOf(item);
    }

    /// <summary>
    /// 在指定索引处插入可赋值给 <typeparamref name="TBaseType"/> 的类型
    /// </summary>
    /// <param name="index">插入类型的位置，从零开始</param>
    /// <param name="item">要插入的类型</param>
    /// <exception cref="ArgumentNullException">当 <paramref name="item"/> 为 <see langword="null"/> 时抛出</exception>
    /// <exception cref="ArgumentException">当 <paramref name="item"/> 不能赋值给 <typeparamref name="TBaseType"/> 时抛出</exception>
    /// <exception cref="ArgumentOutOfRangeException">当 <paramref name="index"/> 超出有效插入范围时抛出</exception>
    public void Insert(int index, Type item)
    {
        items.Insert(index, Check.AssignableTo<TBaseType>(item));
    }

    /// <summary>
    /// 使用精确类型匹配移除给定类型的第一个匹配项
    /// </summary>
    /// <param name="item">要移除的类型</param>
    /// <returns>精确类型移除成功时返回 <see langword="true"/>；否则返回 <see langword="false"/></returns>
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
