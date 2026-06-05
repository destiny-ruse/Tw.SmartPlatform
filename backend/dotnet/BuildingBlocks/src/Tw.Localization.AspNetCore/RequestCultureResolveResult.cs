namespace Tw.Localization.AspNetCore;

/// <summary>
/// 封装请求文化解析的结果，包含最终选定的文化名称和是否由用户显式切换的标志
/// </summary>
/// <param name="CultureName">已解析并规范化的 BCP 47 文化名称，取自 <see cref="Tw.Localization.LocalizationOptions.SupportedCultures"/> 的配置大小写</param>
/// <param name="IsExplicitSwitch">
/// 仅当文化来源为路由参数或查询字符串时为 <see langword="true"/>，表示用户主动切换了语言；
/// 来源为 Cookie、Accept-Language 标头或默认文化回退时均为 <see langword="false"/>。
/// </param>
public sealed record RequestCultureResolveResult(string CultureName, bool IsExplicitSwitch);
