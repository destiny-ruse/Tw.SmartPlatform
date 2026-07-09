namespace Tw.Core.Primitives;

/// <summary>
/// 将通过验证的名称与操作委托关联
/// </summary>
/// <typeparam name="T">操作参数类型</typeparam>
/// <param name="name">非空操作名称</param>
/// <param name="action">要调用的操作委托</param>
/// <exception cref="ArgumentNullException">当 <paramref name="name"/> 或 <paramref name="action"/> 为 <see langword="null"/> 时抛出</exception>
/// <exception cref="ArgumentException">当 <paramref name="name"/> 为空字符串或空白字符串时抛出</exception>
public class NamedAction<T>(string name, Action<T> action) : NamedObject(name)
{
    /// <summary>
    /// 通过验证的操作委托
    /// </summary>
    public Action<T> Action { get; } = Check.NotNull(action);
}
