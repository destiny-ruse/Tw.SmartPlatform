namespace Tw.Core.Extensions;

/// <summary>Provides extension methods for enumerable sequences.</summary>
public static class EnumerableExtensions
{
    /// <summary>Returns whether a sequence is <see langword="null"/> or contains no items.</summary>
    /// <param name="source">The sequence to inspect.</param>
    /// <typeparam name="T">The item type.</typeparam>
    /// <returns><see langword="true"/> when the sequence is <see langword="null"/> or empty.</returns>
    public static bool IsNullOrEmpty<T>(this IEnumerable<T>? source)
    {
        return source is null || !source.Any();
    }

    /// <summary>Joins strings using the supplied separator.</summary>
    /// <param name="source">The sequence to join.</param>
    /// <param name="separator">The separator to use; <see langword="null"/> is treated as an empty string.</param>
    /// <returns>The joined string.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is <see langword="null"/>.</exception>
    public static string JoinAsString(this IEnumerable<string> source, string? separator)
    {
        return string.Join(separator ?? string.Empty, Check.NotNull(source));
    }

    /// <summary>Joins item string representations using the supplied separator.</summary>
    /// <param name="source">The sequence to join.</param>
    /// <param name="separator">The separator to use; <see langword="null"/> is treated as an empty string.</param>
    /// <typeparam name="T">The item type.</typeparam>
    /// <returns>The joined string.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is <see langword="null"/>.</exception>
    public static string JoinAsString<T>(this IEnumerable<T> source, string? separator)
    {
        return string.Join(separator ?? string.Empty, Check.NotNull(source));
    }

    /// <summary>Invokes an action for each item.</summary>
    /// <param name="source">The sequence to enumerate.</param>
    /// <param name="action">The action to invoke.</param>
    /// <typeparam name="T">The item type.</typeparam>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> or <paramref name="action"/> is <see langword="null"/>.</exception>
    public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
    {
        Check.NotNull(source);
        Check.NotNull(action);

        foreach (var item in source)
        {
            action(item);
        }
    }

    /// <summary>Invokes an action for each item with its zero-based index.</summary>
    /// <param name="source">The sequence to enumerate.</param>
    /// <param name="action">The action to invoke.</param>
    /// <typeparam name="T">The item type.</typeparam>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> or <paramref name="action"/> is <see langword="null"/>.</exception>
    public static void ForEach<T>(this IEnumerable<T> source, Action<T, int> action)
    {
        Check.NotNull(source);
        Check.NotNull(action);

        var index = 0;
        foreach (var item in source)
        {
            action(item, index++);
        }
    }

    /// <summary>Invokes an asynchronous action for each item sequentially.</summary>
    /// <param name="source">The sequence to enumerate.</param>
    /// <param name="action">The asynchronous action to await for each item.</param>
    /// <typeparam name="T">The item type.</typeparam>
    /// <returns>A task that completes when all actions complete.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> or <paramref name="action"/> is <see langword="null"/>.</exception>
    public static async Task ForEachAsync<T>(this IEnumerable<T> source, Func<T, Task> action)
    {
        Check.NotNull(source);
        Check.NotNull(action);

        foreach (var item in source)
        {
            await action(item);
        }
    }

    /// <summary>Invokes an asynchronous action for each item with bounded parallelism.</summary>
    /// <param name="source">The sequence to enumerate.</param>
    /// <param name="action">The asynchronous action to await for each item.</param>
    /// <param name="maxDegreeOfParallelism">The maximum number of concurrent actions, or zero to use <see cref="Environment.ProcessorCount"/>.</param>
    /// <param name="cancellationToken">The token that cancels scheduling and waits.</param>
    /// <typeparam name="T">The item type.</typeparam>
    /// <returns>A task that completes when all scheduled actions complete.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> or <paramref name="action"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="maxDegreeOfParallelism"/> is negative.</exception>
    public static async Task ForEachParallelAsync<T>(
        this IEnumerable<T> source,
        Func<T, Task> action,
        int maxDegreeOfParallelism = 0,
        CancellationToken cancellationToken = default)
    {
        Check.NotNull(source);
        Check.NotNull(action);
        Check.NonNegative(maxDegreeOfParallelism);

        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = maxDegreeOfParallelism == 0 ? Environment.ProcessorCount : maxDegreeOfParallelism,
            CancellationToken = cancellationToken,
        };

