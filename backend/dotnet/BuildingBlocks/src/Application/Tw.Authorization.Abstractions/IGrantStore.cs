namespace Tw.Authorization.Abstractions;

/// <summary>
/// 权限 grant 存储读取边界
/// </summary>
public interface IGrantStore
{
    /// <summary>
    /// 判断当前上下文是否存在允许访问的 grant
    /// </summary>
    /// <param name="context">权限检查上下文</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>存在允许访问 grant 时返回 true</returns>
    Task<bool> HasGrantAsync(AuthorizationContext context, CancellationToken cancellationToken);
}
