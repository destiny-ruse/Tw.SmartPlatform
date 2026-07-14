namespace Tw.Authorization;

/// <summary>
/// 汇集授权主体、租户、权限与资源范围信息，供权限检查边界作出决策
/// </summary>
/// <param name="SubjectId">授权主体标识</param>
/// <param name="TenantId">授权决策所属租户标识</param>
/// <param name="Permission">待检查的稳定权限名称</param>
/// <param name="ResourceType">资源级权限对应的资源类型；非资源权限为 null</param>
/// <param name="ResourceId">资源级权限对应的资源标识；非资源权限为 null</param>
/// <param name="Roles">授权主体当前具备的角色集合</param>
public sealed record AuthorizationContext(
    string SubjectId,
    string TenantId,
    string Permission,
    string? ResourceType,
    string? ResourceId,
    IReadOnlySet<string> Roles);
