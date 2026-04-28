using FluentAssertions;
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
    public void Hmac_Convenience_Methods_Use_Backend_Hashers()
    {
        TestBytes.LongText.ComputeHmacSha256Hash(TestBytes.HmacKey).Should().Be(CryptoTestVectors.HmacSha256Fox);
        TestBytes.LongText.VerifyHmacSha256Hash(TestBytes.HmacKey, CryptoTestVectors.HmacSha256Fox).Should().BeTrue();
    }

    [Fact]
    public void Aes_Convenience_Methods_RoundTrip()
    {
        var encrypted = TestBytes.LongText.EncryptWithAes(TestBytes.AesKey16);

        encrypted.DecryptWithAes(TestBytes.AesKey16).Should().Be(TestBytes.LongText);
    }

    [Fact]
    public void Rsa_Convenience_Methods_Sign_And_Verify()
    {
        var keys = RsaCryptography.GenerateKeyPair();

        var signature = TestBytes.LongText.SignWithRsa(keys.PrivateKeyPem);

        TestBytes.LongText.VerifyRsaSignature(signature, keys.PublicKeyPem).Should().BeTrue();
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
}
