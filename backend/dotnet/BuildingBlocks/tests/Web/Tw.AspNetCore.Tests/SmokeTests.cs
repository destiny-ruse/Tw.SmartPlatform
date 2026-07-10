using AwesomeAssertions;
using Xunit;

namespace Tw.AspNetCore.Tests;

/// <summary>
/// 覆盖冒烟的核心行为和边界条件
/// </summary>
public class SmokeTests
{
    /// <summary>
    /// 验证Test项目IsWired
    /// </summary>
    [Fact]
    public void TestProject_IsWired()
    {
        true.Should().BeTrue();
    }
}
