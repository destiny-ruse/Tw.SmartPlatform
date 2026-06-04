namespace Tw.Localization;

/// <summary>
/// 封装一次本地化查找所需的上下文信息，包括目标文化、租户标识和回退策略
/// </summary>
public sealed class LocalizationContext
{
    /// <summary>
    /// 初始化 <see cref="LocalizationContext"/> 类的新实例
    /// </summary>
    /// <param name="cultureName">BCP 47 语言标记，表示本次查找期望使用的文化，例如 "zh-Hans"</param>
    /// <exception cref="ArgumentNullException">当 <paramref name="cultureName"/> 为 <see langword="null"/> 时抛出</exception>
    /// <exception cref="ArgumentException">当 <paramref name="cultureName"/> 为空字符串或空白字符串时抛出</exception>
    public LocalizationContext(string cultureName)
    {
        CultureName = Check.NotNullOrWhiteSpace(cultureName);
    }

    /// <summary>
    /// 获取本次查找期望使用的 BCP 47 文化名称，例如 "zh-Hans"
    /// </summary>
    public string CultureName { get; }

    /// <summary>
    /// 获取或初始化当前操作所属的租户标识；为 <see langword="null"/> 时表示全局范围
    /// </summary>
    public string? TenantId { get; init; }

    /// <summary>
    /// 获取或初始化一个值，指示当指定文化没有翻译时是否回退到父文化；默认为 <see langword="true"/>
    /// </summary>
    public bool FallbackToParentCultures { get; init; } = true;

    /// <summary>
    /// 获取或初始化一个值，指示回退父文化仍未找到时是否再回退到系统默认文化；默认为 <see langword="true"/>
    /// </summary>
    public bool FallbackToDefaultCulture { get; init; } = true;
}
