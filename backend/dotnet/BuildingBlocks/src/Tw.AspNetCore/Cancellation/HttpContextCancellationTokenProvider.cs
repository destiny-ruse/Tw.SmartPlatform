using Microsoft.AspNetCore.Http;
using Tw.DependencyInjection.Cancellation;

namespace Tw.AspNetCore.Cancellation;

/// <summary>
/// 从当前 HTTP 请求读取取消令牌的提供器
/// </summary>
public sealed class HttpContextCancellationTokenProvider(
    IHttpContextAccessor httpContextAccessor,
    CurrentCancellationTokenAccessor ambientAccessor) : ICancellationTokenProvider
{
    /// <inheritdoc />
    public CancellationToken Token => httpContextAccessor.HttpContext?.RequestAborted
                                      ?? ambientAccessor.Token;
}
