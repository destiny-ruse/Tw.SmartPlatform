using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Tw.Core;

namespace Tw.Core.Security.Cryptography;

/// <summary>
/// 提供 PBKDF2 密码密钥派生、哈希和验证辅助方法
/// </summary>
/// <remarks>
/// 密码哈希使用 <c>PBKDF2$HashAlgorithm$Iterations$KeyLength$SaltBase64$HashBase64</c> 格式
/// </remarks>
public static class Pbkdf2PasswordHasher
{
    private const int MinimumSaltLength = 8;
    private const char HashPartSeparator = '$';
    private const string FormatMarker = "PBKDF2";
    private static readonly Encoding DefaultEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
    private static readonly HashAlgorithmName DefaultHashAlgorithm = HashAlgorithmName.SHA256;

    /// <summary>从密码字符串派生 PBKDF2 密钥并以 Base64 返回</summary>
    /// <param name="password">用于派生的密码</param>
    /// <param name="salt">盐值字节</param>
    /// <param name="iterations">PBKDF2 迭代次数</param>
    /// <param name="keyLength">派生密钥长度，单位为字节</param>
    /// <param name="hashAlgorithm">PBKDF2 哈希算法；省略时使用 SHA-256</param>
    /// <param name="encoding">文本编码；省略时使用无字节顺序标记的 UTF-8</param>
    /// <returns>Base64 编码的派生密钥</returns>
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

    /// <summary>从密码字节派生 PBKDF2 密钥</summary>
    /// <param name="password">用于派生的密码字节</param>
    /// <param name="salt">盐值字节</param>
    /// <param name="iterations">PBKDF2 迭代次数</param>
    /// <param name="keyLength">派生密钥长度，单位为字节</param>
    /// <param name="hashAlgorithm">PBKDF2 哈希算法；省略时使用 SHA-256</param>
    /// <returns>派生密钥字节</returns>
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
            throw new ArgumentException($"盐值必须至少为 {MinimumSaltLength} 字节。", nameof(salt));
        }

        return Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            iterations,
            hashAlgorithm ?? DefaultHashAlgorithm,
            keyLength);
    }

    /// <summary>生成密码学随机盐值并以 Base64 返回</summary>
    /// <param name="length">盐值长度，单位为字节</param>
    /// <returns>Base64 编码的盐值</returns>
    public static string GenerateSalt(int length = 16)
    {
        return Convert.ToBase64String(GenerateSaltBytes(length));
    }

    /// <summary>生成密码学随机盐值字节</summary>
    /// <param name="length">盐值长度，单位为字节</param>
    /// <returns>盐值字节</returns>
    public static byte[] GenerateSaltBytes(int length = 16)
    {
        if (length < MinimumSaltLength)
        {
            throw new ArgumentException($"盐值长度必须至少为 {MinimumSaltLength} 字节。", nameof(length));
        }

        return RandomNumberGenerator.GetBytes(length);
    }

    /// <summary>使用 PBKDF2 和生成的盐值对密码进行哈希</summary>
    /// <param name="password">要哈希的密码</param>
    /// <param name="iterations">PBKDF2 迭代次数</param>
    /// <param name="keyLength">派生密钥长度，单位为字节</param>
    /// <param name="saltLength">生成的盐值长度，单位为字节</param>
    /// <param name="hashAlgorithm">PBKDF2 哈希算法；省略时使用 SHA-256</param>
    /// <param name="encoding">文本编码；省略时使用无字节顺序标记的 UTF-8</param>
    /// <returns>自描述 PBKDF2 密码哈希</returns>
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

    /// <summary>根据自描述 PBKDF2 密码哈希验证密码</summary>
    /// <param name="password">要验证的密码</param>
    /// <param name="hashedPassword"><see cref="HashPassword"/> 生成的密码哈希</param>
    /// <param name="iterations">为保持签名兼容而保留；自描述哈希使用其中存储的迭代次数</param>
    /// <param name="hashAlgorithm">为保持签名兼容而保留；自描述哈希使用其中存储的哈希算法</param>
    /// <param name="encoding">文本编码；省略时使用无字节顺序标记的 UTF-8</param>
    /// <returns>密码匹配时返回 <see langword="true"/>；否则返回 <see langword="false"/></returns>
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

    /// <summary>从密码字符串派生 PBKDF2 密钥并以十六进制返回</summary>
    /// <param name="password">用于派生的密码</param>
    /// <param name="salt">盐值字节</param>
    /// <param name="iterations">PBKDF2 迭代次数</param>
    /// <param name="keyLength">派生密钥长度，单位为字节</param>
    /// <param name="useUpperCase">是否返回大写十六进制字符</param>
    /// <param name="hashAlgorithm">PBKDF2 哈希算法；省略时使用 SHA-256</param>
    /// <param name="encoding">文本编码；省略时使用无字节顺序标记的 UTF-8</param>
    /// <returns>十六进制编码的派生密钥</returns>
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
