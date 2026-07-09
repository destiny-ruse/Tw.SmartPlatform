using AwesomeAssertions;
using Xunit;

namespace Tw.Core.Tests;

public class SmokeTests
{
    [Fact]
    public void TestProject_IsWired()
    {
        true.Should().BeTrue();
    }
}
