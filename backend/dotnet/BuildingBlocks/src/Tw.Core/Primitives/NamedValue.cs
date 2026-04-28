namespace Tw.Core.Primitives;

/// <summary>
/// Stores a named string value.
/// </summary>
/// <param name="name">The non-empty value name.</param>
/// <param name="value">The string value.</param>
/// <exception cref="ArgumentNullException">Thrown when <paramref name="name"/> is <see langword="null"/>.</exception>
/// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is empty or whitespace.</exception>
[Serializable]
public class NamedValue(string name, string value) : NamedValue<string>(name, value);

/// <summary>
/// Stores a named value.
/// </summary>
/// <typeparam name="T">The value type.</typeparam>
/// <param name="name">The non-empty value name.</param>
/// <param name="value">The value to store.</param>
/// <exception cref="ArgumentNullException">Thrown when <paramref name="name"/> is <see langword="null"/>.</exception>
/// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is empty or whitespace.</exception>
[Serializable]
public class NamedValue<T>(string name, T value)
{
    /// <summary>
    /// Gets the validated name.
    /// </summary>
    public string Name { get; } = Check.NotNullOrWhiteSpace(name);

    /// <summary>
    /// Gets the stored value.
    /// </summary>
    public T Value { get; } = value;
}
