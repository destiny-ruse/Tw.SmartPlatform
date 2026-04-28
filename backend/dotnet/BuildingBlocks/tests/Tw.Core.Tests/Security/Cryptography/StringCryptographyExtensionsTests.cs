using FluentAssertions;
using System.Security.Cryptography;
using System.Text;
using Tw.Core.Security.Cryptography;
using Tw.TestBase;
using Xunit;

namespace Tw.Core.Tests.Security.Cryptography;

public sealed class StringCryptographyExtensionsTests
{
    [Fact]
    public void Hash_Convenience_Methods_Use_Backend_Hashers()
    {
        TestBytes.Text.ComputeSha256Hash().Should().Be(CryptoTestVectors.Sha256Abc);
        TestBytes.Text.VerifySha256Hash(CryptoTestVectors.Sha256Abc).Should().BeTrue();
    }

    [Fact]
    public void Md5_Convenience_Methods_Forward_Uppercase_And_Short_Hash_Options()
    {
        var expected = Md5Hasher.ComputeHash(TestBytes.Text, useUpperCase: true, useShortHash: true);

        TestBytes.Text.ComputeMd5Hash(useUpperCase: true, useShortHash: true).Should().Be(expected);
        TestBytes.Text.VerifyMd5Hash(expected, useShortHash: true).Should().BeTrue();
    }

    [Fact]
    public void Hmac_Convenience_Methods_Use_Backend_Hashers()
    {
        TestBytes.LongText.ComputeHmacSha256Hash(TestBytes.HmacKey).Should().Be(CryptoTestVectors.HmacSha256Fox);
        TestBytes.LongText.VerifyHmacSha256Hash(TestBytes.HmacKey, CryptoTestVectors.HmacSha256Fox).Should().BeTrue();
    }

    [Fact]
    public void Hmac_Convenience_Methods_Forward_Key_Input_Order_And_Encoding()
    {
        var expected = HmacSha1Hasher.ComputeHash(
            TestBytes.HmacKey,
            TestBytes.Text,
            useUpperCase: true,
            encoding: Encoding.Unicode);

        TestBytes.Text
            .ComputeHmacSha1Hash(TestBytes.HmacKey, useUpperCase: true, encoding: Encoding.Unicode)
            .Should().Be(expected);
        TestBytes.Text.VerifyHmacSha1Hash(TestBytes.HmacKey, expected, Encoding.Unicode).Should().BeTrue();
    }

    [Fact]
    public void Aes_Convenience_Methods_RoundTrip()
    {
        var encrypted = TestBytes.LongText.EncryptWithAes(TestBytes.AesKey16);

        encrypted.DecryptWithAes(TestBytes.AesKey16).Should().Be(TestBytes.LongText);
    }

    [Fact]
    public void Aes_Convenience_Methods_Forward_Base64_Key_Option()
    {
        var key = TestBytes.DeterministicBytes(32);
        var base64Key = Convert.ToBase64String(key);
        var iv = TestBytes.DeterministicBytes(16);
        var expected = AesCryptography.Encrypt(TestBytes.LongText, base64Key, iv, isKeyBase64: true);

        var encrypted = TestBytes.LongText.EncryptWithAes(base64Key, iv, isKeyBase64: true);

        encrypted.Should().Be(expected);
        encrypted.DecryptWithAes(base64Key, iv, isKeyBase64: true).Should().Be(TestBytes.LongText);
    }

    [Fact]
    public void Des_And_TripleDes_Convenience_Methods_RoundTrip()
    {
        var desEncrypted = TestBytes.LongText.EncryptWithDes(TestBytes.DesKey8);
        var tripleDesEncrypted = TestBytes.LongText.EncryptWithTripleDes(TestBytes.TripleDesKey24);

        desEncrypted.DecryptWithDes(TestBytes.DesKey8).Should().Be(TestBytes.LongText);
        tripleDesEncrypted.DecryptWithTripleDes(TestBytes.TripleDesKey24).Should().Be(TestBytes.LongText);
    }

    [Fact]
    public void Rsa_Convenience_Methods_Sign_And_Verify()
    {
        var keys = RsaCryptography.GenerateKeyPair();

        var signature = TestBytes.LongText.SignWithRsa(keys.PrivateKeyPem);

        TestBytes.LongText.VerifyRsaSignature(signature, keys.PublicKeyPem).Should().BeTrue();
    }

    [Fact]
    public void Rsa_Convenience_Methods_Forward_Explicit_Signature_Options()
    {
        var keys = RsaCryptography.GenerateKeyPair();
        var expected = RsaCryptography.Sign(
            TestBytes.LongText,
            keys.PrivateKeyPem,
            HashAlgorithmName.SHA384,
            RSASignaturePadding.Pkcs1);

        var signature = TestBytes.LongText.SignWithRsa(
            keys.PrivateKeyPem,
            HashAlgorithmName.SHA384,
            RSASignaturePadding.Pkcs1);

        signature.Should().Be(expected);
        TestBytes.LongText
            .VerifyRsaSignature(signature, keys.PublicKeyPem, HashAlgorithmName.SHA384, RSASignaturePadding.Pkcs1)
            .Should().BeTrue();
    }

    [Fact]
    public void Rsa_Convenience_Methods_Encrypt_And_Decrypt()
    {
        var keys = RsaCryptography.GenerateKeyPair();

        var encrypted = TestBytes.Text.EncryptWithRsa(keys.PublicKeyPem);

        encrypted.DecryptWithRsa(keys.PrivateKeyPem).Should().Be(TestBytes.Text);
    }

    [Fact]
    public void Pbkdf2_Convenience_Methods_Hash_And_Verify()
    {
        var hashed = "correct horse battery staple".HashPasswordWithPbkdf2(iterations: 10_000);

        "correct horse battery staple".VerifyPbkdf2Password(hashed).Should().BeTrue();
        "wrong".VerifyPbkdf2Password(hashed).Should().BeFalse();
    }

    [Fact]
    public void Pbkdf2_Convenience_Methods_Forward_Metadata_Options()
    {
        var hashed = "correct horse battery staple".HashPasswordWithPbkdf2(
            iterations: 12_345,
            keyLength: 48,
            saltLength: 24,
            hashAlgorithm: HashAlgorithmName.SHA512);
        var parts = hashed.Split('$');

        parts.Should().HaveCount(6);
        parts[1].Should().Be(HashAlgorithmName.SHA512.Name);
        parts[2].Should().Be("12345");
        parts[3].Should().Be("48");
        Convert.FromBase64String(parts[4]).Should().HaveCount(24);
        Pbkdf2PasswordHasher.VerifyPassword("correct horse battery staple", hashed).Should().BeTrue();
        "correct horse battery staple".VerifyPbkdf2Password(hashed).Should().BeTrue();
    }
}
