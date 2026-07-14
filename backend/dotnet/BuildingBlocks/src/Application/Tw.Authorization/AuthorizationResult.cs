namespace Tw.Authorization;

/// <summary>
/// 提供权限检查的允许状态、稳定结果码与安全消息
/// </summary>
/// <param name="Allowed">允许访问时为 true；拒绝访问时为 false</param>
/// <param name="Code">供调用方稳定映射的结果码</param>
/// <param name="Message">可安全传递到协议边界的结果消息</param>
public sealed record AuthorizationResult(bool Allowed, string Code, string Message)
{
    /// <summary>
    /// 创建使用系统成功码的允许访问结果
    /// </summary>
    /// <returns>允许访问且包含稳定系统成功信息的结果</returns>
    public static AuthorizationResult Success() => new(true, "SYSTEM:000000", "success");

    /// <summary>
    /// 创建保留指定错误信息的拒绝访问结果
    /// </summary>
    /// <param name="code">供调用方稳定映射的错误码</param>
    /// <param name="message">可安全传递到协议边界的拒绝消息</param>
    /// <returns>拒绝访问且包含指定错误信息的结果</returns>
    public static AuthorizationResult Denied(string code, string message) => new(false, code, message);
}
