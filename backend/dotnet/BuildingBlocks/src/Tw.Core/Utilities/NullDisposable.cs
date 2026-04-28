namespace Tw.Core.Utilities;

/// <summary>
/// Provides a reusable disposable instance whose disposal has no effect.
/// </summary>
public sealed class NullDisposable : IDisposable
{
    /// <summary>
    /// Gets the shared no-op disposable instance.
    /// </summary>
    public static NullDisposable Instance { get; } = new();

    private NullDisposable()
    {
    }

    /// <summary>
    /// Performs no operation.
    /// </summary>
    public void Dispose()
    {
    }
}
