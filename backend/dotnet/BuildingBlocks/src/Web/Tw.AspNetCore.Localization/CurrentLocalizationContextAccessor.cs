using Tw.Localization;

namespace Tw.AspNetCore.Localization;

/// <summary>
/// <see cref="ICurrentLocalizationContextAccessor"/> 的默认实现，用简单属性存储当前请求的本地化上下文
/// </summary>
public sealed class CurrentLocalizationContextAccessor : ICurrentLocalizationContextAccessor
{
    /// <summary>
    /// 当前请求的 <see cref="LocalizationContext"/>；未指定时为 <see langword="null"/>
    /// </summary>
    public LocalizationContext? Current { get; set; }
}
