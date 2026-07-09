using System.Security.Cryptography;
using System.Text;
using Tw.Core;

namespace Tw.Core.Security.Cryptography;

/// <summary>
/// 提供用于字符串、字节、文件和流的 AES 加密与解密辅助方法
/// </summary>
public static class AesCryptography
{
    /// <summary>加密字符串并返回 Base64 密文</summary>
    /// <param name="input">要加密的明文字符串</param>
    /// <param name="key">文本形式的加密密钥；当 <paramref name="isKeyBase64"/> 为 <see langword="true"/> 时表示 Base64 密钥字节</param>
    /// <param name="iv">初始化向量；对非 ECB 模式传入 <see langword="null"/> 时会前置生成的 IV</param>
    /// <param name="mode">密码模式</param>
    /// <param name="padding">填充模式</param>
    /// <param name="isKeyBase64"><paramref name="key"/> 是否包含 Base64 编码的密钥字节</param>
    /// <param name="encoding">文本编码；省略时使用 UTF-8</param>
    /// <returns>Base64 字符串形式的加密载荷</returns>
    /// <exception cref="ArgumentException">当密钥或 IV 长度无效时抛出</exception>
    public static string Encrypt(
        string input,
        string key,
        byte[]? iv = null,
        CipherMode mode = CipherMode.CBC,
        PaddingMode padding = PaddingMode.PKCS7,
        bool isKeyBase64 = false,
        Encoding? encoding = null)
    {
        return SymmetricCryptographyCore.EncryptString(
            input,
            key,
            iv,
            mode,
            padding,
            isKeyBase64,
            encoding,
            AesAlgorithm);
    }

    /// <summary>加密字节并返回密文字节</summary>
    /// <param name="bytes">要加密的明文字节</param>
    /// <param name="key">加密密钥字节</param>
    /// <param name="iv">初始化向量；对非 ECB 模式传入 <see langword="null"/> 时会前置生成的 IV</param>
    /// <param name="mode">密码模式</param>
    /// <param name="padding">填充模式</param>
    /// <returns>加密后的字节，生成 IV 时包含前置 IV</returns>
    /// <exception cref="ArgumentException">当密钥或 IV 长度无效时抛出</exception>
    public static byte[] Encrypt(
        byte[] bytes,
        byte[] key,
        byte[]? iv = null,
        CipherMode mode = CipherMode.CBC,
        PaddingMode padding = PaddingMode.PKCS7)
    {
        return SymmetricCryptographyCore.EncryptBytes(bytes, key, iv, mode, padding, AesAlgorithm);
    }

    /// <summary>解密 Base64 密文字符串</summary>
    /// <param name="input">要解密的 Base64 密文</param>
    /// <param name="key">文本形式的加密密钥；当 <paramref name="isKeyBase64"/> 为 <see langword="true"/> 时表示 Base64 密钥字节</param>
    /// <param name="iv">初始化向量；对非 ECB 模式传入 <see langword="null"/> 时会读取前置 IV</param>
    /// <param name="mode">密码模式</param>
    /// <param name="padding">填充模式</param>
    /// <param name="isKeyBase64"><paramref name="key"/> 是否包含 Base64 编码的密钥字节</param>
    /// <param name="encoding">文本编码；省略时使用 UTF-8</param>
    /// <returns>解密后的明文字符串</returns>
    /// <exception cref="ArgumentException">当密钥、IV 或加密载荷长度无效时抛出</exception>
    public static string Decrypt(
        string input,
        string key,
        byte[]? iv = null,
        CipherMode mode = CipherMode.CBC,
        PaddingMode padding = PaddingMode.PKCS7,
        bool isKeyBase64 = false,
        Encoding? encoding = null)
    {
        return SymmetricCryptographyCore.DecryptString(
            input,
            key,
            iv,
            mode,
            padding,
            isKeyBase64,
            encoding,
            AesAlgorithm);
    }

    /// <summary>解密密文字节</summary>
    /// <param name="bytes">密文字节，包含生成时前置的 IV</param>
    /// <param name="key">加密密钥字节</param>
    /// <param name="iv">初始化向量；对非 ECB 模式传入 <see langword="null"/> 时会读取前置 IV</param>
    /// <param name="mode">密码模式</param>
    /// <param name="padding">填充模式</param>
    /// <returns>解密后的明文字节</returns>
    /// <exception cref="ArgumentException">当密钥、IV 或加密载荷长度无效时抛出</exception>
    public static byte[] Decrypt(
        byte[] bytes,
        byte[] key,
        byte[]? iv = null,
        CipherMode mode = CipherMode.CBC,
        PaddingMode padding = PaddingMode.PKCS7)
    {
        return SymmetricCryptographyCore.DecryptBytes(bytes, key, iv, mode, padding, AesAlgorithm);
    }

