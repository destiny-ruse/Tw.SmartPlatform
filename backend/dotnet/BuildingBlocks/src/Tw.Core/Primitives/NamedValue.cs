namespace Tw.Core.Primitives;

/// <summary>
/// 存储具名字符串值
/// </summary>
/// <param name="name">非空值名称</param>
/// <param name="value">字符串值</param>
/// <exception cref="ArgumentNullException">当 <paramref name="name"/> 为 <see langword="null"/> 时抛出</exception>
/// <exception cref="ArgumentException">当 <paramref name="name"/> 为空字符串或空白字符串时抛出</exception>
[Serializable]
public class NamedValue(string name, string value) : NamedValue<string>(name, value);

/// <summary>
/// 存储具名值
/// </summary>
/// <typeparam name="T">值类型</typeparam>
/// <param name="name">非空值名称</param>
/// <param name="value">要存储的值</param>
/// <exception cref="ArgumentNullException">当 <paramref name="name"/> 为 <see langword="null"/> 时抛出</exception>
/// <exception cref="ArgumentException">当 <paramref name="name"/> 为空字符串或空白字符串时抛出</exception>
[Serializable]
public class NamedValue<T>(string name, T value)
{
    /// <summary>
    /// 通过验证的名称
    /// </summary>
    public string Name { get; } = Check.NotNullOrWhiteSpace(name);

    /// <summary>
    /// 已存储的值
    /// </summary>
    public T Value { get; } = value;
}
