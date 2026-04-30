using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Tw.AspNetCore.Features;

internal sealed class HttpRequestFeature(
    HttpContext httpContext,
    ActionContext actionContext) : IHttpRequestFeature
{
    public HttpContext HttpContext { get; } = httpContext;

    public ActionContext ActionContext { get; } = actionContext;
}
