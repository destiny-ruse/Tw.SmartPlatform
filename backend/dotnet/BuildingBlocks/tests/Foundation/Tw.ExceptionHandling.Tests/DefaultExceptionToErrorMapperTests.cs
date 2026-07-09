using AwesomeAssertions;
using Tw.ExceptionHandling;
using Xunit;

namespace Tw.ExceptionHandling.Tests;

/// <summary>验证 DefaultExceptionToErrorMapperTests 相关行为</summary>
public sealed class DefaultExceptionToErrorMapperTests
{
    /// <summary>验证 Map_UnknownException_ReturnsSystemError 场景</summary>
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
