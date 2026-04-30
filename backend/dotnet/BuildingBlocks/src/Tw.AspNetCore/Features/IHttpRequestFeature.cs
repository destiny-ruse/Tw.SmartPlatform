using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Tw.AspNetCore.Features;

/// <summary>
/// 暴露给 Entry 拦截器的 HTTP 请求上下文特性
/// </summary>
public interface IHttpRequestFeature
{
    /// <summary>
    /// 当前 HTTP 上下文
    /// </summary>
    HttpContext HttpContext { get; }

    /// <summary>
    /// 当前 MVC Action 上下文
    /// </summary>
    ActionContext ActionContext { get; }
}
