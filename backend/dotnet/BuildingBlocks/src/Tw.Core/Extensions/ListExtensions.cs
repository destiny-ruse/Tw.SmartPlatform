namespace Tw.Core.Extensions;

/// <summary>Provides extension methods for mutable lists.</summary>
public static class ListExtensions
{
    /// <summary>Returns a read-only view or snapshot of a list.</summary>
    /// <param name="source">The list to expose.</param>
    /// <typeparam name="T">The item type.</typeparam>
    /// <returns>A read-only list.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is <see langword="null"/>.</exception>
    public static IReadOnlyList<T> AsReadOnly<T>(this IList<T> source)
    {
        var list = Check.NotNull(source);
        return list as IReadOnlyList<T> ?? list.ToArray();
    }

    /// <summary>Inserts a sequence of items starting at an index.</summary>
    /// <param name="source">The list to update.</param>
    /// <param name="index">The insertion index.</param>
    /// <param name="items">The items to insert.</param>
    /// <typeparam name="T">The item type.</typeparam>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> or <paramref name="items"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="index"/> is outside the valid insertion range.</exception>
    public static void InsertRange<T>(this IList<T> source, int index, IEnumerable<T> items)
    {
        var list = Check.NotNull(source);
        Check.NotNull(items);

        if (index < 0 || index > list.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index), index, "Index must be within the list insertion range.");
        }

        foreach (var item in items.ToList())
        {
            list.Insert(index++, item);
        }
    }

    /// <summary>Finds the first index matching a predicate.</summary>
    /// <param name="source">The list to search.</param>
    /// <param name="selector">The predicate to match.</param>
    /// <typeparam name="T">The item type.</typeparam>
    /// <returns>The zero-based index, or -1 when no item matches.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> or <paramref name="selector"/> is <see langword="null"/>.</exception>
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

    /// <summary>Adds an item at the beginning of a list.</summary>
    /// <param name="source">The list to update.</param>
    /// <param name="item">The item to add.</param>
    /// <typeparam name="T">The item type.</typeparam>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is <see langword="null"/>.</exception>
    public static void AddFirst<T>(this IList<T> source, T item)
    {
        Check.NotNull(source).Insert(0, item);
    }

    /// <summary>Adds an item at the end of a list.</summary>
    /// <param name="source">The list to update.</param>
    /// <param name="item">The item to add.</param>
    /// <typeparam name="T">The item type.</typeparam>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is <see langword="null"/>.</exception>
    public static void AddLast<T>(this IList<T> source, T item)
    {
        Check.NotNull(source).Add(item);
    }

    /// <summary>Inserts an item after the first matching existing item.</summary>
    /// <param name="source">The list to update.</param>
    /// <param name="existingItem">The existing item to find.</param>
    /// <param name="item">The item to insert.</param>
    /// <typeparam name="T">The item type.</typeparam>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">Thrown when <paramref name="existingItem"/> is not found.</exception>
    public static void InsertAfter<T>(this IList<T> source, T existingItem, T item)
    {
        source.InsertAfter(value => EqualityComparer<T>.Default.Equals(value, existingItem), item);
    }

    /// <summary>Inserts an item after the first item matching a predicate.</summary>
    /// <param name="source">The list to update.</param>
    /// <param name="selector">The predicate that selects the anchor item.</param>
    /// <param name="item">The item to insert.</param>
    /// <typeparam name="T">The item type.</typeparam>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> or <paramref name="selector"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">Thrown when no item matches <paramref name="selector"/>.</exception>
    public static void InsertAfter<T>(this IList<T> source, Predicate<T> selector, T item)
    {
        var index = source.FindRequiredIndex(selector);
        source.Insert(index + 1, item);
    }

    /// <summary>Inserts an item before the first matching existing item.</summary>
    /// <param name="source">The list to update.</param>
    /// <param name="existingItem">The existing item to find.</param>
    /// <param name="item">The item to insert.</param>
    /// <typeparam name="T">The item type.</typeparam>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">Thrown when <paramref name="existingItem"/> is not found.</exception>
    public static void InsertBefore<T>(this IList<T> source, T existingItem, T item)
    {
        source.InsertBefore(value => EqualityComparer<T>.Default.Equals(value, existingItem), item);
    }

    /// <summary>Inserts an item before the first item matching a predicate.</summary>
    /// <param name="source">The list to update.</param>
    /// <param name="selector">The predicate that selects the anchor item.</param>
    /// <param name="item">The item to insert.</param>
    /// <typeparam name="T">The item type.</typeparam>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> or <paramref name="selector"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">Thrown when no item matches <paramref name="selector"/>.</exception>
    public static void InsertBefore<T>(this IList<T> source, Predicate<T> selector, T item)
    {
        var index = source.FindRequiredIndex(selector);
        source.Insert(index, item);
    }

    /// <summary>Replaces every matching item with the supplied item.</summary>
    /// <param name="source">The list to update.</param>
    /// <param name="selector">The predicate that selects items to replace.</param>
    /// <param name="item">The replacement item.</param>
    /// <typeparam name="T">The item type.</typeparam>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> or <paramref name="selector"/> is <see langword="null"/>.</exception>
    public static void ReplaceWhile<T>(this IList<T> source, Predicate<T> selector, T item)
    {
        source.ReplaceWhile(selector, _ => item);
    }

    /// <summary>Replaces every matching item with a value created from the current item.</summary>
    /// <param name="source">The list to update.</param>
    /// <param name="selector">The predicate that selects items to replace.</param>
    /// <param name="itemFactory">The factory that receives the current item.</param>
    /// <typeparam name="T">The item type.</typeparam>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/>, <paramref name="selector"/>, or <paramref name="itemFactory"/> is <see langword="null"/>.</exception>
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

    /// <summary>Replaces the first matching item with the supplied item.</summary>
    /// <param name="source">The list to update.</param>
    /// <param name="selector">The predicate that selects the item to replace.</param>
    /// <param name="item">The replacement item.</param>
    /// <typeparam name="T">The item type.</typeparam>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> or <paramref name="selector"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">Thrown when no item matches <paramref name="selector"/>.</exception>
    public static void ReplaceOne<T>(this IList<T> source, Predicate<T> selector, T item)
    {
        source.ReplaceOne(selector, _ => item);
    }

    /// <summary>Replaces the first matching item with a value created from the current item.</summary>
    /// <param name="source">The list to update.</param>
    /// <param name="selector">The predicate that selects the item to replace.</param>
    /// <param name="itemFactory">The factory that receives the current item.</param>
    /// <typeparam name="T">The item type.</typeparam>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/>, <paramref name="selector"/>, or <paramref name="itemFactory"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">Thrown when no item matches <paramref name="selector"/>.</exception>
    public static void ReplaceOne<T>(this IList<T> source, Predicate<T> selector, Func<T, T> itemFactory)
    {
        var index = source.FindRequiredIndex(selector);
        source[index] = Check.NotNull(itemFactory)(source[index]);
    }

    /// <summary>Replaces the first equal item with another item.</summary>
    /// <param name="source">The list to update.</param>
    /// <param name="item">The item to find.</param>
    /// <param name="replaceWith">The replacement item.</param>
    /// <typeparam name="T">The item type.</typeparam>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">Thrown when <paramref name="item"/> is not found.</exception>
    public static void ReplaceOne<T>(this IList<T> source, T item, T replaceWith)
    {
        source.ReplaceOne(value => EqualityComparer<T>.Default.Equals(value, item), replaceWith);
    }

    /// <summary>Gets the first matching item or adds a factory-created item.</summary>
    /// <param name="source">The list to search and update.</param>
    /// <param name="selector">The predicate that selects an existing item.</param>
    /// <param name="factory">The factory called only when no item matches.</param>
    /// <typeparam name="T">The item type.</typeparam>
    /// <returns>The existing or added item.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/>, <paramref name="selector"/>, or <paramref name="factory"/> is <see langword="null"/>.</exception>
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

    /// <summary>Moves the first matching item to a target index.</summary>
    /// <param name="source">The list to update.</param>
    /// <param name="selector">The predicate that selects the item to move.</param>
    /// <param name="targetIndex">The zero-based target index in the original list range.</param>
    /// <typeparam name="T">The item type.</typeparam>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> or <paramref name="selector"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="targetIndex"/> is outside the list range.</exception>
    /// <exception cref="InvalidOperationException">Thrown when no item matches <paramref name="selector"/>.</exception>
    public static void MoveItem<T>(this List<T> source, Predicate<T> selector, int targetIndex)
    {
        var list = Check.NotNull(source);
        Check.NotNull(selector);

        if (targetIndex < 0 || targetIndex >= list.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(targetIndex), targetIndex, "Target index must be within the list range.");
        }

        var sourceIndex = list.FindIndex(selector);
        if (sourceIndex < 0)
        {
            throw new InvalidOperationException("No matching item was found.");
        }

        var item = list[sourceIndex];
        list.RemoveAt(sourceIndex);
        list.Insert(targetIndex, item);
    }

    private static int FindRequiredIndex<T>(this IList<T> source, Predicate<T> selector)
    {
        var index = source.FindIndex(selector);
        return index >= 0 ? index : throw new InvalidOperationException("No matching item was found.");
    }
}
