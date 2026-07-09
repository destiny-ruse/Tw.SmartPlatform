using AwesomeAssertions;
using Xunit;

namespace Tw.AspNetCore.Tests;

public class SmokeTests
{
    [Fact]
    public void TestProject_IsWired()
    {
        true.Should().BeTrue();
    }
}
