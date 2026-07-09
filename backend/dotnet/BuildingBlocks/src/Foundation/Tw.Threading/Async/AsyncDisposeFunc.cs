namespace Tw.Threading;

/// <summary>
/// 在实例释放时调用给定异步委托
/// </summary>
public sealed class AsyncDisposeFunc : IAsyncDisposable
{
    private Func<ValueTask>? disposeAsync;

    /// <summary>
    /// 初始化 <see cref="AsyncDisposeFunc"/> 类的新实例
    /// </summary>
    /// <param name="disposeAsync">释放期间要调用的异步函数</param>
    /// <exception cref="ArgumentNullException">当 <paramref name="disposeAsync"/> 为 <see langword="null"/> 时抛出</exception>
    public AsyncDisposeFunc(Func<Task> disposeAsync)
    {
        ArgumentNullException.ThrowIfNull(disposeAsync);
        this.disposeAsync = () => new ValueTask(disposeAsync.Invoke());
    }

    /// <summary>
    /// 初始化 <see cref="AsyncDisposeFunc"/> 类的新实例
    /// </summary>
    /// <param name="disposeAsync">释放期间要调用的异步函数</param>
    /// <exception cref="ArgumentNullException">当 <paramref name="disposeAsync"/> 为 <see langword="null"/> 时抛出</exception>
    public AsyncDisposeFunc(Func<ValueTask> disposeAsync)
    {
        ArgumentNullException.ThrowIfNull(disposeAsync);
        this.disposeAsync = disposeAsync;
    }

    /// <summary>
    /// 最多调用一次已配置的异步函数
    /// </summary>
    /// <returns>表示释放操作的值任务</returns>
    public ValueTask DisposeAsync()
    {
        var callback = Interlocked.Exchange(ref disposeAsync, null);

        return callback is null ? ValueTask.CompletedTask : callback();
    }
}
