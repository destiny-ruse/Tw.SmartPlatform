using AwesomeAssertions;
using Tw.ExceptionHandling.Validation;
using Xunit;

namespace Tw.ExceptionHandling.Tests.Validation;

/// <summary>
/// 验证结构化输入错误异常的集合快照与参数边界
/// </summary>
public sealed class ValidationExceptionTests
{
    /// <summary>
    /// 构造异常时复制错误集合并阻止调用方修改已捕获内容
    /// </summary>
    [Fact]
    public void Constructor_CapturesImmutableErrorSnapshot()
    {
        var originalError = new ValidationError("customer.address.street", "VALIDATION:REQUIRED", "街道不能为空");
        var source = new List<ValidationError> { originalError };

        var exception = new ValidationException(source);
        source.Clear();

        exception.Errors.Should().ContainSingle().Which.Should().Be(originalError);
        var capturedErrors = exception.Errors.Should().BeAssignableTo<ICollection<ValidationError>>().Subject;
        capturedErrors.IsReadOnly.Should().BeTrue();

        var addCapturedError = () => capturedErrors.Add(
            new ValidationError("customer.name", "VALIDATION:REQUIRED", "客户名称不能为空"));

        addCapturedError.Should().Throw<NotSupportedException>();
    }

    /// <summary>
    /// 空字段错误集合允许表达对象级输入验证失败
    /// </summary>
    [Fact]
    public void Constructor_EmptyErrors_AllowsObjectLevelValidation()
    {
        var exception = new ValidationException([]);

        exception.Errors.Should().BeEmpty();
    }

    /// <summary>
    /// 缺少错误集合时拒绝创建没有诊断上下文的验证异常
    /// </summary>
    [Fact]
    public void Constructor_NullErrors_ThrowsArgumentNullException()
    {
        var act = () => new ValidationException(null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("errors");
    }

    /// <summary>
    /// 错误集合包含空元素时拒绝创建存在不完整诊断上下文的验证异常
    /// </summary>
    [Fact]
    public void Constructor_NullErrorElement_ThrowsArgumentException()
    {
        ValidationError[] errors =
        [
            new("customer.name", "VALIDATION:REQUIRED", "客户名称不能为空"),
            null!
        ];

        var act = () => new ValidationException(errors);

        act.Should().Throw<ArgumentException>()
            .WithParameterName("errors");
    }
}
