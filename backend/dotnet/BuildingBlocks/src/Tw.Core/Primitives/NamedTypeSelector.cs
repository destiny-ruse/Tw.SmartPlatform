namespace Tw.Core.Primitives;

/// <summary>
/// 将通过验证的名称与类型匹配谓词关联
/// </summary>
/// <param name="name">非空选择器名称</param>
/// <param name="predicate">用于评估候选类型的谓词</param>
/// <exception cref="ArgumentNullException">当 <paramref name="name"/> 或 <paramref name="predicate"/> 为 <see langword="null"/> 时抛出</exception>
/// <exception cref="ArgumentException">当 <paramref name="name"/> 为空字符串或空白字符串时抛出</exception>
public class NamedTypeSelector(string name, Func<Type, bool> predicate) : NamedObject(name)
{
    /// <summary>
    /// 通过验证的类型匹配谓词
    /// </summary>
    public Func<Type, bool> Predicate { get; } = Check.NotNull(predicate);
}
