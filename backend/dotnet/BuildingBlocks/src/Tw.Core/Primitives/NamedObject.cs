namespace Tw.Core.Primitives;

/// <summary>
/// Provides a named base object for primitive descriptors.
/// </summary>
/// <param name="name">The non-empty display or lookup name.</param>
/// <exception cref="ArgumentNullException">Thrown when <paramref name="name"/> is <see langword="null"/>.</exception>
/// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is empty or whitespace.</exception>
public class NamedObject(string name)
{
    /// <summary>
    /// Gets the validated name.
    /// </summary>
    public string Name { get; } = Check.NotNullOrWhiteSpace(name);
}
