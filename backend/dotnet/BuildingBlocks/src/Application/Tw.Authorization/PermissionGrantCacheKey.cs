namespace Tw.Authorization;

/// <summary>
/// 隔离不同主体、租户、权限与资源范围的权限 grant 缓存条目
/// </summary>
/// <param name="SubjectId">授权主体标识</param>
/// <param name="TenantId">授权决策所属租户标识</param>
/// <param name="Permission">待检查的稳定权限名称</param>
/// <param name="ResourceType">资源级权限对应的资源类型；非资源权限为 null</param>
/// <param name="ResourceId">资源级权限对应的资源标识；非资源权限为 null</param>
public sealed record PermissionGrantCacheKey(
    string SubjectId,
    string TenantId,
    string Permission,
    string? ResourceType,
    string? ResourceId);
