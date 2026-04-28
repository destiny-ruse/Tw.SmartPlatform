using Tw.Core;

namespace Tw.Core.Utilities;

/// <summary>
/// Invokes a supplied action when the instance is disposed.
/// </summary>
public sealed class DisposeAction : IDisposable
{
    private Action? action;

    /// <summary>
    /// Initializes a new instance of the <see cref="DisposeAction"/> class.
    /// </summary>
    /// <param name="action">The action to invoke during disposal.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="action"/> is <see langword="null"/>.</exception>
    public DisposeAction(Action action)
    {
        this.action = Check.NotNull(action);
    }

    /// <summary>
    /// Invokes the configured action at most once.
    /// </summary>
    public void Dispose()
    {
        Interlocked.Exchange(ref action, null)?.Invoke();
    }
}
