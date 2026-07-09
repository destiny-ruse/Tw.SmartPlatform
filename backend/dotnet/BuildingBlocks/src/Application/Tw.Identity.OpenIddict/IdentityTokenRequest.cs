namespace Tw.Identity.OpenIddict;

/// <summary>
/// token 发行请求
/// </summary>
/// <param name="SubjectId">主体标识</param>
/// <param name="ClientId">客户端标识</param>
/// <param name="Scopes">请求授权范围</param>
public sealed record IdentityTokenRequest(
    string SubjectId,
    string ClientId,
    IReadOnlySet<string> Scopes);
