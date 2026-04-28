using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Tw.Core;

namespace Tw.Core.Security.Cryptography;

/// <summary>
/// Provides PBKDF2 password key derivation, hashing, and verification helpers.
/// </summary>
/// <remarks>
/// Password hashes use <c>PBKDF2$HashAlgorithm$Iterations$KeyLength$SaltBase64$HashBase64</c>.
/// </remarks>
public static class Pbkdf2PasswordHasher
{
    private const int MinimumSaltLength = 8;
    private const char HashPartSeparator = '$';
    private const string FormatMarker = "PBKDF2";
    private static readonly Encoding DefaultEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
    private static readonly HashAlgorithmName DefaultHashAlgorithm = HashAlgorithmName.SHA256;

    /// <summary>Derives a PBKDF2 key from a password string and returns it as Base64.</summary>
    /// <param name="password">The password to derive from.</param>
    /// <param name="salt">The salt bytes.</param>
    /// <param name="iterations">The PBKDF2 iteration count.</param>
    /// <param name="keyLength">The derived key length in bytes.</param>
    /// <param name="hashAlgorithm">The PBKDF2 hash algorithm, or SHA-256 when omitted.</param>
    /// <param name="encoding">The text encoding, or UTF-8 without a byte order mark when omitted.</param>
    /// <returns>The derived key encoded as Base64.</returns>
    public static string DeriveKey(
        string password,
        byte[] salt,
        int iterations = 100000,
        int keyLength = 32,
        HashAlgorithmName? hashAlgorithm = null,
        Encoding? encoding = null)
    {
        Check.NotNull(password);

        var key = DeriveKey((encoding ?? DefaultEncoding).GetBytes(password), salt, iterations, keyLength, hashAlgorithm);
        return Convert.ToBase64String(key);
    }

