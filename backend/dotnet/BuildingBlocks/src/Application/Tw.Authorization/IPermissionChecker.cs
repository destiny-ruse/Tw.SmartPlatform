namespace Tw.Authorization;

/// <summary>
/// 根据授权上下文返回允许或拒绝访问的稳定结果
/// </summary>
public interface IPermissionChecker
{
    /// <summary>
    /// 检查授权主体是否具备指定权限
    /// </summary>
    /// <param name="context">包含主体、租户、权限与资源范围的授权上下文</param>
    /// <param name="cancellationToken">用于终止权限检查的取消令牌</param>
    /// <returns>包含允许状态、稳定结果码与安全消息的权限检查结果</returns>
    /// <exception cref="OperationCanceledException">检查因调用方取消而终止时抛出</exception>
    Task<AuthorizationResult> CheckAsync(AuthorizationContext context, CancellationToken cancellationToken);
}
