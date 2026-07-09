namespace Tw.Validation.Abstractions;

/// <summary>
/// 输入验证失败时抛出的异常，携带结构化验证错误集合
/// </summary>
public sealed class ValidationException : Exception
{
    /// <summary>
    /// 初始化 <see cref="ValidationException"/> 类的新实例
    /// </summary>
    /// <param name="errors">验证错误集合</param>
    /// <exception cref="ArgumentNullException">当 <paramref name="errors"/> 为 <see langword="null"/> 时抛出</exception>
    public ValidationException(IEnumerable<ValidationError> errors)
        : base("输入验证失败")
    {
        ArgumentNullException.ThrowIfNull(errors);
        Errors = errors.ToArray();
    }

    /// <summary>
    /// 结构化验证错误集合
    /// </summary>
    public IReadOnlyList<ValidationError> Errors { get; }
}
