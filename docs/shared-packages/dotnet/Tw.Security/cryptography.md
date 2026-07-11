# 密码学使用指南

## 能力定位

`Tw.Security.Cryptography` 提供哈希、HMAC、PBKDF2、AES、DES、TripleDES 和 RSA 的静态工具。调用方无需注册 DI 服务。

## 哈希与 HMAC

```csharp
using Tw.Security.Cryptography;

var sha256 = Sha256Hasher.ComputeHash("payload");
var hmac = HmacSha256Hasher.ComputeHash("key", "payload");
```

哈希结果默认使用小写十六进制。需要验证时使用对应类型的 `VerifyHash` 方法，避免自行比较结果。

## 密码哈希

```csharp
using Tw.Security.Cryptography;

var hashedPassword = Pbkdf2PasswordHasher.HashPassword("password");
var isMatch = Pbkdf2PasswordHasher.VerifyPassword("password", hashedPassword);
```

`HashPassword` 返回 `PBKDF2$HashAlgorithm$Iterations$KeyLength$SaltBase64$HashBase64` 格式。验证始终读取该格式中保存的算法、迭代次数、密钥长度和盐值。

## 密码学安全随机

```csharp
using Tw.Security.Cryptography;

var bytes = SecureRandomGenerator.GetBytes(32);
var password = SecureRandomGenerator.GetStrongPassword();
```

`SecureRandomGenerator` 基于密码学安全随机源生成范围内的数值、字节、字符串、随机集合顺序和强密码。不得将其替换为面向非安全场景的伪随机源。

## 对称加密

```csharp
using System.Security.Cryptography;
using Tw.Security.Cryptography;

var key = Convert.FromBase64String(configuration["Encryption:Key"]!);
var ciphertext = AesCryptography.Encrypt("payload", Convert.ToBase64String(key), isKeyBase64: true);
var plaintext = AesCryptography.Decrypt(ciphertext, Convert.ToBase64String(key), isKeyBase64: true);
```

CBC 等非 ECB 模式在未显式传入 IV 时会将生成的 IV 前置到密文载荷；解密时会读取该前缀。显式传入 IV 时，调用方必须以相同 IV 解密。密钥必须来自受控密钥来源，不得写入代码、日志、测试夹具或文档示例。

## RSA

`RsaCryptography` 支持 PEM 和 DER 密钥对的生成、加解密与签名验证。调用方应根据用途分别使用公钥加密或验证、私钥解密或签名，并通过受控密钥管理系统保存私钥。

## 注意事项

- 本包不负责密钥轮换、密钥分发或密钥存储。
- 新增业务代码优先使用 AES、SHA-2/SHA-3、HMAC 和 PBKDF2；DES、TripleDES 与 MD5 仅用于已有兼容场景。
- 不要将密码、原始密钥、私钥或未脱敏密文上下文写入日志。
