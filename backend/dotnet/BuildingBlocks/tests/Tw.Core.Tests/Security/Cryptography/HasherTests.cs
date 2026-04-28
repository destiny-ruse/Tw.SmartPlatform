using FluentAssertions;
using System.Text;
using Tw.Core.Security.Cryptography;
using Tw.TestBase;
using Xunit;

namespace Tw.Core.Tests.Security.Cryptography;

public sealed class HasherTests
{
    [Theory]
    [InlineData(nameof(Md5Hasher), CryptoTestVectors.Md5Abc)]
    [InlineData(nameof(Sha1Hasher), CryptoTestVectors.Sha1Abc)]
    [InlineData(nameof(Sha256Hasher), CryptoTestVectors.Sha256Abc)]
    [InlineData(nameof(Sha384Hasher), CryptoTestVectors.Sha384Abc)]
    [InlineData(nameof(Sha512Hasher), CryptoTestVectors.Sha512Abc)]
    [InlineData(nameof(Sha3256Hasher), CryptoTestVectors.Sha3256Abc)]
    [InlineData(nameof(Sha3384Hasher), CryptoTestVectors.Sha3384Abc)]
    [InlineData(nameof(Sha3512Hasher), CryptoTestVectors.Sha3512Abc)]
    public void ComputeHash_Returns_Known_Vector(string hasherName, string expected)
    {
        var actual = hasherName switch
        {
            nameof(Md5Hasher) => Md5Hasher.ComputeHash(TestBytes.Text),
            nameof(Sha1Hasher) => Sha1Hasher.ComputeHash(TestBytes.Text),
            nameof(Sha256Hasher) => Sha256Hasher.ComputeHash(TestBytes.Text),
            nameof(Sha384Hasher) => Sha384Hasher.ComputeHash(TestBytes.Text),
            nameof(Sha512Hasher) => Sha512Hasher.ComputeHash(TestBytes.Text),
            nameof(Sha3256Hasher) => Sha3256Hasher.ComputeHash(TestBytes.Text),
            nameof(Sha3384Hasher) => Sha3384Hasher.ComputeHash(TestBytes.Text),
            nameof(Sha3512Hasher) => Sha3512Hasher.ComputeHash(TestBytes.Text),
            _ => throw new InvalidOperationException(hasherName),
        };

        actual.Should().Be(expected);
    }

    [Fact]
    public void ComputeHash_Supports_Uppercase_And_Byte_Input()
    {
        Sha256Hasher.ComputeHash(TestBytes.AbcUtf8, useUpperCase: true)
            .Should().Be(CryptoTestVectors.Sha256Abc.ToUpperInvariant());
    }

    [Fact]
    public void Md5Hasher_Computes_Short_Hash_From_Middle_Characters()
    {
        Md5Hasher.ComputeHash(TestBytes.Text, useShortHash: true).Should().Be("3cd24fb0d6963f7d");
        Md5Hasher.ComputeHash(TestBytes.AbcUtf8, useUpperCase: true, useShortHash: true).Should().Be("3CD24FB0D6963F7D");
    }

    [Fact]
    public void Sha3Hashers_Return_Known_Empty_Input_Vectors()
    {
        var bytes = Array.Empty<byte>();

        Sha3256Hasher.ComputeHash(bytes).Should().Be("a7ffc6f8bf1ed76651c14756a061d662f580ff4de43b49fa82d80a4b80f8434a");
        Sha3384Hasher.ComputeHash(bytes).Should().Be("0c63a75b845e4f7d01107d852e4c2485c51a50aaaa94fc61995e71bbee983a2ac3713831264adb47fb6bd1e058d5f004");
        Sha3512Hasher.ComputeHash(bytes).Should().Be("a69f73cca23a9ac5c8b567dc185a756e97c982164fe25859e0d1dcc1475c80a615b2123af1f5f94c11e3e9402c3ac558f500199d95b6d3e301758586281dcd26");
    }

