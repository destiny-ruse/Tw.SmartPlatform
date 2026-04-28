using System.Security.Cryptography;
using FluentAssertions;
using Tw.Core.Security.Cryptography;
using Xunit;

namespace Tw.Core.Tests.Security.Cryptography;

public sealed class Pbkdf2PasswordHasherTests
{
    [Fact]
    public void DeriveKey_Returns_Deterministic_Key_For_Salt()
    {
        var salt = Convert.FromBase64String("c2FsdHNhbHRzYWx0MTIzNA==");

        var first = Pbkdf2PasswordHasher.DeriveKey("password", salt, iterations: 10_000, keyLength: 32, HashAlgorithmName.SHA256);
        var second = Pbkdf2PasswordHasher.DeriveKey("password", salt, iterations: 10_000, keyLength: 32, HashAlgorithmName.SHA256);

        first.Should().Be(second);
    }

    [Fact]
    public void HashPassword_And_VerifyPassword_RoundTrip()
    {
        var hashed = Pbkdf2PasswordHasher.HashPassword("correct horse battery staple");

        Pbkdf2PasswordHasher.VerifyPassword("correct horse battery staple", hashed).Should().BeTrue();
        Pbkdf2PasswordHasher.VerifyPassword("wrong", hashed).Should().BeFalse();
    }

    [Fact]
    public void VerifyPassword_Uses_Stored_Metadata_When_Caller_Supplies_Different_Parameters()
    {
        var hashed = Pbkdf2PasswordHasher.HashPassword(
            "correct horse battery staple",
            iterations: 12_345,
            hashAlgorithm: HashAlgorithmName.SHA512);

        Pbkdf2PasswordHasher.VerifyPassword(
                "correct horse battery staple",
                hashed,
                iterations: 1,
                hashAlgorithm: HashAlgorithmName.SHA1)
            .Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("NotAHash")]
    public void VerifyPassword_Returns_False_For_Malformed_Hash_With_Unsupported_Algorithm(string algorithm)
    {
        var hashed = BuildHash(algorithm, new byte[16], new byte[32]);

        var act = () => Pbkdf2PasswordHasher.VerifyPassword("password", hashed);

        act.Should().NotThrow().Which.Should().BeFalse();
    }

    [Fact]
    public void GenerateSalt_Rejects_Short_Length()
    {
        var act = () => Pbkdf2PasswordHasher.GenerateSalt(7);
        act.Should().Throw<ArgumentException>();
    }

    private static string BuildHash(string algorithm, byte[] salt, byte[] hash)
    {
        return string.Join(
            '$',
            "PBKDF2",
            algorithm,
            "10000",
            hash.Length.ToString(),
            Convert.ToBase64String(salt),
            Convert.ToBase64String(hash));
    }
}
