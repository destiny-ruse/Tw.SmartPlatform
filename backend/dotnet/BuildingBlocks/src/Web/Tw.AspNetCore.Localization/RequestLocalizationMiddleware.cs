using System.Globalization;
using Microsoft.AspNetCore.Http;
using Tw.Localization;

namespace Tw.AspNetCore.Localization;

/// <summary>
/// 在每个 HTTP 请求进入管道时，根据路由参数、查询字符串、Cookie 和 Accept-Language 标头
/// 解析目标文化，并将其写入 <see cref="ICurrentLocalizationContextAccessor"/>、
/// <see cref="CultureInfo.CurrentCulture"/> 和 <see cref="CultureInfo.CurrentUICulture"/>
/// </summary>
/// <remarks>
/// 当文化由路由或查询字符串显式指定（<see cref="RequestCultureResolveResult.IsExplicitSwitch"/> 为 <see langword="true"/>）时，
/// 同时向响应写入 <see cref="CultureCookieName"/> Cookie，以便客户端在后续请求中携带已选择的文化。
/// </remarks>
public sealed class RequestLocalizationMiddleware
{
    /// <summary>
    /// 存储用户已选文化的 Cookie 名称
    /// </summary>
    public const string CultureCookieName = ".Tw.Culture";

    /// <summary>
    /// 保存当前类型处理流程依赖的next
    /// </summary>
    private readonly RequestDelegate _next;
    /// <summary>
    /// 保存当前类型处理流程依赖的选项
    /// </summary>
    private readonly LocalizationOptions _options;

    /// <summary>
    /// 初始化 <see cref="RequestLocalizationMiddleware"/> 类的新实例
    /// </summary>
    /// <param name="next">管道中的下一个请求处理委托</param>
    /// <param name="options">本地化配置，提供支持的文化列表和默认文化</param>
    /// <exception cref="ArgumentNullException">
    /// 当 <paramref name="next"/> 或 <paramref name="options"/> 为 <see langword="null"/> 时抛出
    /// </exception>
    public RequestLocalizationMiddleware(RequestDelegate next, LocalizationOptions options)
    {
        _next = Check.NotNull(next);
        _options = Check.NotNull(options);
    }

    /// <summary>
    /// 处理当前 HTTP 请求：解析文化并设置本地化上下文，然后将请求传递给管道中的下一个组件
    /// </summary>
    /// <param name="context">当前 HTTP 上下文</param>
    /// <param name="accessor">当前请求本地化上下文的访问器</param>
    /// <returns>表示异步操作的 <see cref="Task"/></returns>
    /// <exception cref="ArgumentNullException">
    /// 当 <paramref name="context"/> 或 <paramref name="accessor"/> 为 <see langword="null"/> 时抛出
    /// </exception>
    /// <remarks>
    /// 仅当文化由路由或查询字符串显式切换时写入 Cookie；Cookie 作用域为全站（<c>Path = "/"</c>），
    /// 有效期约 1 年（<c>MaxAge = 365 天</c>），确保用户语言偏好在后续访问中保持一致。
    /// </remarks>
    public async Task InvokeAsync(HttpContext context, ICurrentLocalizationContextAccessor accessor)
    {
        Check.NotNull(context);
        Check.NotNull(accessor);

        var routeCulture = context.Request.RouteValues.TryGetValue("culture", out var routeValue)
            ? Convert.ToString(routeValue, CultureInfo.InvariantCulture)
            : null;
        var queryCulture = context.Request.Query["culture"].FirstOrDefault();
        var cookieCulture = context.Request.Cookies[CultureCookieName];
        var headerCulture = context.Request.Headers.AcceptLanguage.ToString();

        var result = RequestCultureResolver.Resolve(
            routeCulture,
            queryCulture,
            cookieCulture,
            headerCulture,
            _options);

        accessor.Current = new LocalizationContext(result.CultureName);
        var cultureInfo = CultureInfo.GetCultureInfo(result.CultureName);
        CultureInfo.CurrentCulture = cultureInfo;
        CultureInfo.CurrentUICulture = cultureInfo;

        if (result.IsExplicitSwitch)
        {
            context.Response.Cookies.Append(
                CultureCookieName,
                result.CultureName,
                new CookieOptions
                {
                    Path = "/",
                    MaxAge = TimeSpan.FromDays(365),
                });
        }

        await _next(context);
    }
}
