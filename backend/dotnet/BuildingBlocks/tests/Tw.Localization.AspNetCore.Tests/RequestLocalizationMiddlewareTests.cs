using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Tw.Localization;
using Xunit;

namespace Tw.Localization.AspNetCore.Tests;

public class RequestLocalizationMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_WritesCurrentContext()
    {
        var options = new LocalizationOptions { DefaultCulture = "en-US", SupportedCultures = { "en-US", "zh-Hans" } };
        var accessor = new CurrentLocalizationContextAccessor();
        var middleware = new RequestLocalizationMiddleware(_ => Task.CompletedTask, options);
        var context = new DefaultHttpContext();
        context.Request.QueryString = new QueryString("?culture=zh-Hans");

        await middleware.InvokeAsync(context, accessor);

        accessor.Current!.CultureName.Should().Be("zh-Hans");
    }

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
}
