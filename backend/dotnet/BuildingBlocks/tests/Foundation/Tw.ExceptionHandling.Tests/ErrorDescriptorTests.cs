using AwesomeAssertions;
using Tw.ExceptionHandling.Validation;
using Xunit;

namespace Tw.ExceptionHandling.Tests;

/// <summary>
/// 验证错误描述对结构化验证错误公开边界的不变量
/// </summary>
public sealed class ErrorDescriptorTests
{
    /// <summary>
    /// 验证错误集合必须复制为独立只读快照
    /// </summary>
    [Fact]
    public void ValidationErrors_MutableSource_CapturesIndependentReadOnlySnapshot()
    {
        var validationError = CreateValidationError();
        var source = new List<ValidationError> { validationError };
        var descriptor = new ErrorDescriptor("VALIDATION:000001", "输入验证失败", ErrorCategory.Validation)
        {
            ValidationErrors = source
        };

        source.Clear();

        descriptor.ValidationErrors.Should().ContainSingle().Which.Should().Be(validationError);
        descriptor.ValidationErrors.Should().NotBeSameAs(source);
        var capturedErrors = descriptor.ValidationErrors
            .Should().BeAssignableTo<ICollection<ValidationError>>().Subject;
        capturedErrors.IsReadOnly.Should().BeTrue();
        var addError = () => capturedErrors.Add(CreateValidationError());
        addError.Should().Throw<NotSupportedException>();
    }

    /// <summary>
    /// 验证错误集合为空引用时拒绝破坏公开边界
    /// </summary>
    [Fact]
    public void ValidationErrors_Null_ThrowsArgumentNullException()
    {
        var act = () => new ErrorDescriptor("VALIDATION:000001", "输入验证失败", ErrorCategory.Validation)
        {
            ValidationErrors = null!
        };

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("ValidationErrors");
    }

    /// <summary>
    /// 验证错误集合包含空元素时拒绝破坏公开边界
    /// </summary>
    [Fact]
    public void ValidationErrors_NullElement_ThrowsArgumentException()
    {
        ValidationError[] errors = [CreateValidationError(), null!];

        var act = () => new ErrorDescriptor("VALIDATION:000001", "输入验证失败", ErrorCategory.Validation)
        {
            ValidationErrors = errors
        };

        act.Should().Throw<ArgumentException>()
            .WithParameterName("ValidationErrors");
    }

    /// <summary>
    /// 非验证类别不得在对象初始化期间注入验证错误
    /// </summary>
    [Fact]
    public void ValidationErrors_NonValidationCategory_ThrowsInvalidOperationException()
    {
        var act = () => new ErrorDescriptor("SYSTEM:999999", "系统异常", ErrorCategory.System)
        {
            ValidationErrors = [CreateValidationError()]
        };

        act.Should().Throw<InvalidOperationException>();
    }

    /// <summary>
    /// with表达式不得向非验证类别注入验证错误
    /// </summary>
    [Fact]
    public void With_NonValidationDescriptorAddingValidationErrors_ThrowsInvalidOperationException()
    {
        var descriptor = new ErrorDescriptor("SYSTEM:999999", "系统异常", ErrorCategory.System);

        var act = () => descriptor with { ValidationErrors = [CreateValidationError()] };

        act.Should().Throw<InvalidOperationException>();
    }

    /// <summary>
    /// with表达式不得把携带验证错误的描述改为非验证类别
    /// </summary>
    [Fact]
    public void With_ValidationDescriptorChangingCategory_ThrowsInvalidOperationException()
    {
        var descriptor = new ErrorDescriptor("VALIDATION:000001", "输入验证失败", ErrorCategory.Validation)
        {
            ValidationErrors = [CreateValidationError()]
        };

        var act = () => descriptor with { Category = ErrorCategory.System };

        act.Should().Throw<InvalidOperationException>();
    }

    /// <summary>
    /// 创建稳定的字段级验证错误
    /// </summary>
    /// <returns>测试使用的验证错误</returns>
    private static ValidationError CreateValidationError()
    {
        return new ValidationError("order.customerId", "VALIDATION:REQUIRED", "客户标识不能为空");
    }
}
