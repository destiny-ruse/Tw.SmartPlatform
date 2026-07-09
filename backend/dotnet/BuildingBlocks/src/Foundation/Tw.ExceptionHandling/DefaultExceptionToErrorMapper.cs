namespace Tw.ExceptionHandling;

/// <summary>
/// 默认异常映射器，将未知异常转换为通用系统错误
/// </summary>
public sealed class DefaultExceptionToErrorMapper : IExceptionToErrorMapper
{
    /// <inheritdoc />
    public ErrorDescriptor Map(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return new ErrorDescriptor("SYSTEM:999999", "系统异常", ErrorCategory.System);
    }
}
