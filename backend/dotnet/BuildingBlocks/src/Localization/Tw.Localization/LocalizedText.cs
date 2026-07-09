namespace Tw.Localization;

/// <summary>
/// 表示一条已解析的本地化文本，包含文本值、来源及是否找到资源的标志
/// </summary>
/// <param name="ResourceName">资源集合的名称，例如 "App"</param>
/// <param name="Name">文本条目的键名，例如 "Menu.Home"</param>
/// <param name="Value">已解析的文本值；未找到时等于 <paramref name="Name"/></param>
/// <param name="CultureName">本次解析使用的 BCP 47 文化名称</param>
/// <param name="ResourceNotFound">指示是否未能找到对应资源；<see langword="true"/> 表示未找到</param>
/// <param name="Source">文本的实际来源渠道</param>
public sealed record LocalizedText(
    string ResourceName,
    string Name,
    string Value,
    string CultureName,
    bool ResourceNotFound,
    LocalizedTextSource Source)
{
    /// <summary>
    /// 创建一个表示"未找到"状态的 <see cref="LocalizedText"/> 实例；
    /// 此时 <see cref="Value"/> 等于 <paramref name="name"/>，
    /// <see cref="ResourceNotFound"/> 为 <see langword="true"/>，
    /// <see cref="Source"/> 为 <see cref="LocalizedTextSource.NotFound"/>
    /// </summary>
    /// <param name="resourceName">资源集合的名称</param>
    /// <param name="name">文本条目的键名</param>
    /// <param name="cultureName">查找时使用的 BCP 47 文化名称</param>
    /// <returns>表示未找到状态的 <see cref="LocalizedText"/> 实例</returns>
    public static LocalizedText NotFound(string resourceName, string name, string cultureName) =>
        new(resourceName, name, name, cultureName, true, LocalizedTextSource.NotFound);
}
