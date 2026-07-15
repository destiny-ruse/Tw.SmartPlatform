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
    /// 结构化验证错误必须只能在构造阶段原子确定
    /// </summary>
    [Fact]
    public void ValidationErrors_PublicSetter_DoesNotExist()
    {
        var property = typeof(ErrorDescriptor).GetProperty(nameof(ErrorDescriptor.ValidationErrors));

        property.Should().NotBeNull();
        property!.SetMethod.Should().BeNull();
    }

    /// <summary>
    /// 验证错误集合必须复制为独立只读快照
    /// </summary>
    [Fact]
    public void Constructor_MutableValidationErrors_CapturesIndependentReadOnlySnapshot()
    {
        var validationError = CreateValidationError();
        var source = new List<ValidationError> { validationError };
        var descriptor = new ErrorDescriptor(
            "VALIDATION:000001",
            "输入验证失败",
            ErrorCategory.Validation,
            source);

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
    public void Constructor_NullValidationErrors_ThrowsArgumentNullException()
    {
        var act = () => new ErrorDescriptor(
            "VALIDATION:000001",
            "输入验证失败",
            ErrorCategory.Validation,
            null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("validationErrors");
    }

    /// <summary>
    /// 验证错误集合包含空元素时拒绝破坏公开边界
    /// </summary>
    [Fact]
    public void Constructor_NullValidationErrorElement_ThrowsArgumentException()
    {
        ValidationError[] errors = [CreateValidationError(), null!];

        var act = () => new ErrorDescriptor(
            "VALIDATION:000001",
            "输入验证失败",
            ErrorCategory.Validation,
            errors);

        act.Should().Throw<ArgumentException>()
            .WithParameterName("validationErrors");
    }

    /// <summary>
    /// 非验证类别不得在对象初始化期间注入验证错误
    /// </summary>
    [Fact]
    public void Constructor_NonValidationCategoryWithErrors_ThrowsInvalidOperationException()
    {
        var act = () => new ErrorDescriptor(
            "SYSTEM:999999",
            "系统异常",
            ErrorCategory.System,
            [CreateValidationError()]);

        act.Should().Throw<InvalidOperationException>();
    }

    /// <summary>
    /// 空字段错误集合允许表达对象级验证失败
    /// </summary>
    [Fact]
    public void Constructor_ValidationCategoryWithEmptyErrors_AllowsObjectLevelValidation()
    {
        var descriptor = new ErrorDescriptor(
            "VALIDATION:000001",
            "输入验证失败",
            ErrorCategory.Validation,
            []);

        descriptor.Category.Should().Be(ErrorCategory.Validation);
        descriptor.ValidationErrors.Should().BeEmpty();
    }

    /// <summary>
    /// 跨类别重建错误描述时通过构造器原子确定结构化错误
    /// </summary>
    [Fact]
    public void Constructor_ReclassifiedValuesWithErrors_CreatesValidationDescriptor()
    {
        var source = new ErrorDescriptor("VALIDATION:000001", "输入验证失败", ErrorCategory.System);
        var validationError = CreateValidationError();

        var descriptor = new ErrorDescriptor(
            source.Code,
            source.Message,
            ErrorCategory.Validation,
            [validationError]);

        descriptor.Category.Should().Be(ErrorCategory.Validation);
        descriptor.ValidationErrors.Should().ContainSingle().Which.Should().Be(validationError);
    }

    /// <summary>
    /// with表达式不得把携带验证错误的描述改为非验证类别
    /// </summary>
    [Fact]
    public void With_ValidationDescriptorChangingCategory_ThrowsInvalidOperationException()
    {
        var descriptor = new ErrorDescriptor(
            "VALIDATION:000001",
            "输入验证失败",
            ErrorCategory.Validation,
            [CreateValidationError()]);

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
