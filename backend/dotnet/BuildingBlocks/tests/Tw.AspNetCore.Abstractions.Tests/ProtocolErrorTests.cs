using AwesomeAssertions;
using Tw.AspNetCore.Abstractions;
using Xunit;

namespace Tw.AspNetCore.Abstractions.Tests;

public sealed class ProtocolErrorTests
{
    [Fact]
    public void Conflict_UsesHttp409()
    {
        var error = ProtocolError.Conflict("DATA:CONFLICT", "Data has been changed by another request.");

        error.StatusCode.Should().Be(409);
        error.Code.Should().Be("DATA:CONFLICT");
    }
}
