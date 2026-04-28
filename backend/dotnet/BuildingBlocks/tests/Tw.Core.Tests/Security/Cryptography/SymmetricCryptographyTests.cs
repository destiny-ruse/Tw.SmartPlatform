using FluentAssertions;
using System.Security.Cryptography;
using System.Text;
using Tw.Core.Security.Cryptography;
using Tw.TestBase;
using Xunit;

namespace Tw.Core.Tests.Security.Cryptography;

public sealed class SymmetricCryptographyTests
{
    [Fact]
    public void AesCryptography_RoundTrips_String()
    {
        var encrypted = AesCryptography.Encrypt(TestBytes.LongText, TestBytes.AesKey16);

        var decrypted = AesCryptography.Decrypt(encrypted, TestBytes.AesKey16);

        decrypted.Should().Be(TestBytes.LongText);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void AesCryptography_RoundTrips_Blank_String(string plaintext)
    {
        var encrypted = AesCryptography.Encrypt(plaintext, TestBytes.AesKey16);

        var decrypted = AesCryptography.Decrypt(encrypted, TestBytes.AesKey16);

        decrypted.Should().Be(plaintext);
    }

    [Fact]
    public void AesCryptography_Rejects_Invalid_Key_Length()
    {
        var act = () => AesCryptography.Encrypt(TestBytes.LongTextUtf8, TestBytes.Utf8("short"));

        act.Should().Throw<ArgumentException>().WithParameterName("key");
    }

    [Fact]
    public void DesCryptography_RoundTrips_String()
    {
        var encrypted = DesCryptography.Encrypt(TestBytes.LongText, TestBytes.DesKey8);

        var decrypted = DesCryptography.Decrypt(encrypted, TestBytes.DesKey8);

        decrypted.Should().Be(TestBytes.LongText);
    }

    [Fact]
    public void TripleDesCryptography_RoundTrips_String()
    {
        var encrypted = TripleDesCryptography.Encrypt(TestBytes.LongText, TestBytes.TripleDesKey24);

        var decrypted = TripleDesCryptography.Decrypt(encrypted, TestBytes.TripleDesKey24);

        decrypted.Should().Be(TestBytes.LongText);
    }

    [Fact]
    public async Task AesCryptography_RoundTrips_Stream()
    {
        await using var stream = TestStreams.FromText(TestBytes.LongText);

        var encrypted = await AesCryptography.EncryptFileAsync(stream, TestBytes.Utf8(TestBytes.AesKey16));
        await using var encryptedStream = TestStreams.FromBytes(encrypted);

        var decrypted = await AesCryptography.DecryptFileAsync(encryptedStream, TestBytes.Utf8(TestBytes.AesKey16));

        decrypted.Should().Equal(TestBytes.LongTextUtf8);
    }

    [Fact]
    public void AesCryptography_Uses_Base64_Key_Bytes_For_String_Overloads()
    {
        var key = TestBytes.DeterministicBytes(32);
        var base64Key = Convert.ToBase64String(key);

        var encrypted = AesCryptography.Encrypt(TestBytes.LongText, base64Key, isKeyBase64: true);

        AesCryptography.Decrypt(encrypted, base64Key, isKeyBase64: true).Should().Be(TestBytes.LongText);
    }

    [Fact]
    public void AesCryptography_Does_Not_Prefix_Explicit_Iv()
    {
        var key = TestBytes.Utf8(TestBytes.AesKey16);
        var iv = TestBytes.DeterministicBytes(16);

        var encrypted = AesCryptography.Encrypt(TestBytes.AbcUtf8, key, iv);

        encrypted.Should().HaveCount(16);
        AesCryptography.Decrypt(encrypted, key, iv).Should().Equal(TestBytes.AbcUtf8);
    }

    [Fact]
    public void AesCryptography_Prefixes_Generated_Iv_For_Cbc()
    {
        var key = TestBytes.Utf8(TestBytes.AesKey16);

        var encrypted = AesCryptography.Encrypt(TestBytes.AbcUtf8, key);

        encrypted.Should().HaveCount(32);
        AesCryptography.Decrypt(encrypted, key).Should().Equal(TestBytes.AbcUtf8);
    }

    [Fact]
    public void AesCryptography_Ecb_Mode_Does_Not_Prefix_Iv()
    {
        var key = TestBytes.Utf8(TestBytes.AesKey16);

        var encrypted = AesCryptography.Encrypt(TestBytes.AbcUtf8, key, mode: CipherMode.ECB);

        encrypted.Should().HaveCount(16);
        AesCryptography.Decrypt(encrypted, key, mode: CipherMode.ECB).Should().Equal(TestBytes.AbcUtf8);
    }

    [Fact]
    public void AesCryptography_Rejects_Invalid_Iv_Length()
    {
        var key = TestBytes.Utf8(TestBytes.AesKey16);
        var invalidIv = TestBytes.DeterministicBytes(15);

        var act = () => AesCryptography.Encrypt(TestBytes.AbcUtf8, key, invalidIv);

        act.Should().Throw<ArgumentException>().WithParameterName("iv");
    }

    [Fact]
    public async Task AesCryptography_RoundTrips_File_Path()
    {
        using var directory = new TemporaryDirectory();
        var file = directory.WriteAllText("plain.txt", TestBytes.LongText, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

        var encrypted = await AesCryptography.EncryptFileAsync(file.FullName, TestBytes.Utf8(TestBytes.AesKey16));
        var encryptedPath = directory.GetPath("encrypted.bin");
        await File.WriteAllBytesAsync(encryptedPath, encrypted);

        var decrypted = await AesCryptography.DecryptFileAsync(encryptedPath, TestBytes.Utf8(TestBytes.AesKey16));

        decrypted.Should().Equal(TestBytes.LongTextUtf8);
    }

    [Fact]
    public async Task AesCryptography_Stream_Overloads_Do_Not_Dispose_Caller_Stream()
    {
        await using var plainStream = TestStreams.FromText(TestBytes.LongText);

        var encrypted = await AesCryptography.EncryptFileAsync(plainStream, TestBytes.Utf8(TestBytes.AesKey16));

        plainStream.CanRead.Should().BeTrue();
        await using var encryptedStream = TestStreams.FromBytes(encrypted);

        _ = await AesCryptography.DecryptFileAsync(encryptedStream, TestBytes.Utf8(TestBytes.AesKey16));

        encryptedStream.CanRead.Should().BeTrue();
    }
}
