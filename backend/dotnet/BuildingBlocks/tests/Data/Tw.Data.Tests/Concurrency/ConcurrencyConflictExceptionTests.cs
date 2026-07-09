using AwesomeAssertions;
using Tw.Data.Concurrency;
using Xunit;

namespace Tw.Data.Tests.Concurrency;

public sealed class ConcurrencyConflictExceptionTests
{
    [Fact]
    public void Constructor_UsesStableErrorCode()
    {
        var exception = new ConcurrencyConflictException("Order", "order-1");

        exception.Code.Should().Be("DATA:CONFLICT");
        exception.Message.Should().Be("Data has been changed by another request.");
        exception.ResourceType.Should().Be("Order");
        exception.ResourceId.Should().Be("order-1");
    }
}
