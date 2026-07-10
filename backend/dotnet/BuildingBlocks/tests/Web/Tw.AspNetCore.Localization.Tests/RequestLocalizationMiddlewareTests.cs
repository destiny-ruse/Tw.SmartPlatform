using AwesomeAssertions;
using Microsoft.AspNetCore.Http;
using Tw.Localization;
using Xunit;

namespace Tw.AspNetCore.Localization.Tests;

/// <summary>
/// 覆盖请求本地化Middleware的核心行为和边界条件
/// </summary>
public class RequestLocalizationMiddlewareTests
{
    /// <summary>
    /// 验证nvoke异步写回Current上下文
    /// </summary>
    /// <returns>表示异步流程完成状态的任务</returns>
    [Fact]
    public async Task InvokeAsync_WritesCurrentContext()
    {
        var options = new LocalizationOptions { DefaultCulture = "en-US", SupportedCultures = { "en-US", "zh-Hans" } };
        var accessor = new CurrentLocalizationContextAccessor();
        System.Globalization.CultureInfo? capturedCulture = null;
        System.Globalization.CultureInfo? capturedUICulture = null;
        var middleware = new RequestLocalizationMiddleware(_ =>
        {
            capturedCulture = System.Globalization.CultureInfo.CurrentCulture;
            capturedUICulture = System.Globalization.CultureInfo.CurrentUICulture;
            return Task.CompletedTask;
        }, options);
        var context = new DefaultHttpContext();
        context.Request.QueryString = new QueryString("?culture=zh-Hans");

        await middleware.InvokeAsync(context, accessor);

        accessor.Current!.CultureName.Should().Be("zh-Hans");
        // 在 _next 内部捕获，确保读取到中间件设置的值（Windows 上 zh-Hans 规范化为 zh-CN）
        capturedCulture!.TwoLetterISOLanguageName.Should().Be("zh");
        capturedUICulture!.TwoLetterISOLanguageName.Should().Be("zh");
    }

    /// <summary>
    /// 验证nvoke异步写回Cookie针对ExplicitSwitch
    /// </summary>
    /// <returns>表示异步流程完成状态的任务</returns>
    [Fact]
    public async Task InvokeAsync_WritesCookieForExplicitSwitch()
    {
        var options = new LocalizationOptions { DefaultCulture = "en-US", SupportedCultures = { "en-US", "zh-Hans" } };
        var accessor = new CurrentLocalizationContextAccessor();
        var middleware = new RequestLocalizationMiddleware(_ => Task.CompletedTask, options);
        var context = new DefaultHttpContext();
        context.Request.QueryString = new QueryString("?culture=zh-Hans");

        await middleware.InvokeAsync(context, accessor);

        context.Response.Headers.SetCookie.ToString().Should().Contain(".Tw.Culture=zh-Hans");
    }

    /// <summary>
    /// 验证nvoke异步不WriteCookie当不ExplicitSwitch
    /// </summary>
    /// <returns>表示异步流程完成状态的任务</returns>
    [Fact]
    public async Task InvokeAsync_DoesNotWriteCookie_WhenNotExplicitSwitch()
    {
        var options = new LocalizationOptions { DefaultCulture = "en-US", SupportedCultures = { "en-US", "zh-Hans" } };
        var accessor = new CurrentLocalizationContextAccessor();
        var middleware = new RequestLocalizationMiddleware(_ => Task.CompletedTask, options);
        var context = new DefaultHttpContext();
        context.Request.Headers.Cookie = ".Tw.Culture=zh-Hans";

        await middleware.InvokeAsync(context, accessor);

        accessor.Current!.CultureName.Should().Be("zh-Hans");
        context.Response.Headers.SetCookie.ToString().Should().BeEmpty();
    }
}
