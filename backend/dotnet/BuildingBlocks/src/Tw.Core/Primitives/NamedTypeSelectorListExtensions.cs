namespace Tw.Core.Primitives;

/// <summary>
/// 提供添加具名类型选择器的便利方法
/// </summary>
public static class NamedTypeSelectorListExtensions
{
    /// <summary>
    /// 添加一个精确匹配给定类型的选择器
    /// </summary>
    /// <param name="selectors">要更新的选择器集合</param>
    /// <param name="name">非空选择器名称</param>
    /// <param name="type">要精确匹配的类型</param>
    /// <exception cref="ArgumentNullException">当 <paramref name="selectors"/>、<paramref name="name"/> 或 <paramref name="type"/> 为 <see langword="null"/> 时抛出</exception>
    /// <exception cref="ArgumentException">当 <paramref name="name"/> 为空字符串或空白字符串时抛出</exception>
    public static void Add(this ICollection<NamedTypeSelector> selectors, string name, Type type)
    {
        var validatedType = Check.NotNull(type);

        selectors.Add(name, candidate => candidate == validatedType);
    }

    /// <summary>
    /// 添加一个使用给定谓词的选择器
    /// </summary>
    /// <param name="selectors">要更新的选择器集合</param>
    /// <param name="name">非空选择器名称</param>
    /// <param name="predicate">用于评估候选类型的谓词</param>
    /// <exception cref="ArgumentNullException">当 <paramref name="selectors"/>、<paramref name="name"/> 或 <paramref name="predicate"/> 为 <see langword="null"/> 时抛出</exception>
    /// <exception cref="ArgumentException">当 <paramref name="name"/> 为空字符串或空白字符串时抛出</exception>
    public static void Add(this ICollection<NamedTypeSelector> selectors, string name, Func<Type, bool> predicate)
    {
        Check.NotNull(selectors).Add(new NamedTypeSelector(name, predicate));
    }
}
