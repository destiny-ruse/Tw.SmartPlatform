using System.Collections.Concurrent;
using System.Dynamic;

namespace Tw.Core.Extensions;

/// <summary>提供字典扩展方法</summary>
public static class DictionaryExtensions
{
    /// <summary>按键获取值；键不存在时返回值类型默认值</summary>
    /// <param name="dictionary">要读取的字典</param>
    /// <param name="key">要查找的键</param>
    /// <typeparam name="TKey">键类型</typeparam>
    /// <typeparam name="TValue">值类型</typeparam>
    /// <returns>已存储的值；键不存在时返回 <see langword="default"/></returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="dictionary"/> 为 <see langword="null"/> 时抛出</exception>
    public static TValue? GetOrDefault<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key)
        where TKey : notnull
    {
        return Check.NotNull(dictionary).TryGetValue(key, out var value) ? value : default;
    }

    /// <summary>按键获取值；键不存在时返回值类型默认值</summary>
    /// <param name="dictionary">要读取的字典</param>
    /// <param name="key">要查找的键</param>
    /// <typeparam name="TKey">键类型</typeparam>
    /// <typeparam name="TValue">值类型</typeparam>
    /// <returns>已存储的值；键不存在时返回 <see langword="default"/></returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="dictionary"/> 为 <see langword="null"/> 时抛出</exception>
    public static TValue? GetOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key)
    {
        return Check.NotNull(dictionary).TryGetValue(key, out var value) ? value : default;
    }

    /// <summary>获取现有值，或添加由缺失键创建的值</summary>
    /// <param name="dictionary">要更新的字典</param>
    /// <param name="key">要查找或添加的键</param>
    /// <param name="factory">键不存在时调用一次的工厂</param>
    /// <typeparam name="TKey">键类型</typeparam>
    /// <typeparam name="TValue">值类型</typeparam>
    /// <returns>现有值或新增值</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="dictionary"/> 或 <paramref name="factory"/> 为 <see langword="null"/> 时抛出</exception>
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

    /// <summary>获取现有值，或添加由工厂创建的值</summary>
    /// <param name="dictionary">要更新的字典</param>
    /// <param name="key">要查找或添加的键</param>
    /// <param name="factory">键不存在时调用一次的工厂</param>
    /// <typeparam name="TKey">键类型</typeparam>
    /// <typeparam name="TValue">值类型</typeparam>
    /// <returns>现有值或新增值</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="dictionary"/> 或 <paramref name="factory"/> 为 <see langword="null"/> 时抛出</exception>
    public static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, Func<TValue> factory)
    {
        Check.NotNull(factory);
        return dictionary.GetOrAdd(key, _ => factory());
    }

    /// <summary>按键获取值；键不存在时返回值类型默认值</summary>
    /// <param name="dictionary">要读取的字典</param>
    /// <param name="key">要查找的键</param>
    /// <typeparam name="TKey">键类型</typeparam>
    /// <typeparam name="TValue">值类型</typeparam>
    /// <returns>已存储的值；键不存在时返回 <see langword="default"/></returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="dictionary"/> 为 <see langword="null"/> 时抛出</exception>
    public static TValue? GetOrDefault<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> dictionary, TKey key)
    {
        return Check.NotNull(dictionary).TryGetValue(key, out var value) ? value : default;
    }

    /// <summary>按键获取值；键不存在时返回值类型默认值</summary>
    /// <param name="dictionary">要读取的并发字典</param>
    /// <param name="key">要查找的键</param>
    /// <typeparam name="TKey">键类型</typeparam>
    /// <typeparam name="TValue">值类型</typeparam>
    /// <returns>已存储的值；键不存在时返回 <see langword="default"/></returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="dictionary"/> 为 <see langword="null"/> 时抛出</exception>
    public static TValue? GetOrDefault<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> dictionary, TKey key)
        where TKey : notnull
    {
        return Check.NotNull(dictionary).TryGetValue(key, out var value) ? value : default;
    }

    /// <summary>获取现有并发值，或添加由工厂创建的值</summary>
    /// <param name="dictionary">要更新的并发字典</param>
    /// <param name="key">要查找或添加的键</param>
    /// <param name="factory"><see cref="ConcurrentDictionary{TKey,TValue}.GetOrAdd(TKey,Func{TKey,TValue})"/> 使用的值工厂</param>
    /// <typeparam name="TKey">键类型</typeparam>
    /// <typeparam name="TValue">值类型</typeparam>
    /// <returns>现有值或新增值</returns>
    /// <remarks>发生竞争时，并发字典可能多次调用 <paramref name="factory"/></remarks>
    /// <exception cref="ArgumentNullException">当 <paramref name="dictionary"/> 或 <paramref name="factory"/> 为 <see langword="null"/> 时抛出</exception>
    public static TValue GetOrAdd<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> dictionary, TKey key, Func<TValue> factory)
        where TKey : notnull
    {
        var map = Check.NotNull(dictionary);
        Check.NotNull(factory);
        return map.GetOrAdd(key, _ => factory());
    }

    /// <summary>将字符串到对象的字典转换为动态对象</summary>
    /// <param name="dictionary">要转换的字典</param>
    /// <returns>包含字典值的 <see cref="ExpandoObject"/></returns>
    /// <remarks>具有字符串键和对象值的嵌套字典会递归转换；其他键类型的字典保持不变</remarks>
    /// <exception cref="ArgumentNullException">当 <paramref name="dictionary"/> 为 <see langword="null"/> 时抛出</exception>
    public static dynamic ConvertToDynamicObject(this Dictionary<string, object> dictionary)
    {
        return ConvertStringObjectDictionary(Check.NotNull(dictionary));
    }

    private static ExpandoObject ConvertStringObjectDictionary(IEnumerable<KeyValuePair<string, object>> dictionary)
    {
        var expando = new ExpandoObject();
        var target = (IDictionary<string, object?>)expando;

        foreach (var item in dictionary)
        {
            target[item.Key] = ConvertDynamicValue(item.Value);
        }

        return expando;
    }

    private static object? ConvertDynamicValue(object? value)
    {
        return value switch
        {
            IDictionary<string, object> dictionary => ConvertStringObjectDictionary(dictionary),
            IReadOnlyDictionary<string, object> dictionary => ConvertStringObjectDictionary(dictionary),
            _ => value,
        };
    }
}
