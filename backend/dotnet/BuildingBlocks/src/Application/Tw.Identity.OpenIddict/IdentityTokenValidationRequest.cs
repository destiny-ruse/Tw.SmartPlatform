namespace Tw.Identity.OpenIddict;

/// <summary>
/// token 校验请求
/// </summary>
/// <param name="AccessToken">访问 token</param>
/// <param name="Audience">期望受众</param>
public sealed record IdentityTokenValidationRequest(string AccessToken, string Audience);
