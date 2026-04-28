namespace Tw.Core.Utilities;

/// <summary>
/// Provides a reusable asynchronous disposable instance whose disposal has no effect.
/// </summary>
public sealed class NullAsyncDisposable : IAsyncDisposable
{
    /// <summary>
    /// Gets the shared no-op asynchronous disposable instance.
    /// </summary>
    public static NullAsyncDisposable Instance { get; } = new();

    private NullAsyncDisposable()
    {
    }

    /// <summary>
    /// Completes without performing any work.
    /// </summary>
    /// <returns>A completed value task.</returns>
    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }
}
