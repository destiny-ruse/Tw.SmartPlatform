namespace Tw.Authorization;

/// <summary>
/// 从持久化授权来源判断指定上下文是否存在允许访问记录
/// </summary>
public interface IGrantStore
{
    /// <summary>
    /// 判断指定授权上下文是否存在允许访问的 grant
    /// </summary>
    /// <param name="context">包含主体、租户、权限与资源范围的授权上下文</param>
    /// <param name="cancellationToken">用于终止授权记录读取的取消令牌</param>
    /// <returns>存在允许访问 grant 时为 true；不存在时为 false</returns>
    /// <exception cref="OperationCanceledException">读取因调用方取消而终止时抛出</exception>
    Task<bool> HasGrantAsync(AuthorizationContext context, CancellationToken cancellationToken);
}
