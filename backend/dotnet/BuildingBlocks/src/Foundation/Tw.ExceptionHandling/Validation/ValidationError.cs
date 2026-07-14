namespace Tw.ExceptionHandling.Validation;

/// <summary>
/// 描述单个输入字段的稳定验证错误
/// </summary>
/// <param name="FieldPath">从请求根对象定位失败字段的路径</param>
/// <param name="Code">供调用方稳定判断失败原因的错误码</param>
/// <param name="Message">可安全返回给调用方的验证失败消息</param>
public sealed record ValidationError(string FieldPath, string Code, string Message);
