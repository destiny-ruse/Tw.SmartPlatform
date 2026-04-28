using FluentAssertions;
using System.Text;
using Tw.Core.Security.Cryptography;
using Tw.TestBase;
using Xunit;

namespace Tw.Core.Tests.Security.Cryptography;

public sealed class HmacHasherTests
{
    [Theory]
    [InlineData(nameof(HmacMd5Hasher), CryptoTestVectors.HmacMd5Fox)]
    [InlineData(nameof(HmacSha1Hasher), CryptoTestVectors.HmacSha1Fox)]
    [InlineData(nameof(HmacSha256Hasher), CryptoTestVectors.HmacSha256Fox)]
    [InlineData(nameof(HmacSha384Hasher), CryptoTestVectors.HmacSha384Fox)]
    [InlineData(nameof(HmacSha512Hasher), CryptoTestVectors.HmacSha512Fox)]
    [InlineData(nameof(HmacSha3256Hasher), CryptoTestVectors.HmacSha3256Fox)]
    [InlineData(nameof(HmacSha3384Hasher), CryptoTestVectors.HmacSha3384Fox)]
    [InlineData(nameof(HmacSha3512Hasher), CryptoTestVectors.HmacSha3512Fox)]
    public void ComputeHash_Returns_Known_Vector(string hasherName, string expected)
    {
        var actual = ComputeHash(hasherName);

        actual.Should().Be(expected);
    }

    [Theory]
    [InlineData(nameof(HmacMd5Hasher), CryptoTestVectors.HmacMd5Fox)]
    [InlineData(nameof(HmacSha1Hasher), CryptoTestVectors.HmacSha1Fox)]
    [InlineData(nameof(HmacSha256Hasher), CryptoTestVectors.HmacSha256Fox)]
    [InlineData(nameof(HmacSha384Hasher), CryptoTestVectors.HmacSha384Fox)]
    [InlineData(nameof(HmacSha512Hasher), CryptoTestVectors.HmacSha512Fox)]
    [InlineData(nameof(HmacSha3256Hasher), CryptoTestVectors.HmacSha3256Fox)]
    [InlineData(nameof(HmacSha3384Hasher), CryptoTestVectors.HmacSha3384Fox)]
    [InlineData(nameof(HmacSha3512Hasher), CryptoTestVectors.HmacSha3512Fox)]
    public void VerifyHash_Returns_True_For_Matching_Hmac(string hasherName, string expected)
    {
        VerifyHash(hasherName, expected).Should().BeTrue();
    }

    [Fact]
    public void ComputeHash_Supports_Uppercase_And_Byte_Input()
    {
        HmacSha256Hasher.ComputeHash(TestBytes.HmacKeyUtf8, TestBytes.LongTextUtf8, useUpperCase: true)
            .Should().Be(CryptoTestVectors.HmacSha256Fox.ToUpperInvariant());
    }

    [Fact]
    public void HmacMd5Hasher_Computes_Short_Hash_From_Middle_Characters()
    {
        HmacMd5Hasher.ComputeHash(TestBytes.HmacKey, TestBytes.LongText, useShortHash: true)
            .Should().Be("463e7749b90c2dc2");
        HmacMd5Hasher.ComputeHash(TestBytes.HmacKeyUtf8, TestBytes.LongTextUtf8, useUpperCase: true, useShortHash: true)
            .Should().Be("463E7749B90C2DC2");
    }

    [Fact]
    public void VerifyHash_Returns_False_For_Different_Or_Invalid_Hash()
    {
        HmacSha256Hasher.VerifyHash(TestBytes.HmacKey, TestBytes.LongText, CryptoTestVectors.HmacSha256Fox.ToUpperInvariant())
            .Should().BeTrue();
        HmacSha256Hasher.VerifyHash(TestBytes.HmacKeyUtf8, TestBytes.LongTextUtf8, CryptoTestVectors.HmacSha512Fox)
            .Should().BeFalse();
        HmacSha256Hasher.VerifyHash(TestBytes.HmacKey, TestBytes.LongText, "not-hex").Should().BeFalse();
    }

    [Fact]
    public void VerifyHash_Uses_Supplied_Encoding()
    {
        var expected = HmacSha1Hasher.ComputeHash(
            Encoding.Unicode.GetBytes(TestBytes.HmacKey),
            Encoding.Unicode.GetBytes(TestBytes.Text));

        HmacSha1Hasher.VerifyHash(TestBytes.HmacKey, TestBytes.Text, expected, Encoding.Unicode)
            .Should().BeTrue();
    }

