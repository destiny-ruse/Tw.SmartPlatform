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
