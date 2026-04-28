namespace Tw.Core.Primitives;

/// <summary>
/// Provides convenience methods for adding named type selectors.
/// </summary>
public static class NamedTypeSelectorListExtensions
{
    /// <summary>
    /// Adds a selector that matches exactly the supplied type.
    /// </summary>
    /// <param name="selectors">The selector collection to update.</param>
    /// <param name="name">The non-empty selector name.</param>
    /// <param name="type">The type to match exactly.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="selectors"/>, <paramref name="name"/>, or <paramref name="type"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is empty or whitespace.</exception>
    public static void Add(this ICollection<NamedTypeSelector> selectors, string name, Type type)
    {
        var validatedType = Check.NotNull(type);

        selectors.Add(name, candidate => candidate == validatedType);
    }

    /// <summary>
    /// Adds a selector that uses the supplied predicate.
    /// </summary>
    /// <param name="selectors">The selector collection to update.</param>
    /// <param name="name">The non-empty selector name.</param>
    /// <param name="predicate">The predicate that evaluates candidate types.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="selectors"/>, <paramref name="name"/>, or <paramref name="predicate"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is empty or whitespace.</exception>
    public static void Add(this ICollection<NamedTypeSelector> selectors, string name, Func<Type, bool> predicate)
    {
        Check.NotNull(selectors).Add(new NamedTypeSelector(name, predicate));
    }
}
