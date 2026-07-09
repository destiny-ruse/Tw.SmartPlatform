namespace Tw.Identity.OpenIddict;

/// <summary>
/// token 校验结果
/// </summary>
/// <param name="Succeeded">校验是否成功</param>
/// <param name="SubjectId">校验成功后的主体标识</param>
/// <param name="Scopes">token 授权范围</param>
/// <param name="Code">稳定结果码</param>
public sealed record IdentityTokenValidationResult(
    bool Succeeded,
    string? SubjectId,
    IReadOnlySet<string> Scopes,
    string Code);
