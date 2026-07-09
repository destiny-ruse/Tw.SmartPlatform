using AwesomeAssertions;
using Xunit;

namespace Tw.AspNetCore.Tests;

/// <summary>验证 SmokeTests 相关行为</summary>
public class SmokeTests
{
    /// <summary>验证 TestProject_IsWired 场景</summary>
    [Fact]
    public void TestProject_IsWired()
    {
        true.Should().BeTrue();
    }
}
