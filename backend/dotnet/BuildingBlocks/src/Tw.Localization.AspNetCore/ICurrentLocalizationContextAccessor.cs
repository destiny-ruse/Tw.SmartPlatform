using Tw.Localization;

namespace Tw.Localization.AspNetCore;

/// <summary>
/// 表示可读写当前请求本地化上下文的访问器接口
/// </summary>
public interface ICurrentLocalizationContextAccessor
{
    /// <summary>
    /// 获取或设置当前请求的 <see cref="LocalizationContext"/>；未设置时为 <see langword="null"/>
    /// </summary>
    LocalizationContext? Current { get; set; }
}
