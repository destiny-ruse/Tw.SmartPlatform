using System.Security.Cryptography;
using System.Text;
using AwesomeAssertions;
using Tw.Security.Cryptography;
using Xunit;

namespace Tw.Security.Tests.Cryptography;

/// <summary>
/// 固定 RSA PKCS#1 密钥导入、加解密和签名失败语义
/// </summary>
public sealed class RsaCryptographyTests
{
    /// <summary>
    /// 公开、合成且非秘密的 1024-bit PKCS#1 DER 公钥夹具，仅用于迁移兼容性测试
    /// </summary>
    private static readonly byte[] PublicKeyDer = Convert.FromBase64String(
        "MIGJAoGBAOhlHj5wXrKVTcAd7hVGWiFL8YXDeJsk4znkvEQ/HZA7cVoOUmdqClVEJ2HHwIQyRYmUPIA5ZRu57WMHMni01einmN5QijggPOruMT+FC7pIAwRIyJGHwDwX+s+cWnH9Opw2Y6mPk1onJeGSG+CSn5/fM2s9lmtLVLH93JVkBQ4HAgMBAAE=");

    /// <summary>
    /// 公开、合成且非秘密的 1024-bit PKCS#1 DER 私钥夹具，仅用于迁移兼容性测试
    /// </summary>
    private static readonly byte[] PrivateKeyDer = Convert.FromBase64String(
        "MIICXAIBAAKBgQDoZR4+cF6ylU3AHe4VRlohS/GFw3ibJOM55LxEPx2QO3FaDlJnagpVRCdhx8CEMkWJlDyAOWUbue1jBzJ4tNXop5jeUIo4IDzq7jE/hQu6SAMESMiRh8A8F/rPnFpx/TqcNmOpj5NaJyXhkhvgkp+f3zNrPZZrS1Sx/dyVZAUOBwIDAQABAoGBALVss5a1LQinzIIOG58aRCS4X/44YsBjpMy+iEeTKmY+MbjHc4duXlDAmyoXwnCxul20jyLfK9LgbLWhmcJoEpFIlsTA3A43KmVbjHWWOl2UX0sI/uDnC5of8fwFp0qma/5HPhywloqNzkzw3fzcl8ae4UhsyqXG8QTGK6PJKakRAkEA/gwu7IWX/trqOvpQMpFcDtvErAtIWi6uCVwIDi6oDWv5XRagLuu8xlOZZPcwLqaJdJ+vABD4r80Tm19FMYOnqQJBAOouVdFc8F9N9KRBM5urz0vY8ypjHwcl9hg05KXvyK/xW5N+V7SmN9Fts1U7jpFz4p7gGA5pcInAwFu7iJBi1i8CQD8JuuMJy0t7+r8juZ6ynws40TZ3nj5yctDzuzP5s82Qy1Gj+Z9q826q89cv1w7cWCNONFhp3auR0ZmuLDc7GfECQHZYtEX6EgAYBWp9CPfC/B/4o+rn7OZP6O6SzHqPk3xXHVCMqQZCejL8nYSVdJdNWVmxJnciEh2Lq6qwO3O8f0ECQHBRRmcghV2jRlvZmVOe92cxDejC7QGG3A+rr1hc2Dx2HEvZ5hs/n96CY7hyA9OLJ/YwrpetuZ2+w/35DunyquA=");

    /// <summary>
    /// 独立的公开合成 PKCS#1 DER 公钥，用于固定错误密钥的验签失败语义
    /// </summary>
    private static readonly byte[] OtherPublicKeyDer = Convert.FromBase64String(
        "MIGJAoGBAN4R7oLvaAtbcRV46OpkAE4s6gygF9943dMuxAu/lh+D16xFH1jvix+ayh48FGd+9zDGPjuPBCg3RIqdz8pW94q8q+3bOv4etIwMTGK+QxQKqXSGg2Qe8tA7V8jrBWGj8uAwqGpyTtDoBJfiCyQY64SXzLD003SiWBSt1Tfzbz7ZAgMBAAE=");

    /// <summary>
    /// PKCS#1 PEM 公钥夹具
    /// </summary>
    private static readonly string PublicKeyPem = PemEncoding.WriteString("RSA PUBLIC KEY", PublicKeyDer);

    /// <summary>
    /// PKCS#1 PEM 私钥夹具
    /// </summary>
    private static readonly string PrivateKeyPem = PemEncoding.WriteString("RSA PRIVATE KEY", PrivateKeyDer);

    /// <summary>
    /// 独立的 PKCS#1 PEM 公钥夹具
    /// </summary>
    private static readonly string OtherPublicKeyPem = PemEncoding.WriteString("RSA PUBLIC KEY", OtherPublicKeyDer);

    /// <summary>
    /// 验证 PKCS#1 PEM 公钥加密的载荷可由对应私钥解密
    /// </summary>
    [Fact]
    public void EncryptAndDecrypt_WithPkcs1PemFixture_PreservesPlaintext()
    {
        const string plaintext = "RSA migration fixture";

        var ciphertext = RsaCryptography.Encrypt(plaintext, PublicKeyPem);
        var decrypted = RsaCryptography.Decrypt(ciphertext, PrivateKeyPem);

        decrypted.Should().Be(plaintext);
    }

    /// <summary>
    /// 验证 PKCS#1 DER 公钥加密的载荷可由对应私钥解密
    /// </summary>
    [Fact]
    public void EncryptAndDecrypt_WithPkcs1DerFixture_PreservesPlaintext()
    {
        var plaintext = Encoding.UTF8.GetBytes("RSA migration fixture");

        var ciphertext = RsaCryptography.Encrypt(plaintext, PublicKeyDer);
        var decrypted = RsaCryptography.Decrypt(ciphertext, PrivateKeyDer);

        decrypted.Should().BeEquivalentTo(plaintext);
    }

    /// <summary>
    /// 验证 PKCS#1 PEM 私钥签名可由对应公钥验证
    /// </summary>
    [Fact]
    public void SignAndVerify_WithPkcs1PemFixture_AcceptsMatchingSignature()
    {
        const string payload = "RSA signature fixture";

        var signature = RsaCryptography.Sign(payload, PrivateKeyPem);
        var isValid = RsaCryptography.VerifySignature(payload, signature, PublicKeyPem);

        isValid.Should().BeTrue();
    }

    /// <summary>
    /// 验证不同公钥无法验证固定私钥产生的签名
    /// </summary>
    [Fact]
    public void VerifySignature_WithDifferentPublicKey_ReturnsFalse()
    {
        const string payload = "RSA signature fixture";
        var signature = RsaCryptography.Sign(payload, PrivateKeyPem);

        var isValid = RsaCryptography.VerifySignature(payload, signature, OtherPublicKeyPem);

        isValid.Should().BeFalse();
    }

    /// <summary>
    /// 验证非 Base64 签名按公开契约返回验证失败
    /// </summary>
    [Fact]
    public void VerifySignature_WithMalformedBase64Signature_ReturnsFalse()
    {
        var isValid = RsaCryptography.VerifySignature(
            "RSA signature fixture",
            "not-a-base64-signature",
            PublicKeyPem);

        isValid.Should().BeFalse();
    }
}