    /// <summary>Derives a PBKDF2 key from password bytes.</summary>
    /// <param name="password">The password bytes to derive from.</param>
    /// <param name="salt">The salt bytes.</param>
    /// <param name="iterations">The PBKDF2 iteration count.</param>
    /// <param name="keyLength">The derived key length in bytes.</param>
    /// <param name="hashAlgorithm">The PBKDF2 hash algorithm, or SHA-256 when omitted.</param>
    /// <returns>The derived key bytes.</returns>
    public static byte[] DeriveKey(
        byte[] password,
        byte[] salt,
        int iterations = 100000,
        int keyLength = 32,
        HashAlgorithmName? hashAlgorithm = null)
    {
        Check.NotNull(password);
        Check.NotNull(salt);
        Check.Positive(iterations);
        Check.Positive(keyLength);

        if (salt.Length < MinimumSaltLength)
        {
            throw new ArgumentException($"Salt must be at least {MinimumSaltLength} bytes.", nameof(salt));
        }

        return Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            iterations,
            hashAlgorithm ?? DefaultHashAlgorithm,
            keyLength);
    }

    /// <summary>Generates a cryptographically random salt and returns it as Base64.</summary>
    /// <param name="length">The salt length in bytes.</param>
    /// <returns>The salt encoded as Base64.</returns>
    public static string GenerateSalt(int length = 16)
    {
        return Convert.ToBase64String(GenerateSaltBytes(length));
    }

    /// <summary>Generates cryptographically random salt bytes.</summary>
    /// <param name="length">The salt length in bytes.</param>
    /// <returns>The salt bytes.</returns>
    public static byte[] GenerateSaltBytes(int length = 16)
    {
        if (length < MinimumSaltLength)
        {
            throw new ArgumentException($"Salt length must be at least {MinimumSaltLength} bytes.", nameof(length));
        }

        return RandomNumberGenerator.GetBytes(length);
    }

    /// <summary>Hashes a password using PBKDF2 and a generated salt.</summary>
    /// <param name="password">The password to hash.</param>
    /// <param name="iterations">The PBKDF2 iteration count.</param>
    /// <param name="keyLength">The derived key length in bytes.</param>
    /// <param name="saltLength">The generated salt length in bytes.</param>
    /// <param name="hashAlgorithm">The PBKDF2 hash algorithm, or SHA-256 when omitted.</param>
    /// <param name="encoding">The text encoding, or UTF-8 without a byte order mark when omitted.</param>
    /// <returns>A self-describing PBKDF2 password hash.</returns>
    public static string HashPassword(
        string password,
        int iterations = 100000,
        int keyLength = 32,
        int saltLength = 16,
        HashAlgorithmName? hashAlgorithm = null,
        Encoding? encoding = null)
    {
        Check.NotNull(password);

        var actualHashAlgorithm = hashAlgorithm ?? DefaultHashAlgorithm;
        var salt = GenerateSaltBytes(saltLength);
        var key = DeriveKey((encoding ?? DefaultEncoding).GetBytes(password), salt, iterations, keyLength, actualHashAlgorithm);

        return string.Join(
            HashPartSeparator,
            FormatMarker,
            actualHashAlgorithm.Name,
            iterations.ToString(CultureInfo.InvariantCulture),
            keyLength.ToString(CultureInfo.InvariantCulture),
            Convert.ToBase64String(salt),
            Convert.ToBase64String(key));
    }

    /// <summary>Verifies a password against a self-describing PBKDF2 password hash.</summary>
    /// <param name="password">The password to verify.</param>
    /// <param name="hashedPassword">The password hash produced by <see cref="HashPassword"/>.</param>
    /// <param name="iterations">Retained for signature compatibility; self-describing hashes use their stored iteration count.</param>
    /// <param name="hashAlgorithm">Retained for signature compatibility; self-describing hashes use their stored hash algorithm.</param>
    /// <param name="encoding">The text encoding, or UTF-8 without a byte order mark when omitted.</param>
    /// <returns><see langword="true"/> when the password matches; otherwise, <see langword="false"/>.</returns>
    public static bool VerifyPassword(
        string password,
        string hashedPassword,
        int iterations = 100000,
        HashAlgorithmName? hashAlgorithm = null,
        Encoding? encoding = null)
    {
        Check.NotNull(password);
        Check.NotNullOrWhiteSpace(hashedPassword);

        if (!TryParseHash(hashedPassword, out var parsedHash))
        {
            return false;
        }

        var key = DeriveKey(
            (encoding ?? DefaultEncoding).GetBytes(password),
            parsedHash.Salt,
            parsedHash.Iterations,
            parsedHash.Hash.Length,
            parsedHash.HashAlgorithm);

        return key.Length == parsedHash.Hash.Length &&
            CryptographicOperations.FixedTimeEquals(key, parsedHash.Hash);
    }

    /// <summary>Derives a PBKDF2 key from a password string and returns it as hexadecimal.</summary>
    /// <param name="password">The password to derive from.</param>
    /// <param name="salt">The salt bytes.</param>
    /// <param name="iterations">The PBKDF2 iteration count.</param>
    /// <param name="keyLength">The derived key length in bytes.</param>
    /// <param name="useUpperCase">Whether to return uppercase hexadecimal characters.</param>
    /// <param name="hashAlgorithm">The PBKDF2 hash algorithm, or SHA-256 when omitted.</param>
    /// <param name="encoding">The text encoding, or UTF-8 without a byte order mark when omitted.</param>
    /// <returns>The derived key encoded as hexadecimal.</returns>
    public static string DeriveKeyToHex(
        string password,
        byte[] salt,
        int iterations = 100000,
        int keyLength = 32,
        bool useUpperCase = false,
        HashAlgorithmName? hashAlgorithm = null,
        Encoding? encoding = null)
    {
        Check.NotNull(password);

        var key = DeriveKey((encoding ?? DefaultEncoding).GetBytes(password), salt, iterations, keyLength, hashAlgorithm);
        return HexEncoding.ToHex(key, useUpperCase);
    }

    private static bool TryParseHash(string hashedPassword, out ParsedPasswordHash parsedHash)
    {
        parsedHash = default;

        var parts = hashedPassword.Split(HashPartSeparator);
        if (parts.Length != 6 || !string.Equals(parts[0], FormatMarker, StringComparison.Ordinal))
        {
            return false;
        }

        if (!int.TryParse(parts[2], NumberStyles.None, CultureInfo.InvariantCulture, out var iterations) ||
            !int.TryParse(parts[3], NumberStyles.None, CultureInfo.InvariantCulture, out var keyLength) ||
            iterations <= 0 ||
            keyLength <= 0)
        {
            return false;
        }

        byte[] salt;
        byte[] hash;

        try
        {
            salt = Convert.FromBase64String(parts[4]);
            hash = Convert.FromBase64String(parts[5]);
        }
        catch (FormatException)
        {
            return false;
        }

        if (salt.Length < MinimumSaltLength || hash.Length != keyLength)
        {
            return false;
        }

        var hashAlgorithm = new HashAlgorithmName(parts[1]);
        if (!IsSupportedHashAlgorithm(hashAlgorithm))
        {
            return false;
        }

        parsedHash = new ParsedPasswordHash(hashAlgorithm, iterations, salt, hash);
        return true;
    }

    private static bool IsSupportedHashAlgorithm(HashAlgorithmName hashAlgorithm)
    {
        return hashAlgorithm.Equals(HashAlgorithmName.SHA1) ||
            hashAlgorithm.Equals(HashAlgorithmName.SHA256) ||
            hashAlgorithm.Equals(HashAlgorithmName.SHA384) ||
            hashAlgorithm.Equals(HashAlgorithmName.SHA512);
    }

    private readonly record struct ParsedPasswordHash(
        HashAlgorithmName HashAlgorithm,
        int Iterations,
        byte[] Salt,
        byte[] Hash);
}
