using Microsoft.AspNetCore.Builder;

namespace Tw.Localization.AspNetCore;

/// <summary>
/// 为 <see cref="IApplicationBuilder"/> 提供本地化中间件注册扩展
/// </summary>
public static class LocalizationApplicationBuilderExtensions
{
    /// <summary>
    /// 将 <see cref="RequestLocalizationMiddleware"/> 注册到请求管道，
    /// 使每个请求均自动完成文化解析与上下文写入
    /// </summary>
    /// <param name="app">应用程序构建器</param>
    /// <returns>同一 <see cref="IApplicationBuilder"/> 实例，便于链式调用</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="app"/> 为 <see langword="null"/> 时抛出</exception>
    public static IApplicationBuilder UseLocalization(this IApplicationBuilder app)
    {
        Check.NotNull(app);

        return app.UseMiddleware<RequestLocalizationMiddleware>();
    }
}
