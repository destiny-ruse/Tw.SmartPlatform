namespace Tw.Authorization.Abstractions;

/// <summary>
/// 权限检查结果
/// </summary>
/// <param name="Allowed">是否允许访问</param>
/// <param name="Code">稳定结果码</param>
/// <param name="Message">安全结果消息</param>
public sealed record AuthorizationResult(bool Allowed, string Code, string Message)
{
    /// <summary>
    /// 创建允许访问结果
    /// </summary>
    /// <returns>允许访问结果</returns>
    public static AuthorizationResult Success() => new(true, "SYSTEM:000000", "success");

    /// <summary>
    /// 创建拒绝访问结果
    /// </summary>
    /// <param name="code">稳定错误码</param>
    /// <param name="message">安全错误消息</param>
    /// <returns>拒绝访问结果</returns>
    public static AuthorizationResult Denied(string code, string message) => new(false, code, message);
}
