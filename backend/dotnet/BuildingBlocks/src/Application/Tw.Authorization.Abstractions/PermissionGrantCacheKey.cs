namespace Tw.Authorization.Abstractions;

/// <summary>
/// 权限 grant 缓存键
/// </summary>
/// <param name="SubjectId">授权主体标识</param>
/// <param name="TenantId">租户标识</param>
/// <param name="Permission">权限名称</param>
/// <param name="ResourceType">资源类型</param>
/// <param name="ResourceId">资源标识</param>
public sealed record PermissionGrantCacheKey(
    string SubjectId,
    string TenantId,
    string Permission,
    string? ResourceType,
    string? ResourceId);
