namespace Tw.Core.Primitives;

/// <summary>
/// Associates a validated name with an action delegate.
/// </summary>
/// <typeparam name="T">The action argument type.</typeparam>
/// <param name="name">The non-empty action name.</param>
/// <param name="action">The action delegate to invoke.</param>
/// <exception cref="ArgumentNullException">Thrown when <paramref name="name"/> or <paramref name="action"/> is <see langword="null"/>.</exception>
/// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is empty or whitespace.</exception>
public class NamedAction<T>(string name, Action<T> action) : NamedObject(name)
{
    /// <summary>
    /// Gets the validated action delegate.
    /// </summary>
    public Action<T> Action { get; } = Check.NotNull(action);
}
