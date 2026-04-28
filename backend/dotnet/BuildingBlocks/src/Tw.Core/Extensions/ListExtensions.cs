namespace Tw.Core.Extensions;

/// <summary>提供可变列表扩展方法</summary>
public static class ListExtensions
{
    /// <summary>返回列表的只读视图或快照</summary>
    /// <param name="source">要暴露的列表</param>
    /// <typeparam name="T">元素类型</typeparam>
    /// <returns>只读列表</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="source"/> 为 <see langword="null"/> 时抛出</exception>
    public static IReadOnlyList<T> AsReadOnly<T>(this IList<T> source)
    {
        var list = Check.NotNull(source);
        return list as IReadOnlyList<T> ?? list.ToArray();
    }

    /// <summary>从某个索引开始插入元素序列</summary>
    /// <param name="source">要更新的列表</param>
    /// <param name="index">插入索引</param>
    /// <param name="items">要插入的元素</param>
    /// <typeparam name="T">元素类型</typeparam>
    /// <exception cref="ArgumentNullException">当 <paramref name="source"/> 或 <paramref name="items"/> 为 <see langword="null"/> 时抛出</exception>
    /// <exception cref="ArgumentOutOfRangeException">当 <paramref name="index"/> 超出有效插入范围时抛出</exception>
    public static void InsertRange<T>(this IList<T> source, int index, IEnumerable<T> items)
    {
        var list = Check.NotNull(source);
        Check.NotNull(items);

        if (index < 0 || index > list.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index), index, "索引必须位于列表可插入范围内。");
        }

        foreach (var item in items.ToList())
        {
            list.Insert(index++, item);
        }
    }

    /// <summary>查找第一个匹配谓词的索引</summary>
    /// <param name="source">要搜索的列表</param>
    /// <param name="selector">要匹配的谓词</param>
    /// <typeparam name="T">元素类型</typeparam>
    /// <returns>从零开始的索引；没有元素匹配时返回 -1</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="source"/> 或 <paramref name="selector"/> 为 <see langword="null"/> 时抛出</exception>
    public static int FindIndex<T>(this IList<T> source, Predicate<T> selector)
    {
        var list = Check.NotNull(source);
        Check.NotNull(selector);

        for (var index = 0; index < list.Count; index++)
        {
            if (selector(list[index]))
            {
                return index;
            }
        }

        return -1;
    }

    /// <summary>在列表开头添加元素</summary>
    /// <param name="source">要更新的列表</param>
    /// <param name="item">要添加的元素</param>
    /// <typeparam name="T">元素类型</typeparam>
    /// <exception cref="ArgumentNullException">当 <paramref name="source"/> 为 <see langword="null"/> 时抛出</exception>
    public static void AddFirst<T>(this IList<T> source, T item)
    {
        Check.NotNull(source).Insert(0, item);
    }

    /// <summary>在列表末尾添加元素</summary>
    /// <param name="source">要更新的列表</param>
    /// <param name="item">要添加的元素</param>
    /// <typeparam name="T">元素类型</typeparam>
    /// <exception cref="ArgumentNullException">当 <paramref name="source"/> 为 <see langword="null"/> 时抛出</exception>
    public static void AddLast<T>(this IList<T> source, T item)
    {
        Check.NotNull(source).Add(item);
    }

    /// <summary>在第一个匹配的现有元素之后插入元素</summary>
    /// <param name="source">要更新的列表</param>
    /// <param name="existingItem">要查找的现有元素</param>
    /// <param name="item">要插入的元素</param>
    /// <typeparam name="T">元素类型</typeparam>
    /// <exception cref="ArgumentNullException">当 <paramref name="source"/> 为 <see langword="null"/> 时抛出</exception>
    /// <exception cref="InvalidOperationException">当找不到 <paramref name="existingItem"/> 时抛出</exception>
    public static void InsertAfter<T>(this IList<T> source, T existingItem, T item)
    {
        source.InsertAfter(value => EqualityComparer<T>.Default.Equals(value, existingItem), item);
    }

    /// <summary>在第一个匹配谓词的元素之后插入元素</summary>
    /// <param name="source">要更新的列表</param>
    /// <param name="selector">用于选择锚点元素的谓词</param>
    /// <param name="item">要插入的元素</param>
    /// <typeparam name="T">元素类型</typeparam>
    /// <exception cref="ArgumentNullException">当 <paramref name="source"/> 或 <paramref name="selector"/> 为 <see langword="null"/> 时抛出</exception>
    /// <exception cref="InvalidOperationException">当没有元素匹配 <paramref name="selector"/> 时抛出</exception>
    public static void InsertAfter<T>(this IList<T> source, Predicate<T> selector, T item)
    {
        var index = source.FindRequiredIndex(selector);
        source.Insert(index + 1, item);
    }

    /// <summary>在第一个匹配的现有元素之前插入元素</summary>
    /// <param name="source">要更新的列表</param>
    /// <param name="existingItem">要查找的现有元素</param>
    /// <param name="item">要插入的元素</param>
    /// <typeparam name="T">元素类型</typeparam>
    /// <exception cref="ArgumentNullException">当 <paramref name="source"/> 为 <see langword="null"/> 时抛出</exception>
    /// <exception cref="InvalidOperationException">当找不到 <paramref name="existingItem"/> 时抛出</exception>
    public static void InsertBefore<T>(this IList<T> source, T existingItem, T item)
    {
        source.InsertBefore(value => EqualityComparer<T>.Default.Equals(value, existingItem), item);
    }

    /// <summary>在第一个匹配谓词的元素之前插入元素</summary>
    /// <param name="source">要更新的列表</param>
    /// <param name="selector">用于选择锚点元素的谓词</param>
    /// <param name="item">要插入的元素</param>
    /// <typeparam name="T">元素类型</typeparam>
    /// <exception cref="ArgumentNullException">当 <paramref name="source"/> 或 <paramref name="selector"/> 为 <see langword="null"/> 时抛出</exception>
    /// <exception cref="InvalidOperationException">当没有元素匹配 <paramref name="selector"/> 时抛出</exception>
    public static void InsertBefore<T>(this IList<T> source, Predicate<T> selector, T item)
    {
        var index = source.FindRequiredIndex(selector);
        source.Insert(index, item);
    }

    /// <summary>用给定元素替换每个匹配元素</summary>
    /// <param name="source">要更新的列表</param>
    /// <param name="selector">用于选择待替换元素的谓词</param>
    /// <param name="item">替换元素</param>
    /// <typeparam name="T">元素类型</typeparam>
    /// <exception cref="ArgumentNullException">当 <paramref name="source"/> 或 <paramref name="selector"/> 为 <see langword="null"/> 时抛出</exception>
    public static void ReplaceWhile<T>(this IList<T> source, Predicate<T> selector, T item)
    {
        source.ReplaceWhile(selector, _ => item);
    }

    /// <summary>用基于当前元素创建的值替换每个匹配元素</summary>
    /// <param name="source">要更新的列表</param>
    /// <param name="selector">用于选择待替换元素的谓词</param>
    /// <param name="itemFactory">接收当前元素的工厂</param>
    /// <typeparam name="T">元素类型</typeparam>
    /// <exception cref="ArgumentNullException">当 <paramref name="source"/>、<paramref name="selector"/> 或 <paramref name="itemFactory"/> 为 <see langword="null"/> 时抛出</exception>
    public static void ReplaceWhile<T>(this IList<T> source, Predicate<T> selector, Func<T, T> itemFactory)
    {
        var list = Check.NotNull(source);
        Check.NotNull(selector);
        Check.NotNull(itemFactory);

        for (var index = 0; index < list.Count; index++)
        {
            if (selector(list[index]))
            {
                list[index] = itemFactory(list[index]);
            }
        }
    }

    /// <summary>用给定元素替换第一个匹配元素</summary>
    /// <param name="source">要更新的列表</param>
    /// <param name="selector">用于选择待替换元素的谓词</param>
    /// <param name="item">替换元素</param>
    /// <typeparam name="T">元素类型</typeparam>
    /// <exception cref="ArgumentNullException">当 <paramref name="source"/> 或 <paramref name="selector"/> 为 <see langword="null"/> 时抛出</exception>
    /// <exception cref="InvalidOperationException">当没有元素匹配 <paramref name="selector"/> 时抛出</exception>
    public static void ReplaceOne<T>(this IList<T> source, Predicate<T> selector, T item)
    {
        source.ReplaceOne(selector, _ => item);
    }

    /// <summary>用基于当前元素创建的值替换第一个匹配元素</summary>
    /// <param name="source">要更新的列表</param>
    /// <param name="selector">用于选择待替换元素的谓词</param>
    /// <param name="itemFactory">接收当前元素的工厂</param>
    /// <typeparam name="T">元素类型</typeparam>
    /// <exception cref="ArgumentNullException">当 <paramref name="source"/>、<paramref name="selector"/> 或 <paramref name="itemFactory"/> 为 <see langword="null"/> 时抛出</exception>
    /// <exception cref="InvalidOperationException">当没有元素匹配 <paramref name="selector"/> 时抛出</exception>
    public static void ReplaceOne<T>(this IList<T> source, Predicate<T> selector, Func<T, T> itemFactory)
    {
        var index = source.FindRequiredIndex(selector);
        source[index] = Check.NotNull(itemFactory)(source[index]);
    }

    /// <summary>用另一个元素替换第一个相等元素</summary>
    /// <param name="source">要更新的列表</param>
    /// <param name="item">要查找的元素</param>
    /// <param name="replaceWith">替换元素</param>
    /// <typeparam name="T">元素类型</typeparam>
    /// <exception cref="ArgumentNullException">当 <paramref name="source"/> 为 <see langword="null"/> 时抛出</exception>
    /// <exception cref="InvalidOperationException">当找不到 <paramref name="item"/> 时抛出</exception>
    public static void ReplaceOne<T>(this IList<T> source, T item, T replaceWith)
    {
        source.ReplaceOne(value => EqualityComparer<T>.Default.Equals(value, item), replaceWith);
    }

    /// <summary>获取第一个匹配元素，或添加由工厂创建的元素</summary>
    /// <param name="source">要搜索并更新的列表</param>
    /// <param name="selector">用于选择现有元素的谓词</param>
    /// <param name="factory">仅在无匹配元素时调用的工厂</param>
    /// <typeparam name="T">元素类型</typeparam>
    /// <returns>现有元素或新增元素</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="source"/>、<paramref name="selector"/> 或 <paramref name="factory"/> 为 <see langword="null"/> 时抛出</exception>
    public static T GetOrAdd<T>(this IList<T> source, Func<T, bool> selector, Func<T> factory)
    {
        var list = Check.NotNull(source);
        Check.NotNull(selector);
        Check.NotNull(factory);

        foreach (var item in list)
        {
            if (selector(item))
            {
                return item;
            }
        }

        var newItem = factory();
        list.Add(newItem);
        return newItem;
    }

    /// <summary>将第一个匹配元素移动到目标索引</summary>
    /// <param name="source">要更新的列表</param>
    /// <param name="selector">用于选择待移动元素的谓词</param>
    /// <param name="targetIndex">原始列表范围内从零开始的目标索引</param>
    /// <typeparam name="T">元素类型</typeparam>
    /// <exception cref="ArgumentNullException">当 <paramref name="source"/> 或 <paramref name="selector"/> 为 <see langword="null"/> 时抛出</exception>
    /// <exception cref="ArgumentOutOfRangeException">当 <paramref name="targetIndex"/> 超出列表范围时抛出</exception>
    /// <exception cref="InvalidOperationException">当没有元素匹配 <paramref name="selector"/> 时抛出</exception>
    public static void MoveItem<T>(this List<T> source, Predicate<T> selector, int targetIndex)
    {
        var list = Check.NotNull(source);
        Check.NotNull(selector);

        if (targetIndex < 0 || targetIndex >= list.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(targetIndex), targetIndex, "目标索引必须位于列表范围内。");
        }

        var sourceIndex = list.FindIndex(selector);
        if (sourceIndex < 0)
        {
            throw new InvalidOperationException("未找到匹配元素。");
        }

        var item = list[sourceIndex];
        list.RemoveAt(sourceIndex);
        list.Insert(targetIndex, item);
    }

    private static int FindRequiredIndex<T>(this IList<T> source, Predicate<T> selector)
    {
        var index = source.FindIndex(selector);
        return index >= 0 ? index : throw new InvalidOperationException("未找到匹配元素。");
    }
}
