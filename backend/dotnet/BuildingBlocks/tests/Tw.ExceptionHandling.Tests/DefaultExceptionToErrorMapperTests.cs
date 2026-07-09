using AwesomeAssertions;
using Tw.ExceptionHandling;
using Xunit;

namespace Tw.ExceptionHandling.Tests;

public sealed class DefaultExceptionToErrorMapperTests
{
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
