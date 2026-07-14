using System.Security.Cryptography;
using AwesomeAssertions;
using Tw.Security.Cryptography;
using Xunit;

namespace Tw.Security.Tests.Cryptography;

/// <summary>
/// 固定 PBKDF2 自描述密码哈希格式的验证兼容性
/// </summary>
public sealed class PasswordHasherTests
{
    /// <summary>
    /// 验证新生成的 PBKDF2 哈希保留六段自描述格式并可用其中参数回验
    /// </summary>
    [Fact]
    public void HashPassword_WithExplicitParameters_ProducesVerifiableSelfDescribingFormat()
    {
        const string password = "correct horse battery staple";

        var hashedPassword = Pbkdf2PasswordHasher.HashPassword(
            password,
            iterations: 4096,
            keyLength: 24,
            saltLength: 12,
            hashAlgorithm: HashAlgorithmName.SHA384);
        var parts = hashedPassword.Split('$');

        parts.Should().HaveCount(6);
        parts[0].Should().Be("PBKDF2");
        parts[1].Should().Be("SHA384");
        parts[2].Should().Be("4096");
        parts[3].Should().Be("24");
        Convert.FromBase64String(parts[4]).Should().HaveCount(12);
        Convert.FromBase64String(parts[5]).Should().HaveCount(24);
        Pbkdf2PasswordHasher.VerifyPassword(password, hashedPassword).Should().BeTrue();
    }

    /// <summary>
    /// 验证既有 PBKDF2 自描述格式可验证固定夹具中的正确密码
    /// </summary>
    [Fact]
    public void VerifyPassword_CurrentSelfDescribingFixture_AcceptsMatchingPassword()
    {
        const string hashedPassword = "PBKDF2$SHA256$4096$32$ABEiM0RVZneImaq7zN3u/w==$IGXVmFSWRPJaEP8bqT+yFXi8U+qCqqpnTMr/Rvw0dMs=";

        var isMatch = Pbkdf2PasswordHasher.VerifyPassword("correct horse battery staple", hashedPassword);

        isMatch.Should().BeTrue();
    }

    /// <summary>
    /// 验证既有 PBKDF2 自描述格式拒绝固定夹具中的错误密码
    /// </summary>
    [Fact]
    public void VerifyPassword_CurrentSelfDescribingFixture_RejectsDifferentPassword()
    {
        const string hashedPassword = "PBKDF2$SHA256$4096$32$ABEiM0RVZneImaq7zN3u/w==$IGXVmFSWRPJaEP8bqT+yFXi8U+qCqqpnTMr/Rvw0dMs=";

        var isMatch = Pbkdf2PasswordHasher.VerifyPassword("incorrect password", hashedPassword);

        isMatch.Should().BeFalse();
    }
}
