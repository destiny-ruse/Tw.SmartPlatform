namespace Tw.ExceptionHandling.Validation;

/// <summary>
/// 表示携带结构化字段错误的输入验证失败
/// </summary>
public sealed class ValidationException : Exception
{
    /// <summary>
    /// 使用错误集合的只读快照创建输入验证异常
    /// </summary>
    /// <param name="errors">需要保留字段路径、错误码和消息的验证错误集合</param>
    /// <exception cref="ArgumentNullException"><paramref name="errors"/> 为 <see langword="null"/> 时抛出</exception>
    public ValidationException(IEnumerable<ValidationError> errors)
        : base("输入验证失败")
    {
        ArgumentNullException.ThrowIfNull(errors);
        Errors = Array.AsReadOnly(errors.ToArray());
    }

    /// <summary>
    /// 构造异常时捕获的结构化验证错误只读快照
    /// </summary>
    public IReadOnlyList<ValidationError> Errors { get; }
}
