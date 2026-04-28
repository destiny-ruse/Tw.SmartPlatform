using Tw.Core;

namespace Tw.Core.Utilities;

/// <summary>
/// Invokes a supplied asynchronous delegate when the instance is disposed.
/// </summary>
public sealed class AsyncDisposeFunc : IAsyncDisposable
{
    private Func<ValueTask>? disposeAsync;

    /// <summary>
    /// Initializes a new instance of the <see cref="AsyncDisposeFunc"/> class.
    /// </summary>
    /// <param name="disposeAsync">The asynchronous function to invoke during disposal.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="disposeAsync"/> is <see langword="null"/>.</exception>
    public AsyncDisposeFunc(Func<Task> disposeAsync)
    {
        var validatedDisposeAsync = Check.NotNull(disposeAsync);
        this.disposeAsync = () => new ValueTask(validatedDisposeAsync.Invoke());
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AsyncDisposeFunc"/> class.
    /// </summary>
    /// <param name="disposeAsync">The asynchronous function to invoke during disposal.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="disposeAsync"/> is <see langword="null"/>.</exception>
    public AsyncDisposeFunc(Func<ValueTask> disposeAsync)
    {
        this.disposeAsync = Check.NotNull(disposeAsync);
    }

    /// <summary>
    /// Invokes the configured asynchronous function at most once.
    /// </summary>
    /// <returns>A value task that represents the disposal operation.</returns>
    public ValueTask DisposeAsync()
    {
        var callback = Interlocked.Exchange(ref disposeAsync, null);

        return callback is null ? ValueTask.CompletedTask : callback();
    }
}