    /// <summary>加密文件路径并释放已打开的文件流</summary>
    /// <param name="filePath">要加密的文件路径</param>
    /// <param name="key">加密密钥字节</param>
    /// <param name="iv">初始化向量；对非 ECB 模式传入 <see langword="null"/> 时会前置生成的 IV</param>
    /// <param name="mode">密码模式</param>
    /// <param name="padding">填充模式</param>
    /// <param name="cancellationToken">取消文件读取和加密操作的令牌</param>
    /// <returns>加密后的字节，生成 IV 时包含前置 IV</returns>
    /// <exception cref="ArgumentException">当密钥或 IV 长度无效时抛出</exception>
    public static Task<byte[]> EncryptFileAsync(
        string filePath,
        byte[] key,
        byte[]? iv = null,
        CipherMode mode = CipherMode.CBC,
        PaddingMode padding = PaddingMode.PKCS7,
        CancellationToken cancellationToken = default)
    {
        return SymmetricCryptographyCore.EncryptFileAsync(filePath, key, iv, mode, padding, AesAlgorithm, cancellationToken);
    }

    /// <summary>加密流且不释放调用方拥有的流</summary>
    /// <param name="stream">要从当前位置开始加密的流</param>
    /// <param name="key">加密密钥字节</param>
    /// <param name="iv">初始化向量；对非 ECB 模式传入 <see langword="null"/> 时会前置生成的 IV</param>
    /// <param name="mode">密码模式</param>
    /// <param name="padding">填充模式</param>
    /// <param name="cancellationToken">取消流读取和加密操作的令牌</param>
    /// <returns>加密后的字节，生成 IV 时包含前置 IV</returns>
    /// <exception cref="ArgumentException">当密钥或 IV 长度无效时抛出</exception>
    public static Task<byte[]> EncryptFileAsync(
        Stream stream,
        byte[] key,
        byte[]? iv = null,
        CipherMode mode = CipherMode.CBC,
        PaddingMode padding = PaddingMode.PKCS7,
        CancellationToken cancellationToken = default)
    {
        return SymmetricCryptographyCore.EncryptStreamAsync(stream, key, iv, mode, padding, AesAlgorithm, cancellationToken);
    }

    /// <summary>解密文件路径并释放已打开的文件流</summary>
    /// <param name="filePath">要解密的加密文件路径</param>
    /// <param name="key">加密密钥字节</param>
    /// <param name="iv">初始化向量；对非 ECB 模式传入 <see langword="null"/> 时会读取前置 IV</param>
    /// <param name="mode">密码模式</param>
    /// <param name="padding">填充模式</param>
    /// <param name="cancellationToken">取消文件读取和解密操作的令牌</param>
    /// <returns>解密后的明文字节</returns>
    /// <exception cref="ArgumentException">当密钥、IV 或加密载荷长度无效时抛出</exception>
    public static Task<byte[]> DecryptFileAsync(
        string filePath,
        byte[] key,
        byte[]? iv = null,
        CipherMode mode = CipherMode.CBC,
        PaddingMode padding = PaddingMode.PKCS7,
        CancellationToken cancellationToken = default)
    {
        return SymmetricCryptographyCore.DecryptFileAsync(filePath, key, iv, mode, padding, AesAlgorithm, cancellationToken);
    }

    /// <summary>解密流且不释放调用方拥有的流</summary>
    /// <param name="stream">要从当前位置开始解密的加密流</param>
    /// <param name="key">加密密钥字节</param>
    /// <param name="iv">初始化向量；对非 ECB 模式传入 <see langword="null"/> 时会读取前置 IV</param>
    /// <param name="mode">密码模式</param>
    /// <param name="padding">填充模式</param>
    /// <param name="cancellationToken">取消流读取和解密操作的令牌</param>
    /// <returns>解密后的明文字节</returns>
    /// <exception cref="ArgumentException">当密钥、IV 或加密载荷长度无效时抛出</exception>
    public static Task<byte[]> DecryptFileAsync(
        Stream stream,
        byte[] key,
        byte[]? iv = null,
        CipherMode mode = CipherMode.CBC,
        PaddingMode padding = PaddingMode.PKCS7,
        CancellationToken cancellationToken = default)
    {
        return SymmetricCryptographyCore.DecryptStreamAsync(stream, key, iv, mode, padding, AesAlgorithm, cancellationToken);
    }

