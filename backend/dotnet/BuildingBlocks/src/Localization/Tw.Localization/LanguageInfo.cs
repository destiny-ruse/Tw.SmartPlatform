namespace Tw.Localization;

/// <summary>
/// 表示系统支持的一种语言的元数据，包含文化名称、UI 文化名称、显示名称及启用状态
/// </summary>
public sealed class LanguageInfo
{
    /// <summary>
    /// 初始化 <see cref="LanguageInfo"/> 类的新实例
    /// </summary>
    /// <param name="cultureName">BCP 47 语言标记，例如 "zh-Hans"、"en-US"</param>
    /// <exception cref="ArgumentNullException">当 <paramref name="cultureName"/> 为 <see langword="null"/> 时抛出</exception>
    /// <exception cref="ArgumentException">当 <paramref name="cultureName"/> 为空字符串或空白字符串时抛出</exception>
    public LanguageInfo(string cultureName)
    {
        CultureName = Check.NotNullOrWhiteSpace(cultureName);
        UiCultureName = CultureName;
    }

    /// <summary>
    /// 获取 BCP 47 文化名称，例如 "zh-Hans"、"en-US"
    /// </summary>
    public string CultureName { get; }

    /// <summary>
    /// 获取或初始化 UI 文化名称；未显式设置时默认与 <see cref="CultureName"/> 相同
    /// </summary>
    public string UiCultureName { get; init; }

    /// <summary>
    /// 获取或初始化在界面上展示给用户的语言显示名称，例如 "简体中文"
    /// </summary>
    public string? DisplayName { get; init; }

    /// <summary>
    /// 获取或初始化一个值，指示该语言是否处于启用状态；默认为 <see langword="true"/>
    /// </summary>
    public bool IsEnabled { get; init; } = true;

    /// <summary>
    /// 获取或初始化语言在列表中的排序顺序；值越小越靠前
    /// </summary>
    public int SortOrder { get; init; }
}
