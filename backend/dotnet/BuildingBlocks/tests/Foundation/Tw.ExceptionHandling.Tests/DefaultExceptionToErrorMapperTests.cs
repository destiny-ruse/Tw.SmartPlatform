using AwesomeAssertions;
using Tw.ExceptionHandling;
using Tw.ExceptionHandling.Validation;
using Xunit;

namespace Tw.ExceptionHandling.Tests;

/// <summary>
/// 覆盖默认ExceptionToErrorMapper的核心行为和边界条件
/// </summary>
public sealed class DefaultExceptionToErrorMapperTests
{
    /// <summary>
    /// 验证映射Unknown异常返回System错误
    /// </summary>
    [Fact]
    public void Map_UnknownException_ReturnsSystemError()
    {
        var mapper = new DefaultExceptionToErrorMapper();

        var error = mapper.Map(new InvalidOperationException("boom"));

        error.Code.Should().Be("SYSTEM:999999");
        error.Message.Should().Be("系统异常");
        error.Category.Should().Be(ErrorCategory.System);
        error.ValidationErrors.Should().BeEmpty();
    }

    /// <summary>
    /// 验证异常映射为稳定输入错误并保留字段路径、错误码和消息
    /// </summary>
    [Fact]
    public void Map_ValidationException_PreservesStructuredErrors()
    {
        ValidationError[] validationErrors =
        [
            new("order.lines[0].quantity", "VALIDATION:RANGE", "数量必须大于零"),
            new("order.customerId", "VALIDATION:REQUIRED", "客户标识不能为空")
        ];
        var mapper = new DefaultExceptionToErrorMapper();

        var exception = new ValidationException(validationErrors);

        var error = mapper.Map(exception);

        error.Code.Should().Be("VALIDATION:000001");
        error.Message.Should().Be("输入验证失败");
        error.Category.Should().Be(ErrorCategory.Validation);
        error.ValidationErrors.Should().Equal(validationErrors);
        error.ValidationErrors.Should().NotBeSameAs(exception.Errors);
    }

    /// <summary>
    /// 缺少异常对象时拒绝生成失去诊断来源的错误描述
    /// </summary>
    [Fact]
    public void Map_NullException_ThrowsArgumentNullException()
    {
        var mapper = new DefaultExceptionToErrorMapper();

        var act = () => mapper.Map(null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("exception");
    }
}
