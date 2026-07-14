namespace Tw.Authorization;

/// <summary>
/// 先读取权限 grant 缓存，未命中时查询授权存储并回填判断结果
/// </summary>
public sealed class PermissionChecker : IPermissionChecker
{
    /// <summary>
    /// 权限拒绝时供调用方稳定映射的错误码
    /// </summary>
    private const string DeniedCode = "AUTHORIZATION:000001";

    /// <summary>
    /// 权限拒绝时可安全传递到协议边界的提示文本
    /// </summary>
    private const string DeniedMessage = "没有操作权限";

    /// <summary>
    /// 权限缓存未命中时读取授权记录的存储边界
    /// </summary>
    private readonly IGrantStore _grantStore;

    /// <summary>
    /// 保存权限允许或拒绝判断的缓存边界
    /// </summary>
    private readonly IPermissionGrantCache _grantCache;

    /// <summary>
    /// 初始化依赖指定授权存储与缓存的权限检查器
    /// </summary>
    /// <param name="grantStore">权限缓存未命中时使用的授权记录存储</param>
    /// <param name="grantCache">读取和回填权限判断的授权缓存</param>
    /// <exception cref="ArgumentNullException">任一依赖为 null 时抛出</exception>
    public PermissionChecker(IGrantStore grantStore, IPermissionGrantCache grantCache)
    {
        _grantStore = grantStore ?? throw new ArgumentNullException(nameof(grantStore));
        _grantCache = grantCache ?? throw new ArgumentNullException(nameof(grantCache));
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentNullException">context 为 null 时抛出</exception>
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
