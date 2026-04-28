namespace Tw.Core.Security.Cryptography;

/// <summary>
/// Represents an RSA public/private key pair encoded as DER bytes.
/// </summary>
/// <param name="PublicKeyDer">The public key in PKCS#1 DER format.</param>
/// <param name="PrivateKeyDer">The private key in PKCS#1 DER format.</param>
public readonly record struct RsaDerKeyPair(byte[] PublicKeyDer, byte[] PrivateKeyDer);