    private static SymmetricAlgorithmProfile AesAlgorithm => new(
        Aes.Create,
        ValidKeyLengths: [16, 24, 32],
        IvLength: 16,
        KeyLengthMessage: "密钥长度必须为 16、24 或 32 字节。",
        IvLengthMessage: "IV 长度必须为 16 字节。");
}

internal readonly record struct SymmetricAlgorithmProfile(
    Func<SymmetricAlgorithm> Create,
    int[] ValidKeyLengths,
    int IvLength,
    string KeyLengthMessage,
    string IvLengthMessage);

internal static class SymmetricCryptographyCore
{
    private static readonly Encoding DefaultEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    public static string EncryptString(
        string input,
        string key,
        byte[]? iv,
        CipherMode mode,
        PaddingMode padding,
        bool isKeyBase64,
        Encoding? encoding,
        SymmetricAlgorithmProfile profile)
    {
        Check.NotNull(input);
        Check.NotNullOrWhiteSpace(key);

        var textEncoding = encoding ?? DefaultEncoding;
        var keyBytes = isKeyBase64 ? Convert.FromBase64String(key) : textEncoding.GetBytes(key);
        var encrypted = EncryptBytes(textEncoding.GetBytes(input), keyBytes, iv, mode, padding, profile);

        return Convert.ToBase64String(encrypted);
    }

    public static string DecryptString(
        string input,
        string key,
        byte[]? iv,
        CipherMode mode,
        PaddingMode padding,
        bool isKeyBase64,
        Encoding? encoding,
        SymmetricAlgorithmProfile profile)
    {
        Check.NotNullOrWhiteSpace(input);
        Check.NotNullOrWhiteSpace(key);

        var textEncoding = encoding ?? DefaultEncoding;
        var keyBytes = isKeyBase64 ? Convert.FromBase64String(key) : textEncoding.GetBytes(key);
        var decrypted = DecryptBytes(Convert.FromBase64String(input), keyBytes, iv, mode, padding, profile);

        return textEncoding.GetString(decrypted);
    }

    public static byte[] EncryptBytes(
        byte[] bytes,
        byte[] key,
        byte[]? iv,
        CipherMode mode,
        PaddingMode padding,
        SymmetricAlgorithmProfile profile)
    {
        Check.NotNull(bytes);

        using var algorithm = CreateAlgorithm(key, iv, mode, padding, profile);
        using var encryptor = algorithm.CreateEncryptor();

        var cipherBytes = encryptor.TransformFinalBlock(bytes, 0, bytes.Length);
        return ShouldPrefixIv(mode, iv)
            ? PrefixIv(algorithm.IV, cipherBytes)
            : cipherBytes;
    }

    public static byte[] DecryptBytes(
        byte[] bytes,
        byte[] key,
        byte[]? iv,
        CipherMode mode,
        PaddingMode padding,
        SymmetricAlgorithmProfile profile)
    {
        Check.NotNull(bytes);

        var cipherBytes = bytes;
        if (ShouldPrefixIv(mode, iv))
        {
            (iv, cipherBytes) = SplitPrefixedIv(bytes, profile.IvLength);
        }

        using var algorithm = CreateAlgorithm(key, iv, mode, padding, profile);
        using var decryptor = algorithm.CreateDecryptor();

        return decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
    }

    public static async Task<byte[]> EncryptFileAsync(
        string filePath,
        byte[] key,
        byte[]? iv,
        CipherMode mode,
        PaddingMode padding,
        SymmetricAlgorithmProfile profile,
        CancellationToken cancellationToken)
    {
        Check.NotNullOrWhiteSpace(filePath);

        await using var stream = new FileStream(
            filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 81920,
            FileOptions.Asynchronous | FileOptions.SequentialScan);

        return await EncryptStreamAsync(stream, key, iv, mode, padding, profile, cancellationToken);
    }

    public static async Task<byte[]> EncryptStreamAsync(
        Stream stream,
        byte[] key,
        byte[]? iv,
        CipherMode mode,
        PaddingMode padding,
        SymmetricAlgorithmProfile profile,
        CancellationToken cancellationToken)
    {
        Check.NotNull(stream);

        using var algorithm = CreateAlgorithm(key, iv, mode, padding, profile);
        await using var output = new MemoryStream();

        if (ShouldPrefixIv(mode, iv))
        {
            await output.WriteAsync(algorithm.IV.AsMemory(), cancellationToken);
        }

        using (var cryptoStream = new CryptoStream(output, algorithm.CreateEncryptor(), CryptoStreamMode.Write, leaveOpen: true))
        {
            await stream.CopyToAsync(cryptoStream, cancellationToken);
        }

        return output.ToArray();
    }

