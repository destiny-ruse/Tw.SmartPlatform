using Tw.Localization;

namespace Tw.AspNetCore.Localization;

/// <summary>
/// 表示可读写当前请求本地化上下文的访问器接口
/// </summary>
/// <remarks>
/// 实现类应以 Scoped 生命周期注册，确保每个 HTTP 请求拥有独立的访问器实例。
/// </remarks>
public interface ICurrentLocalizationContextAccessor
{
    /// <summary>
    /// 当前请求的 <see cref="LocalizationContext"/>；未指定时为 <see langword="null"/>
    /// </summary>
    LocalizationContext? Current { get; set; }
}
