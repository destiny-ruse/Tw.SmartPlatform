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
}
