using Microsoft.AspNetCore.Http;
using Tw.Threading;

namespace Tw.AspNetCore.Mvc.Context;

/// <summary>
/// 在 ASP.NET Core MVC/Razor Pages 中基于 <see cref="HttpContext.RequestAborted"/> 提供请求取消令牌
/// </summary>
/// <remarks>
/// 覆盖令牌优先；存在 <see cref="HttpContext"/> 时返回 <see cref="HttpContext.RequestAborted"/>；
/// 没有 <see cref="HttpContext"/> 且没有覆盖令牌时返回 <see cref="CancellationToken.None"/>。
/// </remarks>
public sealed class HttpContextCancellationTokenProvider : CancellationTokenProviderBase
{
    /// <summary>表示 _httpContextAccessor 字段</summary>
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>
    /// 初始化 <see cref="HttpContextCancellationTokenProvider"/> 类的新实例
    /// </summary>
    /// <param name="scopeProvider">维护异步作用域的取消令牌作用域 provider</param>
    /// <param name="httpContextAccessor">当前 <see cref="HttpContext"/> 访问器</param>
    /// <exception cref="ArgumentNullException">当 <paramref name="httpContextAccessor"/> 为 <see langword="null"/> 时抛出</exception>
    public HttpContextCancellationTokenProvider(
        AsyncLocalCancellationTokenScopeProvider scopeProvider,
        IHttpContextAccessor httpContextAccessor)
        : base(scopeProvider)
    {
        _httpContextAccessor = Check.NotNull(httpContextAccessor);
    }

    /// <inheritdoc />
    public override CancellationToken Token =>
        OverrideValue ?? _httpContextAccessor.HttpContext?.RequestAborted ?? CancellationToken.None;
}
