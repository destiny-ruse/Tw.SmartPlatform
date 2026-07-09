namespace Tw.Authorization.Abstractions;

/// <summary>
/// 权限检查器
/// </summary>
public interface IPermissionChecker
{
    /// <summary>
    /// 检查授权主体是否具备指定权限
    /// </summary>
    /// <param name="context">权限检查上下文</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>权限检查结果</returns>
    Task<AuthorizationResult> CheckAsync(AuthorizationContext context, CancellationToken cancellationToken);
}
