using Tw.Localization;

namespace Tw.AspNetCore.Localization;

/// <summary>
/// 根据路由参数、查询字符串、Cookie 和 Accept-Language 标头，按优先级顺序解析当前请求应使用的文化
/// </summary>
public static class RequestCultureResolver
{
    /// <summary>
    /// 解析当前请求应使用的文化，并返回解析结果
    /// </summary>
    /// <param name="routeCulture">路由参数中传入的文化名称；无路由值时传 <see langword="null"/></param>
    /// <param name="queryCulture">查询字符串中传入的文化名称；无查询值时传 <see langword="null"/></param>
    /// <param name="cookieCulture">Cookie 中存储的文化名称；无 Cookie 时传 <see langword="null"/></param>
    /// <param name="acceptLanguageHeader">
    /// 请求的 Accept-Language 标头原始值；无标头时传 <see langword="null"/>。
    /// 按标头给定的顺序依次匹配，首个受支持的语言即为结果；
    /// 每个条目的 <c>;q=</c> 权重后缀会被剥离但不用于重排序，标头顺序即为权威顺序。
    /// </param>
    /// <param name="options">本地化配置，提供已支持的文化列表和默认文化</param>
    /// <returns>
    /// 包含最终选定文化名称和是否由用户显式切换标志的 <see cref="RequestCultureResolveResult"/>
    /// </returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="options"/> 为 <see langword="null"/> 时抛出</exception>
    public static RequestCultureResolveResult Resolve(
        string? routeCulture,
        string? queryCulture,
        string? cookieCulture,
        string? acceptLanguageHeader,
        LocalizationOptions options)
    {
        Check.NotNull(options);

        if (TrySelect(routeCulture, options, out var routeResult))
        {
            return new RequestCultureResolveResult(routeResult, true);
        }

        if (TrySelect(queryCulture, options, out var queryResult))
        {
            return new RequestCultureResolveResult(queryResult, true);
        }

        if (TrySelect(cookieCulture, options, out var cookieResult))
        {
            return new RequestCultureResolveResult(cookieResult, false);
        }

        foreach (var language in ParseAcceptLanguage(acceptLanguageHeader))
        {
            if (TrySelect(language, options, out var headerResult))
            {
                return new RequestCultureResolveResult(headerResult, false);
            }
        }

        return new RequestCultureResolveResult(options.DefaultCulture, false);
    }

    /// <summary>执行 TrySelect 操作</summary>
    /// <param name="value">value 参数</param>
    /// <param name="options">options 参数</param>
    /// <param name="cultureName">cultureName 参数</param>
    /// <returns>TrySelect 的执行结果</returns>
    private static bool TrySelect(string? value, LocalizationOptions options, out string cultureName)
    {
        cultureName = string.Empty;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var supported = options.SupportedCultures.FirstOrDefault(
            x => string.Equals(x, value, StringComparison.OrdinalIgnoreCase));
        if (supported is null)
        {
            return false;
        }

        cultureName = supported;
        return true;
    }

    /// <summary>执行 ParseAcceptLanguage 操作</summary>
    /// <param name="header">header 参数</param>
    /// <returns>ParseAcceptLanguage 的执行结果</returns>
    private static IEnumerable<string> ParseAcceptLanguage(string? header)
    {
        if (string.IsNullOrWhiteSpace(header))
        {
            yield break;
        }

        foreach (var item in header.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var culture = item.Split(';', 2, StringSplitOptions.TrimEntries)[0];
            if (!string.IsNullOrWhiteSpace(culture))
            {
                yield return culture;
            }
        }
    }
}
