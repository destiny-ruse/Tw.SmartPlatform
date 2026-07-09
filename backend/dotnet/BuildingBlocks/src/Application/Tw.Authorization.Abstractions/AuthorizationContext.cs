namespace Tw.Authorization.Abstractions;

/// <summary>
/// 权限检查上下文
/// </summary>
/// <param name="SubjectId">授权主体标识</param>
/// <param name="TenantId">租户标识</param>
/// <param name="Permission">权限名称</param>
/// <param name="ResourceType">资源类型</param>
/// <param name="ResourceId">资源标识</param>
/// <param name="Roles">主体角色集合</param>
public sealed record AuthorizationContext(
    string SubjectId,
    string TenantId,
    string Permission,
    string? ResourceType,
    string? ResourceId,
    IReadOnlySet<string> Roles);
