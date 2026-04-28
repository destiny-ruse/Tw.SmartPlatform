namespace Tw.Core.Primitives;

/// <summary>
/// Associates a validated name with a type matching predicate.
/// </summary>
/// <param name="name">The non-empty selector name.</param>
/// <param name="predicate">The predicate that evaluates candidate types.</param>
/// <exception cref="ArgumentNullException">Thrown when <paramref name="name"/> or <paramref name="predicate"/> is <see langword="null"/>.</exception>
/// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is empty or whitespace.</exception>
public class NamedTypeSelector(string name, Func<Type, bool> predicate) : NamedObject(name)
{
    /// <summary>
    /// Gets the validated type matching predicate.
    /// </summary>
    public Func<Type, bool> Predicate { get; } = Check.NotNull(predicate);
}