        await Parallel.ForEachAsync(source, options, async (item, _) => await action(item));
    }

    /// <summary>Splits a sequence into materialized batches.</summary>
    /// <param name="source">The sequence to split.</param>
    /// <param name="batchSize">The maximum number of items in each batch.</param>
    /// <typeparam name="T">The item type.</typeparam>
    /// <returns>Materialized batches in source order.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="batchSize"/> is less than or equal to zero.</exception>
    public static IEnumerable<IEnumerable<T>> Batch<T>(this IEnumerable<T> source, int batchSize)
    {
        Check.NotNull(source);
        Check.Positive(batchSize);

        var batch = new List<T>(batchSize);
        foreach (var item in source)
        {
            batch.Add(item);
            if (batch.Count == batchSize)
            {
                yield return batch.ToArray();
                batch.Clear();
            }
        }

        if (batch.Count > 0)
        {
            yield return batch.ToArray();
        }
    }

    /// <summary>Filters a sequence only when the condition is true.</summary>
    /// <param name="source">The sequence to filter.</param>
    /// <param name="condition">Whether to apply the predicate.</param>
    /// <param name="predicate">The predicate to apply when <paramref name="condition"/> is true.</param>
    /// <typeparam name="T">The item type.</typeparam>
    /// <returns>The filtered sequence when enabled; otherwise, the original sequence.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> or <paramref name="predicate"/> is <see langword="null"/>.</exception>
    public static IEnumerable<T> WhereIf<T>(this IEnumerable<T> source, bool condition, Func<T, bool> predicate)
    {
        Check.NotNull(source);
        Check.NotNull(predicate);
        return condition ? source.Where(predicate) : source;
    }

    /// <summary>Filters a sequence with an indexed predicate only when the condition is true.</summary>
    /// <param name="source">The sequence to filter.</param>
    /// <param name="condition">Whether to apply the predicate.</param>
    /// <param name="predicate">The indexed predicate to apply when <paramref name="condition"/> is true.</param>
    /// <typeparam name="T">The item type.</typeparam>
    /// <returns>The filtered sequence when enabled; otherwise, the original sequence.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> or <paramref name="predicate"/> is <see langword="null"/>.</exception>
    public static IEnumerable<T> WhereIf<T>(this IEnumerable<T> source, bool condition, Func<T, int, bool> predicate)
    {
        Check.NotNull(source);
        Check.NotNull(predicate);
        return condition ? source.Where(predicate) : source;
    }

    /// <summary>Returns one page from a sequence using a one-based page number.</summary>
    /// <param name="source">The sequence to page.</param>
    /// <param name="pageNumber">The one-based page number.</param>
    /// <param name="pageSize">The number of items in a page.</param>
    /// <typeparam name="T">The item type.</typeparam>
    /// <returns>The requested page.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="pageNumber"/> or <paramref name="pageSize"/> is less than or equal to zero.</exception>
    public static IEnumerable<T> PageBy<T>(this IEnumerable<T> source, int pageNumber, int pageSize)
    {
        Check.NotNull(source);
        Check.Positive(pageNumber);
        Check.Positive(pageSize);
        return source.Skip((pageNumber - 1) * pageSize).Take(pageSize);
    }

    /// <summary>Returns the sequence as a read-only collection.</summary>
    /// <param name="source">The sequence to expose.</param>
    /// <typeparam name="T">The item type.</typeparam>
    /// <returns>The original read-only collection when possible; otherwise, a materialized array.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is <see langword="null"/>.</exception>
    public static IReadOnlyCollection<T> AsReadOnlyCollection<T>(this IEnumerable<T> source)
    {
        var sequence = Check.NotNull(source);
        return sequence as IReadOnlyCollection<T> ?? sequence.ToArray();
    }
}
