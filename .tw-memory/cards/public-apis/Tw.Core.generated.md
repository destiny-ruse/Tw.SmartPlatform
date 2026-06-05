# Public API: Tw.Core

标识：Tw.Core / backend/dotnet/BuildingBlocks/src/Tw.Core

公开能力边界：
- Tw.Context
- Tw.Check
- Tw.Collections
- Tw.Core.Configuration
- Tw.Core.Primitives
- Tw.Core.Reflection
- Tw.Core.Security.Cryptography
- Tw.Exceptions
- Tw.Extensions
- Tw.Utilities

实现公开命名空间：
- Tw
- Tw.Collections
- Tw.Context
- Tw.Core.Configuration
- Tw.Core.Primitives
- Tw.Core.Reflection
- Tw.Core.Security.Cryptography
- Tw.Exceptions
- Tw.Extensions
- Tw.Utilities

公开类型：
- static class Check - Tw (backend/dotnet/BuildingBlocks/src/Tw.Core/Check.cs:8)
- interface ITypeList - Tw.Collections (backend/dotnet/BuildingBlocks/src/Tw.Core/Collections/ITypeList.cs:6)
- interface ITypeList - Tw.Collections (backend/dotnet/BuildingBlocks/src/Tw.Core/Collections/ITypeList.cs:12)
- class TypeList - Tw.Collections (backend/dotnet/BuildingBlocks/src/Tw.Core/Collections/TypeList.cs:8)
- class TypeList - Tw.Collections (backend/dotnet/BuildingBlocks/src/Tw.Core/Collections/TypeList.cs:14)
- sealed class AsyncLocalCancellationTokenScopeProvider - Tw.Context (backend/dotnet/BuildingBlocks/src/Tw.Core/Context/AsyncLocalCancellationTokenScopeProvider.cs:12)
- sealed class CancellationTokenOverride - Tw.Context (backend/dotnet/BuildingBlocks/src/Tw.Core/Context/CancellationTokenOverride.cs:6)
- abstract class CancellationTokenProviderBase - Tw.Context (backend/dotnet/BuildingBlocks/src/Tw.Core/Context/CancellationTokenProviderBase.cs:7)
- static class CancellationTokenProviderExtensions - Tw.Context (backend/dotnet/BuildingBlocks/src/Tw.Core/Context/CancellationTokenProviderExtensions.cs:6)
- static class CancellationTokenServiceCollectionExtensions - Tw.Context (backend/dotnet/BuildingBlocks/src/Tw.Core/Context/CancellationTokenServiceCollectionExtensions.cs:10)
- interface ICancellationTokenProvider - Tw.Context (backend/dotnet/BuildingBlocks/src/Tw.Core/Context/ICancellationTokenProvider.cs:6)
- sealed class NullCancellationTokenProvider - Tw.Context (backend/dotnet/BuildingBlocks/src/Tw.Core/Context/NullCancellationTokenProvider.cs:7)
- interface IConfigurableOptions - Tw.Core.Configuration (backend/dotnet/BuildingBlocks/src/Tw.Core/Configuration/IConfigurableOptions.cs:6)
- class NamedAction - Tw.Core.Primitives (backend/dotnet/BuildingBlocks/src/Tw.Core/Primitives/NamedAction.cs:11)
- class NamedActionList - Tw.Core.Primitives (backend/dotnet/BuildingBlocks/src/Tw.Core/Primitives/NamedActionList.cs:7)
- class NamedObject - Tw.Core.Primitives (backend/dotnet/BuildingBlocks/src/Tw.Core/Primitives/NamedObject.cs:9)
- class NamedObjectList - Tw.Core.Primitives (backend/dotnet/BuildingBlocks/src/Tw.Core/Primitives/NamedObjectList.cs:7)
- class NamedTypeSelector - Tw.Core.Primitives (backend/dotnet/BuildingBlocks/src/Tw.Core/Primitives/NamedTypeSelector.cs:10)
- static class NamedTypeSelectorListExtensions - Tw.Core.Primitives (backend/dotnet/BuildingBlocks/src/Tw.Core/Primitives/NamedTypeSelectorListExtensions.cs:6)
- class NamedValue - Tw.Core.Primitives (backend/dotnet/BuildingBlocks/src/Tw.Core/Primitives/NamedValue.cs:11)
- class NamedValue - Tw.Core.Primitives (backend/dotnet/BuildingBlocks/src/Tw.Core/Primitives/NamedValue.cs:22)
- sealed record CacheStatistics - Tw.Core.Reflection (backend/dotnet/BuildingBlocks/src/Tw.Core/Reflection/ReflectionCache.cs:224)
- interface ITypeFinder - Tw.Core.Reflection (backend/dotnet/BuildingBlocks/src/Tw.Core/Reflection/ITypeFinder.cs:8)
- static class ReflectionCache - Tw.Core.Reflection (backend/dotnet/BuildingBlocks/src/Tw.Core/Reflection/ReflectionCache.cs:9)
- sealed class TypeFinder - Tw.Core.Reflection (backend/dotnet/BuildingBlocks/src/Tw.Core/Reflection/TypeFinder.cs:8)
- static class TypeFinderExtensions - Tw.Core.Reflection (backend/dotnet/BuildingBlocks/src/Tw.Core/Reflection/TypeFinderExtensions.cs:6)
- static class AesCryptography - Tw.Core.Security.Cryptography (backend/dotnet/BuildingBlocks/src/Tw.Core/Security/Cryptography/AesCryptography.cs:10)
- static class DesCryptography - Tw.Core.Security.Cryptography (backend/dotnet/BuildingBlocks/src/Tw.Core/Security/Cryptography/DesCryptography.cs:9)
- static class HmacMd5Hasher - Tw.Core.Security.Cryptography (backend/dotnet/BuildingBlocks/src/Tw.Core/Security/Cryptography/HmacMd5Hasher.cs:10)
- static class HmacSha1Hasher - Tw.Core.Security.Cryptography (backend/dotnet/BuildingBlocks/src/Tw.Core/Security/Cryptography/HmacSha1Hasher.cs:9)
- static class HmacSha256Hasher - Tw.Core.Security.Cryptography (backend/dotnet/BuildingBlocks/src/Tw.Core/Security/Cryptography/HmacSha256Hasher.cs:9)
- static class HmacSha3256Hasher - Tw.Core.Security.Cryptography (backend/dotnet/BuildingBlocks/src/Tw.Core/Security/Cryptography/HmacSha3256Hasher.cs:8)
- static class HmacSha3384Hasher - Tw.Core.Security.Cryptography (backend/dotnet/BuildingBlocks/src/Tw.Core/Security/Cryptography/HmacSha3384Hasher.cs:8)
- static class HmacSha3512Hasher - Tw.Core.Security.Cryptography (backend/dotnet/BuildingBlocks/src/Tw.Core/Security/Cryptography/HmacSha3512Hasher.cs:8)
- static class HmacSha384Hasher - Tw.Core.Security.Cryptography (backend/dotnet/BuildingBlocks/src/Tw.Core/Security/Cryptography/HmacSha384Hasher.cs:9)
- static class HmacSha512Hasher - Tw.Core.Security.Cryptography (backend/dotnet/BuildingBlocks/src/Tw.Core/Security/Cryptography/HmacSha512Hasher.cs:9)
- static class Md5Hasher - Tw.Core.Security.Cryptography (backend/dotnet/BuildingBlocks/src/Tw.Core/Security/Cryptography/Md5Hasher.cs:9)
- static class Pbkdf2PasswordHasher - Tw.Core.Security.Cryptography (backend/dotnet/BuildingBlocks/src/Tw.Core/Security/Cryptography/Pbkdf2PasswordHasher.cs:14)
- static class RsaCryptography - Tw.Core.Security.Cryptography (backend/dotnet/BuildingBlocks/src/Tw.Core/Security/Cryptography/RsaCryptography.cs:13)
- readonly record struct RsaDerKeyPair - Tw.Core.Security.Cryptography (backend/dotnet/BuildingBlocks/src/Tw.Core/Security/Cryptography/RsaDerKeyPair.cs:8)
- readonly record struct RsaKeyPair - Tw.Core.Security.Cryptography (backend/dotnet/BuildingBlocks/src/Tw.Core/Security/Cryptography/RsaKeyPair.cs:8)
- static class Sha1Hasher - Tw.Core.Security.Cryptography (backend/dotnet/BuildingBlocks/src/Tw.Core/Security/Cryptography/Sha1Hasher.cs:9)
- static class Sha256Hasher - Tw.Core.Security.Cryptography (backend/dotnet/BuildingBlocks/src/Tw.Core/Security/Cryptography/Sha256Hasher.cs:9)
- static class Sha3256Hasher - Tw.Core.Security.Cryptography (backend/dotnet/BuildingBlocks/src/Tw.Core/Security/Cryptography/Sha3256Hasher.cs:8)
- static class Sha3384Hasher - Tw.Core.Security.Cryptography (backend/dotnet/BuildingBlocks/src/Tw.Core/Security/Cryptography/Sha3384Hasher.cs:8)
- static class Sha3512Hasher - Tw.Core.Security.Cryptography (backend/dotnet/BuildingBlocks/src/Tw.Core/Security/Cryptography/Sha3512Hasher.cs:8)
- static class Sha384Hasher - Tw.Core.Security.Cryptography (backend/dotnet/BuildingBlocks/src/Tw.Core/Security/Cryptography/Sha384Hasher.cs:9)
- static class Sha512Hasher - Tw.Core.Security.Cryptography (backend/dotnet/BuildingBlocks/src/Tw.Core/Security/Cryptography/Sha512Hasher.cs:9)
- static class StringCryptographyExtensions - Tw.Core.Security.Cryptography (backend/dotnet/BuildingBlocks/src/Tw.Core/Security/Cryptography/StringCryptographyExtensions.cs:9)
- static class TripleDesCryptography - Tw.Core.Security.Cryptography (backend/dotnet/BuildingBlocks/src/Tw.Core/Security/Cryptography/TripleDesCryptography.cs:9)
- class TwConfigurationException - Tw.Exceptions (backend/dotnet/BuildingBlocks/src/Tw.Core/Exceptions/TwConfigurationException.cs:6)
- class TwException - Tw.Exceptions (backend/dotnet/BuildingBlocks/src/Tw.Core/Exceptions/TwException.cs:6)
- static class ByteArrayExtensions - Tw.Extensions (backend/dotnet/BuildingBlocks/src/Tw.Core/Extensions/ByteArrayExtensions.cs:6)
- static class CollectionExtensions - Tw.Extensions (backend/dotnet/BuildingBlocks/src/Tw.Core/Extensions/CollectionExtensions.cs:4)
- static class ComparableExtensions - Tw.Extensions (backend/dotnet/BuildingBlocks/src/Tw.Core/Extensions/ComparableExtensions.cs:4)
- static class DateTimeExtensions - Tw.Extensions (backend/dotnet/BuildingBlocks/src/Tw.Core/Extensions/DateTimeExtensions.cs:4)
- static class DictionaryExtensions - Tw.Extensions (backend/dotnet/BuildingBlocks/src/Tw.Core/Extensions/DictionaryExtensions.cs:7)
- static class EnumerableExtensions - Tw.Extensions (backend/dotnet/BuildingBlocks/src/Tw.Core/Extensions/EnumerableExtensions.cs:4)
- static class ExceptionExtensions - Tw.Extensions (backend/dotnet/BuildingBlocks/src/Tw.Core/Extensions/ExceptionExtensions.cs:6)
- static class GuidExtensions - Tw.Extensions (backend/dotnet/BuildingBlocks/src/Tw.Core/Extensions/GuidExtensions.cs:4)
- static class ListExtensions - Tw.Extensions (backend/dotnet/BuildingBlocks/src/Tw.Core/Extensions/ListExtensions.cs:4)
- static class NumberExtensions - Tw.Extensions (backend/dotnet/BuildingBlocks/src/Tw.Core/Extensions/NumberExtensions.cs:6)
- static class ObjectExtensions - Tw.Extensions (backend/dotnet/BuildingBlocks/src/Tw.Core/Extensions/ObjectExtensions.cs:4)
- static class StreamExtensions - Tw.Extensions (backend/dotnet/BuildingBlocks/src/Tw.Core/Extensions/StreamExtensions.cs:6)
- static class StringExtensions - Tw.Extensions (backend/dotnet/BuildingBlocks/src/Tw.Core/Extensions/StringExtensions.cs:7)
- static class TypeExtensions - Tw.Extensions (backend/dotnet/BuildingBlocks/src/Tw.Core/Extensions/TypeExtensions.cs:4)
- sealed class AsyncDisposeFunc - Tw.Utilities (backend/dotnet/BuildingBlocks/src/Tw.Core/Utilities/AsyncDisposeFunc.cs:6)
- sealed class DisposeAction - Tw.Utilities (backend/dotnet/BuildingBlocks/src/Tw.Core/Utilities/DisposeAction.cs:6)
- sealed class NullAsyncDisposable - Tw.Utilities (backend/dotnet/BuildingBlocks/src/Tw.Core/Utilities/NullAsyncDisposable.cs:6)
- sealed class NullDisposable - Tw.Utilities (backend/dotnet/BuildingBlocks/src/Tw.Core/Utilities/NullDisposable.cs:6)
- static class SecureRandomGenerator - Tw.Utilities (backend/dotnet/BuildingBlocks/src/Tw.Core/Utilities/SecureRandomGenerator.cs:8)

DI 注册入口：
- Tw.Context.CancellationTokenServiceCollectionExtensions.AddCancellationTokenProvider

使用文档：
- docs/shared-packages/dotnet/Tw.Core/context/cancellation-token-provider.md
- docs/shared-packages/dotnet/Tw.Core/README.md

契约关联：
- none

消费提示：
- 公开能力来自 package-charter.yaml
- 实现公开 API 来自当前包源码
- 使用文档来自 docs/shared-packages

source_refs:
- charter:package-charter:Tw.Core