    [Fact]
    public async Task Sha3Hashers_Return_Known_MultiBlock_Stream_Vector()
    {
        var input = new string('a', 200);
        var bytes = Encoding.UTF8.GetBytes(input);
        await using var stream = TestStreams.FromText(input);

        var hash = await Sha3256Hasher.ComputeFileHashAsync(stream);

        Sha3256Hasher.ComputeHash(bytes).Should().Be("cce34485baf2bf2aca99b94833892a4f52896d3d153f7b840cc4f9fe695f1387");
        Sha3384Hasher.ComputeHash(bytes).Should().Be("f97756776c1874724c94a8008f7f155553b4bf00fbf8fbeac246624ad59c258a3c0977d9f2543d7cbd75b9ac8fdc0d40");
        Sha3512Hasher.ComputeHash(bytes).Should().Be("eae6c85c6904f11075de9f9d5e1064371d000510fa3d2d79d40cf9be34892fb01859d0a0234e138bcb0ad5c84f6c0dca226a414b0c9a2897cb695f5185fe36ec");
        hash.Should().Be("cce34485baf2bf2aca99b94833892a4f52896d3d153f7b840cc4f9fe695f1387");
    }

    [Fact]
    public async Task Sha3FileHashFallback_Reads_Stream_Incrementally()
    {
        var input = Encoding.UTF8.GetBytes(new string('a', 200));
        await using var stream = new CopyToForbiddenStream(input);

        var hash = await Sha3256Hasher.ComputeFileHashAsync(stream);

        hash.Should().Be("cce34485baf2bf2aca99b94833892a4f52896d3d153f7b840cc4f9fe695f1387");
    }

    [Fact]
    public void VerifyHash_Uses_Hex_Normalization_And_Returns_False_For_Different_Or_Invalid_Hash()
    {
        Sha256Hasher.VerifyHash(TestBytes.Text, CryptoTestVectors.Sha256Abc.ToUpperInvariant()).Should().BeTrue();
        Sha256Hasher.VerifyHash(TestBytes.AbcUtf8, CryptoTestVectors.Sha256Abc).Should().BeTrue();
        Sha256Hasher.VerifyHash(TestBytes.Text, CryptoTestVectors.Sha512Abc).Should().BeFalse();
        Sha256Hasher.VerifyHash(TestBytes.Text, "not-hex").Should().BeFalse();
    }

    [Fact]
    public void VerifyHash_Validates_Null_Hash()
    {
        string hash = null!;

        var act = () => Sha256Hasher.VerifyHash(TestBytes.Text, hash);

        act.Should().Throw<ArgumentNullException>().WithParameterName(nameof(hash));
    }

    [Fact]
    public void ComputeHash_Uses_Supplied_Encoding()
    {
        var expected = Sha1Hasher.ComputeHash(Encoding.Unicode.GetBytes(TestBytes.Text));

        Sha1Hasher.ComputeHash(TestBytes.Text, encoding: Encoding.Unicode).Should().Be(expected);
    }

    [Fact]
    public async Task ComputeFileHashAsync_Reads_Stream_Without_Disposing_It()
    {
        await using var stream = TestStreams.FromText(TestBytes.Text);

        var hash = await Sha256Hasher.ComputeFileHashAsync(stream);

        hash.Should().Be(CryptoTestVectors.Sha256Abc);
        stream.CanRead.Should().BeTrue();
    }

    [Fact]
    public async Task ComputeFileHashAsync_Reads_File_Path()
    {
        using var directory = new TemporaryDirectory();
        var file = directory.WriteAllText("hash.txt", TestBytes.Text, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

        var hash = await Sha384Hasher.ComputeFileHashAsync(file.FullName);

        hash.Should().Be(CryptoTestVectors.Sha384Abc);
    }

    private sealed class CopyToForbiddenStream(byte[] buffer) : MemoryStream(buffer)
    {
        public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("Stream hashing must read incrementally instead of copying the whole stream.");
        }
    }
}
