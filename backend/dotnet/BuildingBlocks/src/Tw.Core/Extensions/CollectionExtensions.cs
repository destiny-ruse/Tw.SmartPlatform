namespace Tw.Core.Extensions;

/// <summary>Provides extension methods for mutable collections.</summary>
public static class CollectionExtensions
{
    /// <summary>Adds an item when the collection does not already contain it.</summary>
    /// <param name="source">The collection to update.</param>
    /// <param name="item">The item to add.</param>
    /// <typeparam name="T">The item type.</typeparam>
    /// <returns><see langword="true"/> when the item was added; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is <see langword="null"/>.</exception>
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

    /// <summary>Adds each missing item and returns the items that were added.</summary>
    /// <param name="source">The collection to update.</param>
    /// <param name="items">The candidate items to add. The sequence is enumerated once.</param>
    /// <typeparam name="T">The item type.</typeparam>
    /// <returns>The items that were added, in source enumeration order.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> or <paramref name="items"/> is <see langword="null"/>.</exception>
    public static IEnumerable<T> AddIfNotContains<T>(this ICollection<T> source, IEnumerable<T> items)
    {
        Check.NotNull(source);
        Check.NotNull(items);

        foreach (var item in items)
        {
            if (source.AddIfNotContains(item))
            {
                yield return item;
            }
        }
    }

    /// <summary>Adds a factory-created item when no existing item matches the predicate.</summary>
    /// <param name="source">The collection to update.</param>
    /// <param name="predicate">The predicate used to test existing items.</param>
    /// <param name="itemFactory">The factory used only when no item matches.</param>
    /// <typeparam name="T">The item type.</typeparam>
    /// <returns><see langword="true"/> when a new item was added; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/>, <paramref name="predicate"/>, or <paramref name="itemFactory"/> is <see langword="null"/>.</exception>
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

    /// <summary>Removes all matching items and returns them in their original enumeration order.</summary>
    /// <param name="source">The collection to update.</param>
    /// <param name="predicate">The predicate that selects items to remove.</param>
    /// <typeparam name="T">The item type.</typeparam>
    /// <returns>The removed items.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> or <paramref name="predicate"/> is <see langword="null"/>.</exception>
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

    /// <summary>Removes all provided items from the collection.</summary>
    /// <param name="source">The collection to update.</param>
    /// <param name="items">The items to remove. The sequence is snapshotted before mutation.</param>
    /// <typeparam name="T">The item type.</typeparam>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> or <paramref name="items"/> is <see langword="null"/>.</exception>
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
