using AwesomeAssertions;
using Tw.Security.Cryptography;
using Xunit;

namespace Tw.Security.Tests.Cryptography;

/// <summary>
/// 固定哈希算法的公开兼容性向量
/// </summary>
public sealed class HashCompatibilityTests
{
    /// <summary>
    /// 验证 SHA-256 对标准 abc 输入保持既有十六进制结果
    /// </summary>
    [Fact]
    public void ComputeHash_Sha256_ProducesKnownVector()
    {
        var hash = Sha256Hasher.ComputeHash("abc");

        hash.Should().Be("ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad");
    }

    /// <summary>
    /// 验证 SHA3-256 对标准 abc 输入保持既有十六进制结果
    /// </summary>
    [Fact]
    public void ComputeHash_Sha3256_ProducesKnownVector()
    {
        var hash = Sha3256Hasher.ComputeHash("abc");

        hash.Should().Be("3a985da74fe225b2045c172d6bd390bd855f086e3e9d525b46bfe24511431532");
    }

    /// <summary>
    /// 验证 HMAC-SHA-256 对标准密钥和消息保持既有十六进制结果
    /// </summary>
    [Fact]
    public void ComputeHash_HmacSha256_ProducesKnownVector()
    {
        var hash = HmacSha256Hasher.ComputeHash("key", "The quick brown fox jumps over the lazy dog");

        hash.Should().Be("f7bc83f430538424b13298e6aa6fb143ef4d59a14946175997479dbc2d1a3cd8");
    }
}
