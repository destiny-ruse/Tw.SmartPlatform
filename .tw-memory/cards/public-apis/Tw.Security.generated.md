# Public API: Tw.Security

标识：Tw.Security / backend/dotnet/BuildingBlocks/src/Foundation/Tw.Security

公开能力边界：
- Tw.Security
- Tw.Security.Cryptography
- Tw.Security.DataMasking

实现公开命名空间：
- Tw.Security.Cryptography
- Tw.Security.DataMasking

公开类型：
- static class AesCryptography - Tw.Security.Cryptography (backend/dotnet/BuildingBlocks/src/Foundation/Tw.Security/Cryptography/AesCryptography.cs:10)
- static class DesCryptography - Tw.Security.Cryptography (backend/dotnet/BuildingBlocks/src/Foundation/Tw.Security/Cryptography/DesCryptography.cs:9)
- static class HmacMd5Hasher - Tw.Security.Cryptography (backend/dotnet/BuildingBlocks/src/Foundation/Tw.Security/Cryptography/HmacMd5Hasher.cs:10)
- static class HmacSha1Hasher - Tw.Security.Cryptography (backend/dotnet/BuildingBlocks/src/Foundation/Tw.Security/Cryptography/HmacSha1Hasher.cs:9)
- static class HmacSha256Hasher - Tw.Security.Cryptography (backend/dotnet/BuildingBlocks/src/Foundation/Tw.Security/Cryptography/HmacSha256Hasher.cs:9)
- static class HmacSha3256Hasher - Tw.Security.Cryptography (backend/dotnet/BuildingBlocks/src/Foundation/Tw.Security/Cryptography/HmacSha3256Hasher.cs:8)
- static class HmacSha3384Hasher - Tw.Security.Cryptography (backend/dotnet/BuildingBlocks/src/Foundation/Tw.Security/Cryptography/HmacSha3384Hasher.cs:8)
- static class HmacSha3512Hasher - Tw.Security.Cryptography (backend/dotnet/BuildingBlocks/src/Foundation/Tw.Security/Cryptography/HmacSha3512Hasher.cs:8)
- static class HmacSha384Hasher - Tw.Security.Cryptography (backend/dotnet/BuildingBlocks/src/Foundation/Tw.Security/Cryptography/HmacSha384Hasher.cs:9)
- static class HmacSha512Hasher - Tw.Security.Cryptography (backend/dotnet/BuildingBlocks/src/Foundation/Tw.Security/Cryptography/HmacSha512Hasher.cs:9)
- static class Md5Hasher - Tw.Security.Cryptography (backend/dotnet/BuildingBlocks/src/Foundation/Tw.Security/Cryptography/Md5Hasher.cs:9)
- static class Pbkdf2PasswordHasher - Tw.Security.Cryptography (backend/dotnet/BuildingBlocks/src/Foundation/Tw.Security/Cryptography/Pbkdf2PasswordHasher.cs:14)
- static class RsaCryptography - Tw.Security.Cryptography (backend/dotnet/BuildingBlocks/src/Foundation/Tw.Security/Cryptography/RsaCryptography.cs:13)
- readonly record struct RsaDerKeyPair - Tw.Security.Cryptography (backend/dotnet/BuildingBlocks/src/Foundation/Tw.Security/Cryptography/RsaDerKeyPair.cs:8)
- readonly record struct RsaKeyPair - Tw.Security.Cryptography (backend/dotnet/BuildingBlocks/src/Foundation/Tw.Security/Cryptography/RsaKeyPair.cs:8)
- static class SecureRandomGenerator - Tw.Security.Cryptography (backend/dotnet/BuildingBlocks/src/Foundation/Tw.Security/Cryptography/SecureRandomGenerator.cs:10)
- static class Sha1Hasher - Tw.Security.Cryptography (backend/dotnet/BuildingBlocks/src/Foundation/Tw.Security/Cryptography/Sha1Hasher.cs:9)
- static class Sha256Hasher - Tw.Security.Cryptography (backend/dotnet/BuildingBlocks/src/Foundation/Tw.Security/Cryptography/Sha256Hasher.cs:9)
- static class Sha3256Hasher - Tw.Security.Cryptography (backend/dotnet/BuildingBlocks/src/Foundation/Tw.Security/Cryptography/Sha3256Hasher.cs:8)
- static class Sha3384Hasher - Tw.Security.Cryptography (backend/dotnet/BuildingBlocks/src/Foundation/Tw.Security/Cryptography/Sha3384Hasher.cs:8)
- static class Sha3512Hasher - Tw.Security.Cryptography (backend/dotnet/BuildingBlocks/src/Foundation/Tw.Security/Cryptography/Sha3512Hasher.cs:8)
- static class Sha384Hasher - Tw.Security.Cryptography (backend/dotnet/BuildingBlocks/src/Foundation/Tw.Security/Cryptography/Sha384Hasher.cs:9)
- static class Sha512Hasher - Tw.Security.Cryptography (backend/dotnet/BuildingBlocks/src/Foundation/Tw.Security/Cryptography/Sha512Hasher.cs:9)
- static class StringCryptographyExtensions - Tw.Security.Cryptography (backend/dotnet/BuildingBlocks/src/Foundation/Tw.Security/Cryptography/StringCryptographyExtensions.cs:9)
- static class TripleDesCryptography - Tw.Security.Cryptography (backend/dotnet/BuildingBlocks/src/Foundation/Tw.Security/Cryptography/TripleDesCryptography.cs:9)
- interface IDataMasker - Tw.Security.DataMasking (backend/dotnet/BuildingBlocks/src/Foundation/Tw.Security/DataMasking/IDataMasker.cs:6)
- interface IDataMaskingPolicyProvider - Tw.Security.DataMasking (backend/dotnet/BuildingBlocks/src/Foundation/Tw.Security/DataMasking/IDataMaskingPolicyProvider.cs:6)
- interface IDataMaskingRule - Tw.Security.DataMasking (backend/dotnet/BuildingBlocks/src/Foundation/Tw.Security/DataMasking/IDataMaskingRule.cs:6)
- interface ISensitiveValueDetector - Tw.Security.DataMasking (backend/dotnet/BuildingBlocks/src/Foundation/Tw.Security/DataMasking/ISensitiveValueDetector.cs:6)
- sealed class MaskedValueWriteBackException - Tw.Security.DataMasking (backend/dotnet/BuildingBlocks/src/Foundation/Tw.Security/DataMasking/MaskedValueWriteBackException.cs:6)
- sealed class SensitiveDataAttribute - Tw.Security.DataMasking (backend/dotnet/BuildingBlocks/src/Foundation/Tw.Security/DataMasking/SensitiveDataAttribute.cs:7)
- enum SensitiveDataKind - Tw.Security.DataMasking (backend/dotnet/BuildingBlocks/src/Foundation/Tw.Security/DataMasking/SensitiveDataKind.cs:6)

DI 注册入口：
- none

包参考文档：
- docs/shared-packages/dotnet/Tw.Security/cryptography.md
- docs/shared-packages/dotnet/Tw.Security/README.md

契约关联：
- none

消费提示：
- 公开能力来自 package-charter.yaml
- 实现公开 API 来自当前包源码
- 关联文档来自 docs/shared-packages

source_refs:
- charter:package-charter:Tw.Security
