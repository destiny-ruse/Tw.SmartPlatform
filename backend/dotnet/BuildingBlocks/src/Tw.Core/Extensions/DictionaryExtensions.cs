using System.Collections.Concurrent;
using System.Dynamic;

namespace Tw.Core.Extensions;

/// <summary>Provides extension methods for dictionaries.</summary>
public static class DictionaryExtensions
{
    /// <summary>Gets a value by key or returns the default value for the value type.</summary>
    /// <param name="dictionary">The dictionary to read.</param>
    /// <param name="key">The key to find.</param>
    /// <typeparam name="TKey">The key type.</typeparam>
    /// <typeparam name="TValue">The value type.</typeparam>
    /// <returns>The stored value, or <see langword="default"/> when the key is absent.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="dictionary"/> is <see langword="null"/>.</exception>
    public static TValue? GetOrDefault<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key)
        where TKey : notnull
    {
        return Check.NotNull(dictionary).TryGetValue(key, out var value) ? value : default;
    }

    /// <summary>Gets a value by key or returns the default value for the value type.</summary>
    /// <param name="dictionary">The dictionary to read.</param>
    /// <param name="key">The key to find.</param>
    /// <typeparam name="TKey">The key type.</typeparam>
    /// <typeparam name="TValue">The value type.</typeparam>
    /// <returns>The stored value, or <see langword="default"/> when the key is absent.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="dictionary"/> is <see langword="null"/>.</exception>
    public static TValue? GetOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key)
    {
        return Check.NotNull(dictionary).TryGetValue(key, out var value) ? value : default;
    }

    /// <summary>Gets an existing value or adds a value created from the missing key.</summary>
    /// <param name="dictionary">The dictionary to update.</param>
    /// <param name="key">The key to find or add.</param>
    /// <param name="factory">The factory called once when the key is absent.</param>
    /// <typeparam name="TKey">The key type.</typeparam>
    /// <typeparam name="TValue">The value type.</typeparam>
    /// <returns>The existing or added value.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="dictionary"/> or <paramref name="factory"/> is <see langword="null"/>.</exception>
    public static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, Func<TKey, TValue> factory)
    {
        var map = Check.NotNull(dictionary);
        Check.NotNull(factory);

        if (map.TryGetValue(key, out var existingValue))
        {
            return existingValue;
        }

        var value = factory(key);
        map.Add(key, value);
        return value;
    }

    /// <summary>Gets an existing value or adds a factory-created value.</summary>
    /// <param name="dictionary">The dictionary to update.</param>
    /// <param name="key">The key to find or add.</param>
    /// <param name="factory">The factory called once when the key is absent.</param>
    /// <typeparam name="TKey">The key type.</typeparam>
    /// <typeparam name="TValue">The value type.</typeparam>
    /// <returns>The existing or added value.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="dictionary"/> or <paramref name="factory"/> is <see langword="null"/>.</exception>
    public static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, Func<TValue> factory)
    {
        Check.NotNull(factory);
        return dictionary.GetOrAdd(key, _ => factory());
    }

    /// <summary>Gets a value by key or returns the default value for the value type.</summary>
    /// <param name="dictionary">The dictionary to read.</param>
    /// <param name="key">The key to find.</param>
    /// <typeparam name="TKey">The key type.</typeparam>
    /// <typeparam name="TValue">The value type.</typeparam>
    /// <returns>The stored value, or <see langword="default"/> when the key is absent.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="dictionary"/> is <see langword="null"/>.</exception>
    public static TValue? GetOrDefault<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> dictionary, TKey key)
    {
        return Check.NotNull(dictionary).TryGetValue(key, out var value) ? value : default;
    }

    /// <summary>Gets a value by key or returns the default value for the value type.</summary>
    /// <param name="dictionary">The concurrent dictionary to read.</param>
    /// <param name="key">The key to find.</param>
    /// <typeparam name="TKey">The key type.</typeparam>
    /// <typeparam name="TValue">The value type.</typeparam>
    /// <returns>The stored value, or <see langword="default"/> when the key is absent.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="dictionary"/> is <see langword="null"/>.</exception>
    public static TValue? GetOrDefault<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> dictionary, TKey key)
        where TKey : notnull
    {
        return Check.NotNull(dictionary).TryGetValue(key, out var value) ? value : default;
    }

    /// <summary>Gets an existing concurrent value or adds a factory-created value.</summary>
    /// <param name="dictionary">The concurrent dictionary to update.</param>
    /// <param name="key">The key to find or add.</param>
    /// <param name="factory">The value factory used by <see cref="ConcurrentDictionary{TKey,TValue}.GetOrAdd(TKey,Func{TKey,TValue})"/>.</param>
    /// <typeparam name="TKey">The key type.</typeparam>
    /// <typeparam name="TValue">The value type.</typeparam>
    /// <returns>The existing or added value.</returns>
    /// <remarks>The concurrent dictionary may invoke <paramref name="factory"/> more than once under contention.</remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="dictionary"/> or <paramref name="factory"/> is <see langword="null"/>.</exception>
    public static TValue GetOrAdd<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> dictionary, TKey key, Func<TValue> factory)
        where TKey : notnull
    {
        var map = Check.NotNull(dictionary);
        Check.NotNull(factory);
        return map.GetOrAdd(key, _ => factory());
    }

    /// <summary>Converts a string/object dictionary to a dynamic object.</summary>
    /// <param name="dictionary">The dictionary to convert.</param>
    /// <returns>An <see cref="ExpandoObject"/> containing the dictionary values.</returns>
    /// <remarks>Nested <see cref="Dictionary{TKey,TValue}"/> values with string keys and object values are converted recursively.</remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="dictionary"/> is <see langword="null"/>.</exception>
    public static dynamic ConvertToDynamicObject(this Dictionary<string, object> dictionary)
    {
        var expando = new ExpandoObject();
        var target = (IDictionary<string, object?>)expando;

        foreach (var item in Check.NotNull(dictionary))
        {
            target[item.Key] = item.Value is Dictionary<string, object> nested
                ? ConvertToDynamicObject(nested)
                : item.Value;
        }

        return expando;
    }
}
