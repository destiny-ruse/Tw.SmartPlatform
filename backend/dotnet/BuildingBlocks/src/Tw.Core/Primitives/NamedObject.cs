namespace Tw.Core.Primitives;

/// <summary>
/// 为基元描述符提供具名基对象
/// </summary>
/// <param name="name">非空显示名或查找名</param>
/// <exception cref="ArgumentNullException">当 <paramref name="name"/> 为 <see langword="null"/> 时抛出</exception>
/// <exception cref="ArgumentException">当 <paramref name="name"/> 为空字符串或空白字符串时抛出</exception>
public class NamedObject(string name)
{
    /// <summary>
    /// 通过验证的名称
    /// </summary>
    public string Name { get; } = Check.NotNullOrWhiteSpace(name);
}
