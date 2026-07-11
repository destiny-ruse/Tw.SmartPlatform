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
