namespace Tw.ExceptionHandling;

/// <summary>
/// 错误类别，用于在协议边界统一表达失败类型
/// </summary>
public enum ErrorCategory
{
    /// <summary>
    /// 输入验证错误
    /// </summary>
    Validation,

    /// <summary>
    /// 身份认证错误
    /// </summary>
    Authentication,

    /// <summary>
    /// 授权错误
    /// </summary>
    Authorization,

    /// <summary>
    /// 业务规则错误
    /// </summary>
    Business,

    /// <summary>
    /// 资源不存在错误
    /// </summary>
    NotFound,

    /// <summary>
    /// 并发或状态冲突错误
    /// </summary>
    Conflict,

    /// <summary>
    /// 下游依赖错误
    /// </summary>
    Dependency,

    /// <summary>
    /// 系统未知错误
    /// </summary>
    System
}

/// <summary>
/// 对外稳定错误描述
/// </summary>
/// <param name="Code">稳定错误码</param>
/// <param name="Message">可安全返回给调用方的错误消息</param>
/// <param name="Category">错误类别</param>
public sealed record ErrorDescriptor(string Code, string Message, ErrorCategory Category);
