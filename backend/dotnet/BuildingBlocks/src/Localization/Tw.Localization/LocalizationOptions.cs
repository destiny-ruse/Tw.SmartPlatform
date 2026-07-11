namespace Tw.Localization;

/// <summary>
/// 配置本地化子系统的行为，包括默认文化、支持语言列表、资源路径和回退策略
/// </summary>
public sealed class LocalizationOptions
{
    /// <summary>
    /// 系统默认文化的 BCP 47 名称；当所有回退均失败时使用该文化，默认为 "zh-Hans"
    /// </summary>
    public string DefaultCulture { get; set; } = "zh-Hans";

    /// <summary>
    /// 已支持文化的 BCP 47 名称列表
    /// </summary>
    public List<string> SupportedCultures { get; } = [];

    /// <summary>
    /// JSON 资源文件的路径列表
    /// </summary>
    public List<string> JsonResourcePaths { get; } = [];

    /// <summary>
    /// 运行时监视 JSON 资源文件变更的开关；默认为 <see langword="false"/>
    /// </summary>
    public bool WatchJsonFiles { get; set; }

    /// <summary>
    /// 本地化键缺失时的回退行为；默认为 <see cref="MissingTextBehavior.ReturnKey"/>
    /// </summary>
    public MissingTextBehavior MissingTextBehavior { get; set; } = MissingTextBehavior.ReturnKey;

    /// <summary>
    /// 系统层面回退到父文化的开关；默认为 <see langword="true"/>
    /// </summary>
    public bool FallbackToParentCultures { get; set; } = true;

    /// <summary>
    /// 系统层面回退到默认文化的开关；默认为 <see langword="true"/>
    /// </summary>
    public bool FallbackToDefaultCulture { get; set; } = true;

    /// <summary>
    /// 不同资源文件包含相同键的允许策略；默认为 <see langword="true"/>
    /// </summary>
    public bool AllowDuplicateResourceKeys { get; set; } = true;

    /// <summary>
    /// 校验当前配置的合法性
    /// </summary>
    /// <exception cref="LocalizationConfigurationException">
    /// 当 <see cref="DefaultCulture"/> 不是合法的 BCP 47 文化名称时抛出；
    /// 当 <see cref="SupportedCultures"/> 为空时抛出；
    /// 当 <see cref="SupportedCultures"/> 中包含非法文化名称时抛出；
    /// 或当 <see cref="DefaultCulture"/> 不在 <see cref="SupportedCultures"/> 中时抛出
    /// </exception>
    public void Validate()
    {
        if (!CultureFallback.IsValidCulture(DefaultCulture))
        {
            throw new LocalizationConfigurationException($"默认 culture 无效：{DefaultCulture}");
        }

        if (SupportedCultures.Count == 0)
        {
            throw new LocalizationConfigurationException("支持语言列表不能为空");
        }

        foreach (var culture in SupportedCultures)
        {
            if (!CultureFallback.IsValidCulture(culture))
            {
                throw new LocalizationConfigurationException($"支持语言 culture 无效：{culture}");
            }
        }

        if (!SupportedCultures.Contains(DefaultCulture, StringComparer.OrdinalIgnoreCase))
        {
            throw new LocalizationConfigurationException($"默认 culture 必须包含在支持语言列表中：{DefaultCulture}");
        }
    }
}
