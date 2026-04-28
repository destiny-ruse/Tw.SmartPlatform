using FluentAssertions;
using Tw.Core.Security.Cryptography;
using Tw.TestBase;
using Xunit;

namespace Tw.Core.Tests.Security.Cryptography;

public sealed class RsaCryptographyTests
{
    [Fact]
    public void RsaCryptography_RoundTrips_String_With_Pem_Key()
    {
        var keys = RsaCryptography.GenerateKeyPair();

        var encrypted = RsaCryptography.Encrypt(TestBytes.LongText, keys.PublicKeyPem);
        var decrypted = RsaCryptography.Decrypt(encrypted, keys.PrivateKeyPem);

        decrypted.Should().Be(TestBytes.LongText);
    }

    [Fact]
    public void RsaCryptography_Signs_And_Verifies()
    {
        var keys = RsaCryptography.GenerateKeyPair();

        var signature = RsaCryptography.Sign(TestBytes.LongText, keys.PrivateKeyPem);

        RsaCryptography.VerifySignature(TestBytes.LongText, signature, keys.PublicKeyPem).Should().BeTrue();
        RsaCryptography.VerifySignature("changed", signature, keys.PublicKeyPem).Should().BeFalse();
    }

    [Fact]
    public void RsaCryptography_RoundTrips_Bytes_With_Der_Key()
    {
        var keys = RsaCryptography.GenerateDerKeyPair();

        var encrypted = RsaCryptography.Encrypt(TestBytes.AbcUtf8, keys.PublicKeyDer);
        var decrypted = RsaCryptography.Decrypt(encrypted, keys.PrivateKeyDer);

        decrypted.Should().Equal(TestBytes.AbcUtf8);
    }
}
