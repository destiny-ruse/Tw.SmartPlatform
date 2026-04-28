namespace Tw.Core.Security.Cryptography;

/// <summary>
/// Represents an RSA public/private key pair encoded as PEM.
/// </summary>
/// <param name="PublicKeyPem">The public key in PKCS#1 PEM format.</param>
/// <param name="PrivateKeyPem">The private key in PKCS#1 PEM format.</param>
public readonly record struct RsaKeyPair(string PublicKeyPem, string PrivateKeyPem);
