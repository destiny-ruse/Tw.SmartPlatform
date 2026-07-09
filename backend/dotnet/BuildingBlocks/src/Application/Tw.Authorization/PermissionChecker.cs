using Tw.Authorization.Abstractions;

namespace Tw.Authorization;

/// <summary>
/// 基于 grant store 与 grant cache 的权限检查器
/// </summary>
public sealed class PermissionChecker : IPermissionChecker
{
    /// <summary>表示 DeniedCode 常量</summary>
    private const string DeniedCode = "AUTHORIZATION:000001";
    /// <summary>表示 DeniedMessage 常量</summary>
    private const string DeniedMessage = "没有操作权限";

    /// <summary>表示 _grantStore 字段</summary>
    private readonly IGrantStore _grantStore;
    /// <summary>表示 _grantCache 字段</summary>
    private readonly IPermissionGrantCache _grantCache;

    /// <summary>
    /// 初始化权限检查器
    /// </summary>
    /// <param name="grantStore">权限 grant 存储读取边界</param>
    /// <param name="grantCache">权限 grant 缓存边界</param>
    /// <exception cref="ArgumentNullException">任一参数为 null 时抛出</exception>
    public PermissionChecker(IGrantStore grantStore, IPermissionGrantCache grantCache)
    {
        _grantStore = grantStore ?? throw new ArgumentNullException(nameof(grantStore));
        _grantCache = grantCache ?? throw new ArgumentNullException(nameof(grantCache));
    }

    /// <inheritdoc />
    public async Task<AuthorizationResult> CheckAsync(
        AuthorizationContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        var key = new PermissionGrantCacheKey(
            context.SubjectId,
            context.TenantId,
            context.Permission,
            context.ResourceType,
            context.ResourceId);

        var cached = await _grantCache.GetAsync(key, cancellationToken);
        if (cached is not null)
        {
            return cached.Value
                ? AuthorizationResult.Success()
                : AuthorizationResult.Denied(DeniedCode, DeniedMessage);
        }

        var allowed = await _grantStore.HasGrantAsync(context, cancellationToken);
        await _grantCache.SetAsync(key, allowed, cancellationToken);

        return allowed
            ? AuthorizationResult.Success()
            : AuthorizationResult.Denied(DeniedCode, DeniedMessage);
    }
}
