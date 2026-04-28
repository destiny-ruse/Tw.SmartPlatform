namespace Tw.Core.Extensions;

/// <summary>提供可变集合的扩展方法</summary>
public static class CollectionExtensions
{
    /// <summary>当集合尚未包含元素时添加该元素</summary>
    /// <param name="source">要更新的集合</param>
    /// <param name="item">要添加的元素</param>
    /// <typeparam name="T">元素类型</typeparam>
    /// <returns>元素添加成功时返回 <see langword="true"/>；否则返回 <see langword="false"/></returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="source"/> 为 <see langword="null"/> 时抛出</exception>
    public static bool AddIfNotContains<T>(this ICollection<T> source, T item)
    {
        var collection = Check.NotNull(source);

        if (collection.Contains(item))
        {
            return false;
        }

        collection.Add(item);
        return true;
    }

    /// <summary>添加每个缺失元素，并返回实际添加的元素</summary>
    /// <param name="source">要更新的集合</param>
    /// <param name="items">要添加的候选元素。序列会在调用时枚举一次</param>
    /// <typeparam name="T">元素类型</typeparam>
    /// <returns>已添加的元素，顺序与源枚举顺序一致</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="source"/> 或 <paramref name="items"/> 为 <see langword="null"/> 时抛出</exception>
    public static IEnumerable<T> AddIfNotContains<T>(this ICollection<T> source, IEnumerable<T> items)
    {
        var collection = Check.NotNull(source);
        var addedItems = new List<T>();

        foreach (var item in Check.NotNull(items))
        {
            if (collection.AddIfNotContains(item))
            {
                addedItems.Add(item);
            }
        }

        return addedItems;
    }

    /// <summary>当没有现有元素匹配谓词时添加由工厂创建的元素</summary>
    /// <param name="source">要更新的集合</param>
    /// <param name="predicate">用于测试现有元素的谓词</param>
    /// <param name="itemFactory">仅在无匹配元素时使用的工厂</param>
    /// <typeparam name="T">元素类型</typeparam>
    /// <returns>新增元素时返回 <see langword="true"/>；否则返回 <see langword="false"/></returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="source"/>、<paramref name="predicate"/> 或 <paramref name="itemFactory"/> 为 <see langword="null"/> 时抛出</exception>
    public static bool AddIfNotContains<T>(this ICollection<T> source, Func<T, bool> predicate, Func<T> itemFactory)
    {
        var collection = Check.NotNull(source);
        Check.NotNull(predicate);
        Check.NotNull(itemFactory);

        if (collection.Any(predicate))
        {
            return false;
        }

        collection.Add(itemFactory());
        return true;
    }

    /// <summary>移除所有匹配元素，并按原始枚举顺序返回这些元素</summary>
    /// <param name="source">要更新的集合</param>
    /// <param name="predicate">用于选择待移除元素的谓词</param>
    /// <typeparam name="T">元素类型</typeparam>
    /// <returns>已移除的元素</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="source"/> 或 <paramref name="predicate"/> 为 <see langword="null"/> 时抛出</exception>
    public static IList<T> RemoveAll<T>(this ICollection<T> source, Func<T, bool> predicate)
    {
        var collection = Check.NotNull(source);
        Check.NotNull(predicate);

        var removedItems = collection.Where(predicate).ToList();
        foreach (var item in removedItems)
        {
            collection.Remove(item);
        }

        return removedItems;
    }

    /// <summary>从集合中移除所有给定元素</summary>
    /// <param name="source">要更新的集合</param>
    /// <param name="items">要移除的元素。序列会在变更前创建快照</param>
    /// <typeparam name="T">元素类型</typeparam>
    /// <exception cref="ArgumentNullException">当 <paramref name="source"/> 或 <paramref name="items"/> 为 <see langword="null"/> 时抛出</exception>
    public static void RemoveAll<T>(this ICollection<T> source, IEnumerable<T> items)
    {
        var collection = Check.NotNull(source);
        Check.NotNull(items);

        foreach (var item in items.ToList())
        {
            collection.Remove(item);
        }
    }
}