    public static async Task<byte[]> DecryptFileAsync(
        string filePath,
        byte[] key,
        byte[]? iv,
        CipherMode mode,
        PaddingMode padding,
        SymmetricAlgorithmProfile profile,
        CancellationToken cancellationToken)
    {
        Check.NotNullOrWhiteSpace(filePath);

        await using var stream = new FileStream(
            filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 81920,
            FileOptions.Asynchronous | FileOptions.SequentialScan);

        return await DecryptStreamAsync(stream, key, iv, mode, padding, profile, cancellationToken);
    }

    public static async Task<byte[]> DecryptStreamAsync(
        Stream stream,
        byte[] key,
        byte[]? iv,
        CipherMode mode,
        PaddingMode padding,
        SymmetricAlgorithmProfile profile,
        CancellationToken cancellationToken)
    {
        Check.NotNull(stream);

        if (ShouldPrefixIv(mode, iv))
        {
            iv = await ReadPrefixedIvAsync(stream, profile.IvLength, cancellationToken);
        }

        using var algorithm = CreateAlgorithm(key, iv, mode, padding, profile);
        await using var output = new MemoryStream();

        using (var cryptoStream = new CryptoStream(stream, algorithm.CreateDecryptor(), CryptoStreamMode.Read, leaveOpen: true))
        {
            await cryptoStream.CopyToAsync(output, cancellationToken);
        }

        return output.ToArray();
    }

    private static SymmetricAlgorithm CreateAlgorithm(
        byte[] key,
        byte[]? iv,
        CipherMode mode,
        PaddingMode padding,
        SymmetricAlgorithmProfile profile)
    {
        Check.NotNull(key);
        ValidateKeyLength(key, profile);
        ValidateIvLength(iv, mode, profile);

        var algorithm = profile.Create();
        algorithm.Key = key;
        algorithm.Mode = mode;
        algorithm.Padding = padding;

        if (mode != CipherMode.ECB && iv is not null)
        {
            algorithm.IV = iv;
        }

        return algorithm;
    }

    private static bool ShouldPrefixIv(CipherMode mode, byte[]? iv)
    {
        return mode != CipherMode.ECB && iv is null;
    }

    private static void ValidateKeyLength(byte[] key, SymmetricAlgorithmProfile profile)
    {
        if (!profile.ValidKeyLengths.Contains(key.Length))
        {
            throw new ArgumentException(profile.KeyLengthMessage, nameof(key));
        }
    }

    private static void ValidateIvLength(byte[]? iv, CipherMode mode, SymmetricAlgorithmProfile profile)
    {
        if (mode != CipherMode.ECB && iv is not null && iv.Length != profile.IvLength)
        {
            throw new ArgumentException(profile.IvLengthMessage, nameof(iv));
        }
    }

    private static byte[] PrefixIv(byte[] iv, byte[] cipherBytes)
    {
        var result = new byte[iv.Length + cipherBytes.Length];
        Buffer.BlockCopy(iv, 0, result, 0, iv.Length);
        Buffer.BlockCopy(cipherBytes, 0, result, iv.Length, cipherBytes.Length);

        return result;
    }

    private static (byte[] Iv, byte[] CipherBytes) SplitPrefixedIv(byte[] bytes, int ivLength)
    {
        if (bytes.Length < ivLength)
        {
            throw new ArgumentException("加密数据太短，无法提取 IV。", nameof(bytes));
        }

        var iv = bytes[..ivLength];
        var cipherBytes = bytes[ivLength..];

        return (iv, cipherBytes);
    }

    private static async Task<byte[]> ReadPrefixedIvAsync(
        Stream stream,
        int ivLength,
        CancellationToken cancellationToken)
    {
        var iv = new byte[ivLength];
        var offset = 0;

        while (offset < iv.Length)
        {
            var bytesRead = await stream.ReadAsync(iv.AsMemory(offset), cancellationToken);
            if (bytesRead == 0)
            {
                throw new ArgumentException("加密流太短，无法提取 IV。", nameof(stream));
            }

            offset += bytesRead;
        }

        return iv;
    }
}
