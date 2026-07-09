namespace Tw.AspNetCore.Localization;

/// <summary>
/// 表示一条运行时导出的本地化文本条目，包含键名、已解析文本值及是否未找到资源的标志
/// </summary>
/// <param name="Name">文本条目的键名，例如 "Menu__Home"</param>
/// <param name="Value">已解析的文本值；未找到时等于 <paramref name="Name"/></param>
/// <param name="ResourceNotFound">指示是否未能找到对应资源；<see langword="true"/> 表示未找到</param>
public sealed record LocalizationTextDto(
    string Name,
    string Value,
    bool ResourceNotFound);
