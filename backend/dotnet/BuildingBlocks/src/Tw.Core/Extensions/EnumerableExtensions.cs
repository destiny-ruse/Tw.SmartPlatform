namespace Tw.Core.Extensions;

/// <summary>提供可枚举序列的扩展方法</summary>
public static class EnumerableExtensions
{
    /// <summary>返回序列是否为 <see langword="null"/> 或不包含任何元素</summary>
    /// <param name="source">要检查的序列</param>
    /// <typeparam name="T">元素类型</typeparam>
    /// <returns>序列为 <see langword="null"/> 或空序列时返回 <see langword="true"/></returns>
    public static bool IsNullOrEmpty<T>(this IEnumerable<T>? source)
    {
        return source is null || !source.Any();
    }

    /// <summary>使用给定分隔符拼接字符串</summary>
    /// <param name="source">要拼接的序列</param>
    /// <param name="separator">要使用的分隔符；<see langword="null"/> 会按空字符串处理</param>
    /// <returns>拼接后的字符串</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="source"/> 为 <see langword="null"/> 时抛出</exception>
    public static string JoinAsString(this IEnumerable<string> source, string? separator)
    {
        return string.Join(separator ?? string.Empty, Check.NotNull(source));
    }

    /// <summary>使用给定分隔符拼接元素的字符串表示</summary>
    /// <param name="source">要拼接的序列</param>
    /// <param name="separator">要使用的分隔符；<see langword="null"/> 会按空字符串处理</param>
    /// <typeparam name="T">元素类型</typeparam>
    /// <returns>拼接后的字符串</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="source"/> 为 <see langword="null"/> 时抛出</exception>
    public static string JoinAsString<T>(this IEnumerable<T> source, string? separator)
    {
        return string.Join(separator ?? string.Empty, Check.NotNull(source));
    }

    /// <summary>为每个元素调用操作</summary>
    /// <param name="source">要枚举的序列</param>
    /// <param name="action">要调用的操作</param>
    /// <typeparam name="T">元素类型</typeparam>
    /// <exception cref="ArgumentNullException">当 <paramref name="source"/> 或 <paramref name="action"/> 为 <see langword="null"/> 时抛出</exception>
    public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
    {
        Check.NotNull(source);
        Check.NotNull(action);

        foreach (var item in source)
        {
            action(item);
        }
    }

    /// <summary>为每个元素及其从零开始索引调用操作</summary>
    /// <param name="source">要枚举的序列</param>
    /// <param name="action">要调用的操作</param>
    /// <typeparam name="T">元素类型</typeparam>
    /// <exception cref="ArgumentNullException">当 <paramref name="source"/> 或 <paramref name="action"/> 为 <see langword="null"/> 时抛出</exception>
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

    /// <summary>按顺序为每个元素调用异步操作</summary>
    /// <param name="source">要枚举的序列</param>
    /// <param name="action">每个元素要等待的异步操作</param>
    /// <typeparam name="T">元素类型</typeparam>
    /// <returns>所有操作完成时结束的任务</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="source"/> 或 <paramref name="action"/> 为 <see langword="null"/> 时抛出</exception>
    public static async Task ForEachAsync<T>(this IEnumerable<T> source, Func<T, Task> action)
    {
        Check.NotNull(source);
        Check.NotNull(action);

        foreach (var item in source)
        {
            await action(item);
        }
    }

    /// <summary>以受限并行度为每个元素调用异步操作</summary>
    /// <param name="source">要枚举的序列</param>
    /// <param name="action">每个元素要等待的异步操作</param>
    /// <param name="maxDegreeOfParallelism">最大并发操作数；为零时使用 <see cref="Environment.ProcessorCount"/></param>
    /// <param name="cancellationToken">取消调度和等待的令牌</param>
    /// <typeparam name="T">元素类型</typeparam>
    /// <returns>所有已调度操作完成时结束的任务</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="source"/> 或 <paramref name="action"/> 为 <see langword="null"/> 时抛出</exception>
    /// <exception cref="ArgumentOutOfRangeException">当 <paramref name="maxDegreeOfParallelism"/> 为负数时抛出</exception>
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

