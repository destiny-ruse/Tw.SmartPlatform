using AwesomeAssertions;
using Tw.Security.Cryptography;
using Xunit;

namespace Tw.Security.Tests.Cryptography;

/// <summary>
/// 覆盖密码学安全随机值生成器的范围和输入契约
/// </summary>
public sealed class SecureRandomGeneratorTests
{
    /// <summary>
    /// 验证生成的随机整数始终位于请求的半开区间内
    /// </summary>
    [Fact]
    public void GetInt_ReturnsValuesWithinRequestedRange()
    {
        var values = Enumerable.Range(0, 64)
            .Select(_ => SecureRandomGenerator.GetInt(-10, 10));

        values.Should().OnlyContain(value => value >= -10 && value < 10);
    }

    /// <summary>
    /// 验证生成的随机字节数组具有请求长度
    /// </summary>
    [Fact]
    public void GetBytes_ReturnsRequestedLength()
    {
        var bytes = SecureRandomGenerator.GetBytes(32);

        bytes.Should().HaveCount(32);
    }

    /// <summary>
    /// 验证生成随机字节时拒绝负长度
    /// </summary>
    [Fact]
    public void GetBytes_WithNegativeLength_ThrowsArgumentOutOfRangeException()
    {
        var act = () => SecureRandomGenerator.GetBytes(-1);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    /// <summary>
    /// 验证强密码包含请求的长度和每个必需字符类别
    /// </summary>
    [Fact]
    public void GetStrongPassword_WithSpecialCharacters_ContainsRequiredCharacterCategories()
    {
        var password = SecureRandomGenerator.GetStrongPassword(length: 16, includeSpecialChars: true);

        password.Should().HaveLength(16);
        password.Any(char.IsLower).Should().BeTrue();
        password.Any(char.IsUpper).Should().BeTrue();
        password.Any(char.IsDigit).Should().BeTrue();
        password.Any(character => "!@#$%^&*()_+-=[]{}|;:,.<>?".Contains(character)).Should().BeTrue();
    }
}
