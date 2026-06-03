using System.Security.Cryptography;
using System.Text;

namespace Tw.Core.Security.Cryptography;

/// <summary>
/// 提供用于字符串、字节、文件和流的 DES 加密与解密辅助方法
/// </summary>
public static class DesCryptography
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
        return SymmetricCryptographyCore.EncryptString(input, key, iv, mode, padding, isKeyBase64, encoding, DesAlgorithm);
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
        return SymmetricCryptographyCore.EncryptBytes(bytes, key, iv, mode, padding, DesAlgorithm);
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
        return SymmetricCryptographyCore.DecryptString(input, key, iv, mode, padding, isKeyBase64, encoding, DesAlgorithm);
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
        return SymmetricCryptographyCore.DecryptBytes(bytes, key, iv, mode, padding, DesAlgorithm);
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
        return SymmetricCryptographyCore.EncryptFileAsync(filePath, key, iv, mode, padding, DesAlgorithm, cancellationToken);
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
        return SymmetricCryptographyCore.EncryptStreamAsync(stream, key, iv, mode, padding, DesAlgorithm, cancellationToken);
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
        return SymmetricCryptographyCore.DecryptFileAsync(filePath, key, iv, mode, padding, DesAlgorithm, cancellationToken);
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
        return SymmetricCryptographyCore.DecryptStreamAsync(stream, key, iv, mode, padding, DesAlgorithm, cancellationToken);
    }

    private static SymmetricAlgorithmProfile DesAlgorithm => new(
        DES.Create,
        ValidKeyLengths: [8],
        IvLength: 8,
        KeyLengthMessage: "密钥长度必须为 8 字节。",
        IvLengthMessage: "IV 长度必须为 8 字节。");
}