    /// <summary>将序列拆分为已物化的批次</summary>
    /// <param name="source">要拆分的序列</param>
    /// <param name="batchSize">每个批次的最大元素数</param>
    /// <typeparam name="T">元素类型</typeparam>
    /// <returns>按源顺序排列的已物化批次</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="source"/> 为 <see langword="null"/> 时抛出</exception>
    /// <exception cref="ArgumentOutOfRangeException">当 <paramref name="batchSize"/> 小于或等于零时抛出</exception>
    public static IEnumerable<IEnumerable<T>> Batch<T>(this IEnumerable<T> source, int batchSize)
    {
        Check.NotNull(source);
        Check.Positive(batchSize);

        return BatchIterator(source, batchSize);
    }

    private static IEnumerable<IEnumerable<T>> BatchIterator<T>(IEnumerable<T> source, int batchSize)
    {
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

    /// <summary>仅在条件为真时筛选序列</summary>
    /// <param name="source">要筛选的序列</param>
    /// <param name="condition">是否应用谓词</param>
    /// <param name="predicate">当 <paramref name="condition"/> 为真时应用的谓词</param>
    /// <typeparam name="T">元素类型</typeparam>
    /// <returns>启用时返回筛选后的序列；否则返回原始序列</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="source"/> 或 <paramref name="predicate"/> 为 <see langword="null"/> 时抛出</exception>
    public static IEnumerable<T> WhereIf<T>(this IEnumerable<T> source, bool condition, Func<T, bool> predicate)
    {
        Check.NotNull(source);
        Check.NotNull(predicate);
        return condition ? source.Where(predicate) : source;
    }

    /// <summary>仅在条件为真时使用带索引谓词筛选序列</summary>
    /// <param name="source">要筛选的序列</param>
    /// <param name="condition">是否应用谓词</param>
    /// <param name="predicate">当 <paramref name="condition"/> 为真时应用的带索引谓词</param>
    /// <typeparam name="T">元素类型</typeparam>
    /// <returns>启用时返回筛选后的序列；否则返回原始序列</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="source"/> 或 <paramref name="predicate"/> 为 <see langword="null"/> 时抛出</exception>
    public static IEnumerable<T> WhereIf<T>(this IEnumerable<T> source, bool condition, Func<T, int, bool> predicate)
    {
        Check.NotNull(source);
        Check.NotNull(predicate);
        return condition ? source.Where(predicate) : source;
    }

    /// <summary>使用从一开始的页码返回序列中的一页</summary>
    /// <param name="source">要分页的序列</param>
    /// <param name="pageNumber">从一开始的页码</param>
    /// <param name="pageSize">每页元素数</param>
    /// <typeparam name="T">元素类型</typeparam>
    /// <returns>请求的页</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="source"/> 为 <see langword="null"/> 时抛出</exception>
    /// <exception cref="ArgumentOutOfRangeException">当 <paramref name="pageNumber"/> 或 <paramref name="pageSize"/> 小于或等于零时抛出</exception>
    public static IEnumerable<T> PageBy<T>(this IEnumerable<T> source, int pageNumber, int pageSize)
    {
        Check.NotNull(source);
        Check.Positive(pageNumber);
        Check.Positive(pageSize);

        var offset = (long)(pageNumber - 1) * pageSize;
        if (offset > int.MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(pageNumber), pageNumber, "计算出的分页偏移超出支持范围。");
        }

        return source.Skip((int)offset).Take(pageSize);
    }

    /// <summary>将序列作为只读集合返回</summary>
    /// <param name="source">要暴露的序列</param>
    /// <typeparam name="T">元素类型</typeparam>
    /// <returns>可行时返回原始只读集合；否则返回已物化数组</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="source"/> 为 <see langword="null"/> 时抛出</exception>
    public static IReadOnlyCollection<T> AsReadOnlyCollection<T>(this IEnumerable<T> source)
    {
        var sequence = Check.NotNull(source);
        return sequence as IReadOnlyCollection<T> ?? sequence.ToArray();
    }
}
