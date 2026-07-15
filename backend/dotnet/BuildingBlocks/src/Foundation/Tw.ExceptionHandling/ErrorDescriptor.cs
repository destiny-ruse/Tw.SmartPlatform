using Tw.ExceptionHandling.Validation;

namespace Tw.ExceptionHandling;

/// <summary>
/// 错误类别用于在协议边界统一表达失败类型
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
public sealed record ErrorDescriptor
{
    /// <summary>
    /// 保存错误类别
    /// </summary>
    private ErrorCategory _category;

    /// <summary>
    /// 保存字段级验证错误的独立只读快照
    /// </summary>
    private IReadOnlyList<ValidationError> _validationErrors = Array.Empty<ValidationError>();

    /// <summary>
    /// 创建对外稳定错误描述
    /// </summary>
    /// <param name="Code">供协议调用方稳定判断失败原因的错误码</param>
    /// <param name="Message">可安全返回给调用方的错误消息</param>
    /// <param name="Category">决定协议边界失败分类的错误类别</param>
    public ErrorDescriptor(string Code, string Message, ErrorCategory Category)
    {
        this.Code = Code;
        this.Message = Message;
        _category = Category;
    }

    /// <summary>
    /// 获取供协议调用方稳定判断失败原因的错误码
    /// </summary>
    public string Code { get; init; }

    /// <summary>
    /// 获取可安全返回给调用方的错误消息
    /// </summary>
    public string Message { get; init; }

    /// <summary>
    /// 获取决定协议边界失败分类的错误类别
    /// </summary>
    /// <exception cref="InvalidOperationException">尝试把携带验证错误的描述改为非验证类别时抛出</exception>
    public ErrorCategory Category
    {
        get => _category;
        init
        {
            if (value != ErrorCategory.Validation && _validationErrors.Count > 0)
            {
                throw new InvalidOperationException("非验证类别不得携带字段级验证错误");
            }

            _category = value;
        }
    }

    /// <summary>
    /// 获取输入验证失败时保留的字段级结构化错误
    /// </summary>
    /// <exception cref="ArgumentNullException">赋值为空引用时抛出</exception>
    /// <exception cref="ArgumentException">集合包含空元素时抛出</exception>
    /// <exception cref="InvalidOperationException">向非验证类别赋予非空验证错误时抛出</exception>
    public IReadOnlyList<ValidationError> ValidationErrors
    {
        get => _validationErrors;
        init
        {
            ArgumentNullException.ThrowIfNull(value, nameof(ValidationErrors));
            var snapshot = value.ToArray();

            if (snapshot.Any(static error => error is null))
            {
                throw new ArgumentException("验证错误集合不得包含空元素", nameof(ValidationErrors));
            }

            if (Category != ErrorCategory.Validation && snapshot.Length > 0)
            {
                throw new InvalidOperationException("非验证类别不得携带字段级验证错误");
            }

            _validationErrors = Array.AsReadOnly(snapshot);
        }
    }

    /// <summary>
    /// 将错误描述分解为原有位置记录公开的三个组成部分
    /// </summary>
    /// <param name="Code">错误码</param>
    /// <param name="Message">错误消息</param>
    /// <param name="Category">错误类别</param>
    public void Deconstruct(out string Code, out string Message, out ErrorCategory Category)
    {
        Code = this.Code;
        Message = this.Message;
        Category = this.Category;
    }
}
