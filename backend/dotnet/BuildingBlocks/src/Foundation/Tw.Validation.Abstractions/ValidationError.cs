namespace Tw.Validation.Abstractions;

/// <summary>
/// 输入验证错误
/// </summary>
/// <param name="FieldPath">字段路径</param>
/// <param name="Code">稳定错误码</param>
/// <param name="Message">验证失败消息</param>
public sealed record ValidationError(string FieldPath, string Code, string Message);
