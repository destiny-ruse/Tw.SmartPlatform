using Tw.ExceptionHandling.Validation;

namespace Tw.ExceptionHandling;

/// <summary>
/// 将已知验证异常和未知异常转换为稳定、安全的错误描述
/// </summary>
public sealed class DefaultExceptionToErrorMapper : IExceptionToErrorMapper
{
    /// <inheritdoc />
    public ErrorDescriptor Map(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        if (exception is ValidationException validationException)
        {
            return new ErrorDescriptor("VALIDATION:000001", validationException.Message, ErrorCategory.Validation)
            {
                ValidationErrors = validationException.Errors
            };
        }

        return new ErrorDescriptor("SYSTEM:999999", "系统异常", ErrorCategory.System);
    }
}
