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
    public void GenerateSalt_Rejects_Short_Length()
    {
        var act = () => Pbkdf2PasswordHasher.GenerateSalt(7);
        act.Should().Throw<ArgumentException>();
    }
}
