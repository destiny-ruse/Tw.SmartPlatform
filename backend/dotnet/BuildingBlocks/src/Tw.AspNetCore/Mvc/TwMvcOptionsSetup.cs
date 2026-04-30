using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Tw.AspNetCore.Mvc;

/// <summary>
/// 配置 MVC 全局 Entry 拦截 Filter
/// </summary>
public sealed class TwMvcOptionsSetup : IConfigureOptions<MvcOptions>
{
    /// <inheritdoc />
    public void Configure(MvcOptions options)
    {
        if (options.Filters.OfType<ServiceFilterAttribute>()
            .Any(f => f.ServiceType == typeof(TwEntryInterceptorFilter)))
        {
            return;
        }

        options.Filters.AddService<TwEntryInterceptorFilter>();
    }
}
