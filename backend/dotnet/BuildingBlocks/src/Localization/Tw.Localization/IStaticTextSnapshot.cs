namespace Tw.Localization;

/// <summary>
/// 表示本地化资源的静态（编译期或启动期加载）文本快照，
/// 提供同步的文本查找能力，适用于性能敏感路径
/// </summary>
public interface IStaticTextSnapshot
{
    /// <summary>
    /// 在静态快照中查找单条本地化文本
    /// </summary>
    /// <param name="resourceName">资源集合的名称，例如 "App"</param>
    /// <param name="name">文本条目的键名，例如 "Menu.Home"</param>
    /// <param name="candidateCultureNames">按优先级排列的候选文化名称列表；查找时将按顺序依次尝试，直到找到匹配项</param>
    /// <returns>找到时返回对应的 <see cref="LocalizedText"/>；否则返回 <see langword="null"/></returns>
    LocalizedText? Find(
        string resourceName,
        string name,
        IReadOnlyList<string> candidateCultureNames);

    /// <summary>
    /// 获取指定资源集合在候选文化列表下的所有本地化文本条目
    /// </summary>
    /// <param name="resourceName">资源集合的名称，例如 "App"</param>
    /// <param name="candidateCultureNames">按优先级排列的候选文化名称列表</param>
    /// <returns>以文本条目键名为键、<see cref="LocalizedText"/> 为值的只读字典</returns>
    IReadOnlyDictionary<string, LocalizedText> GetAll(
        string resourceName,
        IReadOnlyList<string> candidateCultureNames);
}
