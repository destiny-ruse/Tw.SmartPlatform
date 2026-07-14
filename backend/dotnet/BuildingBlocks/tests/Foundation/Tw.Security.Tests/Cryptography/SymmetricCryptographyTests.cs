using System.Security.Cryptography;
using System.Text;
using AwesomeAssertions;
using Tw.Security.Cryptography;
using Xunit;

namespace Tw.Security.Tests.Cryptography;

/// <summary>
/// 固定 AES 对称加密的载荷布局和失败语义
/// </summary>
public sealed class SymmetricCryptographyTests
{
    /// <summary>
    /// 固定 AES-128 密钥，确保测试只覆盖算法兼容性
    /// </summary>
    private static readonly byte[] Key = Encoding.UTF8.GetBytes("0123456789abcdef");

    /// <summary>
    /// 固定 CBC 初始化向量，确保密文可作为回归夹具
    /// </summary>
    private static readonly byte[] InitializationVector = Encoding.UTF8.GetBytes("fedcba9876543210");

    /// <summary>
    /// 固定 UTF-8 明文，覆盖非 ASCII 内容的字节布局
    /// </summary>
    private static readonly byte[] Plaintext = Encoding.UTF8.GetBytes("迁移前后密文布局固定");

    /// <summary>
    /// 固定迁移前由 IV 前缀和 AES-CBC 密文组成的公开兼容性夹具
    /// </summary>
    private const string PrefixedCiphertextFixture =
        "ZmVkY2JhOTg3NjU0MzIxMOnOLU3qVsImHQmCxz7/RL9FL1Wzhc5Jk2dFMaqQWpsV";

    /// <summary>
    /// 验证 AES 加密和解密保持固定密文布局及原始明文
    /// </summary>
    [Fact]
    public void EncryptAndDecrypt_AesCbcWithExplicitIv_PreservesCiphertextLayoutAndPlaintext()
    {
        var ciphertext = AesCryptography.Encrypt(Plaintext, Key, InitializationVector);
        var decrypted = AesCryptography.Decrypt(ciphertext, Key, InitializationVector);

        Convert.ToBase64String(ciphertext).Should().Be("6c4tTepWwiYdCYLHPv9Ev0UvVbOFzkmTZ0UxqpBamxU=");
        decrypted.Should().BeEquivalentTo(Plaintext);
    }

    /// <summary>
    /// 验证 AES-CBC 未显式传入 IV 时使用生成的 IV 作为密文前缀
    /// </summary>
    [Fact]
    public void Encrypt_AesCbcWithoutExplicitIv_PrefixesGeneratedIv()
    {
        var payload = AesCryptography.Encrypt(Plaintext, Key);
        var prefixedIv = payload[..InitializationVector.Length];
        var ciphertext = payload[InitializationVector.Length..];

        ciphertext.Should().NotBeEmpty();
        (ciphertext.Length % InitializationVector.Length).Should()
            .Be(0, because: "AES-CBC 密文必须按 16 字节块对齐");
        AesCryptography.Decrypt(ciphertext, Key, prefixedIv).Should().BeEquivalentTo(Plaintext);
        AesCryptography.Decrypt(payload, Key).Should().BeEquivalentTo(Plaintext);
    }

    /// <summary>
    /// 验证 AES-CBC 可解密迁移前带 IV 前缀的固定密文夹具
    /// </summary>
    [Fact]
    public void Decrypt_AesCbcWithoutExplicitIv_DecryptsPrefixedMigrationFixture()
    {
        var payload = Convert.FromBase64String(PrefixedCiphertextFixture);

        var decrypted = AesCryptography.Decrypt(payload, Key);

        decrypted.Should().BeEquivalentTo(Plaintext);
    }

    /// <summary>
    /// 验证 AES-CBC 拒绝短于 IV 长度的载荷
    /// </summary>
    [Fact]
    public void Decrypt_AesCbcWithPayloadShorterThanIv_ThrowsArgumentException()
    {
        var act = () => AesCryptography.Decrypt(new byte[InitializationVector.Length - 1], Key);

        act.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("bytes");
    }

    /// <summary>
    /// 验证 AES 拒绝长度不符合算法要求的密钥
    /// </summary>
    [Fact]
    public void Encrypt_AesWithInvalidKeyLength_ThrowsArgumentException()
    {
        var act = () => AesCryptography.Encrypt(Plaintext, new byte[15], InitializationVector);

        act.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("key");
    }

    /// <summary>
    /// 验证 AES 不会以不同的有效密钥成功解密固定密文
    /// </summary>
    [Fact]
    public void Decrypt_AesWithDifferentValidKey_ThrowsCryptographicException()
    {
        var ciphertext = AesCryptography.Encrypt(Plaintext, Key, InitializationVector);
        var differentKey = Encoding.UTF8.GetBytes("abcdef0123456789");
        var act = () => AesCryptography.Decrypt(ciphertext, differentKey, InitializationVector);

        act.Should().Throw<CryptographicException>();
    }
}
