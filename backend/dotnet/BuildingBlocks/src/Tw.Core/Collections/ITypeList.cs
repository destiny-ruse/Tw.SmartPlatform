namespace Tw.Core.Collections;

/// <summary>
/// 表示可赋值给 <see cref="object"/> 的类型列表
/// </summary>
public interface ITypeList : ITypeList<object>;

/// <summary>
/// 表示受基类型约束的类型列表
/// </summary>
/// <typeparam name="TBaseType">每个元素要求的基类型</typeparam>
public interface ITypeList<TBaseType> : IList<Type>
{
    /// <summary>
    /// 将指定类型参数添加到列表
    /// </summary>
    /// <typeparam name="T">要添加的类型</typeparam>
    void Add<T>()
        where T : TBaseType;

    /// <summary>
    /// 当指定类型参数尚不存在时添加该类型
    /// </summary>
    /// <typeparam name="T">要添加的类型</typeparam>
    /// <returns>添加成功时返回 <see langword="true"/>；否则返回 <see langword="false"/></returns>
    bool TryAdd<T>()
        where T : TBaseType;

    /// <summary>
    /// 返回指定类型参数是否存在
    /// </summary>
    /// <typeparam name="T">要查找的类型</typeparam>
    /// <returns>类型存在时返回 <see langword="true"/>；否则返回 <see langword="false"/></returns>
    bool Contains<T>()
        where T : TBaseType;

    /// <summary>
    /// 从列表中移除指定类型参数
    /// </summary>
    /// <typeparam name="T">要移除的类型</typeparam>
    /// <returns>移除成功时返回 <see langword="true"/>；否则返回 <see langword="false"/></returns>
    bool Remove<T>()
        where T : TBaseType;
}