    [Fact]
    public async Task ComputeFileHashAsync_Reads_File_Path()
    {
        using var directory = new TemporaryDirectory();
        var file = directory.WriteAllText("hmac.txt", TestBytes.LongText, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

        var hash = await HmacSha384Hasher.ComputeFileHashAsync(TestBytes.HmacKey, file.FullName);

        hash.Should().Be(CryptoTestVectors.HmacSha384Fox);
    }

    [Fact]
    public async Task ComputeFileHashAsync_Reads_Stream_Without_Disposing_It()
    {
        await using var stream = TestStreams.FromText(TestBytes.LongText);

        var hash = await HmacSha512Hasher.ComputeFileHashAsync(TestBytes.HmacKeyUtf8, stream);

        hash.Should().Be(CryptoTestVectors.HmacSha512Fox);
        stream.CanRead.Should().BeTrue();
    }

    [Fact]
    public async Task HmacSha3Fallback_Can_Read_Stream_Incrementally()
    {
        await using var stream = new CopyToForbiddenStream(TestBytes.LongTextUtf8);

        var hash = await HmacSha3256Hasher.ComputeFileHashAsync(TestBytes.HmacKeyUtf8, stream);

        hash.Should().Be(CryptoTestVectors.HmacSha3256Fox);
    }

    private static string ComputeHash(string hasherName)
    {
        return hasherName switch
        {
            nameof(HmacMd5Hasher) => HmacMd5Hasher.ComputeHash(TestBytes.HmacKey, TestBytes.LongText),
            nameof(HmacSha1Hasher) => HmacSha1Hasher.ComputeHash(TestBytes.HmacKey, TestBytes.LongText),
            nameof(HmacSha256Hasher) => HmacSha256Hasher.ComputeHash(TestBytes.HmacKey, TestBytes.LongText),
            nameof(HmacSha384Hasher) => HmacSha384Hasher.ComputeHash(TestBytes.HmacKey, TestBytes.LongText),
            nameof(HmacSha512Hasher) => HmacSha512Hasher.ComputeHash(TestBytes.HmacKey, TestBytes.LongText),
            nameof(HmacSha3256Hasher) => HmacSha3256Hasher.ComputeHash(TestBytes.HmacKey, TestBytes.LongText),
            nameof(HmacSha3384Hasher) => HmacSha3384Hasher.ComputeHash(TestBytes.HmacKey, TestBytes.LongText),
            nameof(HmacSha3512Hasher) => HmacSha3512Hasher.ComputeHash(TestBytes.HmacKey, TestBytes.LongText),
            _ => throw new InvalidOperationException($"未知 HMAC 哈希器：{hasherName}"),
        };
    }

    private static bool VerifyHash(string hasherName, string expected)
    {
        return hasherName switch
        {
            nameof(HmacMd5Hasher) => HmacMd5Hasher.VerifyHash(TestBytes.HmacKey, TestBytes.LongText, expected),
            nameof(HmacSha1Hasher) => HmacSha1Hasher.VerifyHash(TestBytes.HmacKey, TestBytes.LongText, expected),
            nameof(HmacSha256Hasher) => HmacSha256Hasher.VerifyHash(TestBytes.HmacKey, TestBytes.LongText, expected),
            nameof(HmacSha384Hasher) => HmacSha384Hasher.VerifyHash(TestBytes.HmacKey, TestBytes.LongText, expected),
            nameof(HmacSha512Hasher) => HmacSha512Hasher.VerifyHash(TestBytes.HmacKey, TestBytes.LongText, expected),
            nameof(HmacSha3256Hasher) => HmacSha3256Hasher.VerifyHash(TestBytes.HmacKey, TestBytes.LongText, expected),
            nameof(HmacSha3384Hasher) => HmacSha3384Hasher.VerifyHash(TestBytes.HmacKey, TestBytes.LongText, expected),
            nameof(HmacSha3512Hasher) => HmacSha3512Hasher.VerifyHash(TestBytes.HmacKey, TestBytes.LongText, expected),
            _ => throw new InvalidOperationException($"未知 HMAC 哈希器：{hasherName}"),
        };
    }

    private sealed class CopyToForbiddenStream(byte[] buffer) : MemoryStream(buffer)
    {
        public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("流哈希必须增量读取，而不是复制整个流。");
        }
    }
}
