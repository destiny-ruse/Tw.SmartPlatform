using AwesomeAssertions;
using Tw.ExceptionHandling;
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
    }
}
