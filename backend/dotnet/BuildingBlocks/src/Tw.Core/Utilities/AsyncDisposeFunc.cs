using Tw.Core;

namespace Tw.Core.Utilities;

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
        var validatedDisposeAsync = Check.NotNull(disposeAsync);
        this.disposeAsync = () => new ValueTask(validatedDisposeAsync.Invoke());
    }

    /// <summary>
    /// 初始化 <see cref="AsyncDisposeFunc"/> 类的新实例
    /// </summary>
    /// <param name="disposeAsync">释放期间要调用的异步函数</param>
    /// <exception cref="ArgumentNullException">当 <paramref name="disposeAsync"/> 为 <see langword="null"/> 时抛出</exception>
    public AsyncDisposeFunc(Func<ValueTask> disposeAsync)
    {
        this.disposeAsync = Check.NotNull(disposeAsync);
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
