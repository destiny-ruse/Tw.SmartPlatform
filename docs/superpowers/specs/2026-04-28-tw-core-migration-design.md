# Tw.Core Migration Design

## Status

Approved for design by the user on 2026-04-28.

## Context

`backend/dotnet/BuildingBlocks/src/Tw.Core` is the core building block for backend shared types, basic utilities, extension methods, and cross-module shared capabilities. It is currently a minimal project.

The source material is `D:\WorkSpaces\Nebulaxis_Old\src`. The migration target is a new project API surface, not a compatibility layer for the old `Nebulaxis` API.

## Goals

1. Analyze all code under `D:\WorkSpaces\Nebulaxis_Old\src`.
2. Migrate code that belongs in `Tw.Core`, excluding DI and AOP.
3. Rename namespaces, classes, and methods to match current `Tw.*` project conventions and business naming rules.
4. Implement migrated code with non-obsolete .NET 10 APIs.
5. Create `Tw.TestBase` before creating any unit or integration test project.
6. Use centrally managed NuGet package versions pinned to current stable versions.

## Non-Goals

1. Do not migrate dependency injection infrastructure.
2. Do not migrate AOP, dynamic proxy, interceptor, Autofac, Castle, or AspectCore infrastructure.
3. Do not preserve old `Nebulaxis` names or provide compatibility wrappers.
4. Do not mark cryptography APIs as legacy or obsolete just because older algorithms may be used by future business requirements.
5. Do not migrate application, domain, data, messaging, infrastructure, or web-layer code into `Tw.Core`.

## Standards

The design follows these repository standards:

1. `rules.naming-dotnet#rules`, version `1.1.0`, `docs/standards/rules/naming-dotnet.md`: namespaces, types, members, async methods, and tests must use clear .NET naming.
2. `rules.comments-dotnet#rules`, version `1.1.0`, `docs/standards/rules/comments-dotnet.md`: public APIs need XML comments that explain purpose and key contracts.
3. `rules.repo-layout#rules`, version `1.1.0`, `docs/standards/rules/repo-layout.md`: source and test directories must have clear boundaries and test paths must map to tested source.
4. `rules.test-strategy#rules`, version `1.1.0`, `docs/standards/rules/test-strategy.md`: verification must declare test levels and uncovered risks.
5. `rules.test-data-mock#rules`, version `1.1.0`, `docs/standards/rules/test-data-mock.md`: test data must be deterministic and not use real secrets or PII.
6. `rules.test-coverage#rules`, version `1.1.0`, `docs/standards/rules/test-coverage.md`: new behavior must cover normal paths, failure paths, and boundaries.
7. `rules.dependency-policy#rules`, version `1.1.0`, `docs/standards/rules/dependency-policy.md`: dependencies must be trusted, pinned, and verified.
8. `processes.dependency-onboarding#rules`, version `1.0.0`, `docs/standards/processes/dependency-onboarding.md`: dependency decisions must be traceable.

## Knowledge Alignment

The target module is `backend.dotnet.building-blocks.core`, declared in `docs/knowledge/graph/modules/backend.dotnet.building-blocks.core.yaml`.

The module summary says `Tw.Core` carries backend common core types, basic utilities, extension methods, and cross-module shared capabilities. This migration stays inside that boundary.

## Migration Scope

### Included From Nebulaxis.Core

The following categories should be migrated into `Tw.Core` after renaming:

1. Guard and argument validation helpers.
2. Disposable action helpers and null disposable instances.
3. Named object, named value, named action, and named type selector primitives.
4. Type list collections.
5. Extension methods for strings, collections, dictionaries, enumerable sequences, streams, dates, numbers, exceptions, GUIDs, objects, and types.
6. Reflection helpers and reflection cache.
7. Configuration section marker attribute and configurable options marker.
8. Current user abstraction.
9. Clock abstraction.
10. Core diagnostics and exception primitives.
11. Secure random generation utilities.

### Included From Nebulaxis.Security

All cryptography capability should be migrated into `Tw.Core` under a clear security namespace:

1. AES encryption and decryption.
2. DES encryption and decryption.
3. TripleDES encryption and decryption.
4. RSA key generation, encryption, decryption, signing, and verification.
5. PBKDF2 key derivation and password hashing.
6. MD5 hashing.
7. SHA1 hashing.
8. SHA2 hashing: SHA256, SHA384, SHA512.
9. SHA3 hashing: SHA3-256, SHA3-384, SHA3-512.
10. HMAC hashing for MD5, SHA1, SHA2, and SHA3 variants.

### Excluded

The following code must not be migrated into `Tw.Core`:

1. `Core/Nebulaxis.Core/DependencyInjection/**`
2. `Core/Nebulaxis.DependencyInjection/**`
3. `Core/Nebulaxis.Core/DynamicProxy/**`
4. `Core/Nebulaxis.DynamicProxy.Castle/**`
5. `Core/Nebulaxis.Autofac/**`
6. `Core/Nebulaxis.Autofac.Castle/**`
7. `Core/Nebulaxis.Core/Attributes/InterceptAttribute.cs`
8. `Core/Nebulaxis.Core/Attributes/LogAttribute.cs`
9. `Core/Nebulaxis.Core/Interceptors/LogInterceptor.cs`
10. Application, Domain, Data, Messaging, Infrastructure, Web, and provider-specific projects.

If an included file depends on excluded DI or AOP types, remove or redesign that dependency instead of pulling the excluded capability into `Tw.Core`.

## Target Structure

The target project should use this structure:

```text
backend/dotnet/BuildingBlocks/src/Tw.Core/
  Check.cs
  Context/
  Configuration/
  Collections/
  Diagnostics/
  Exceptions/
  Extensions/
  Reflection/
  Security/
    Cryptography/
  Timing/
  Utilities/
```

Test projects should use:

```text
backend/dotnet/BuildingBlocks/tests/
  Tw.TestBase/
  Tw.Core.Tests/
```

`Tw.TestBase` must be created before `Tw.Core.Tests`.

## Naming Design

Namespaces must use `Tw.Core.*` and align with folders.

Primary renames:

| Old name | New name |
| --- | --- |
| `Nebulaxis` | `Tw.Core` |
| `Nebulaxis.Extensions` | `Tw.Core.Extensions` |
| `Nebulaxis.Reflection` | `Tw.Core.Reflection` |
| `Nebulaxis.Configuration` | `Tw.Core.Configuration` |
| `Nebulaxis.Context` | `Tw.Core.Context` |
| `Nebulaxis.Diagnostics` | `Tw.Core.Diagnostics` |
| `Nebulaxis.Security.Cryptography` | `Tw.Core.Security.Cryptography` |
| `RandomHelper` | `SecureRandomGenerator` |
| `NameValue<T>` | `NamedValue<T>` |
| `NameValue` | `NamedValue` |
| `NebulaxisException` | `TwException` |
| `ConfigurationException` | `TwConfigurationException` |
| `CoreException` | `TwCoreException` |
| `AESEncryption` | `AesCryptography` |
| `DESEncryption` | `DesCryptography` |
| `TripleDESEncryption` | `TripleDesCryptography` |
| `RSAEncryption` | `RsaCryptography` |
| `PBKDF2Encryption` | `Pbkdf2PasswordHasher` |
| `MD5Encryption` | `Md5Hasher` |
| `SHA1Encryption` | `Sha1Hasher` |
| `SHA256Encryption` | `Sha256Hasher` |
| `SHA384Encryption` | `Sha384Hasher` |
| `SHA512Encryption` | `Sha512Hasher` |
| `SHA3_256Encryption` | `Sha3_256Hasher` |
| `SHA3_384Encryption` | `Sha3_384Hasher` |
| `SHA3_512Encryption` | `Sha3_512Hasher` |
| `HMACSHA256Encryption` | `HmacSha256Hasher` |

Hashing methods must use hashing terminology:

| Old method intent | New method name |
| --- | --- |
| Compute string hash | `ComputeHash` |
| Compute byte hash | `ComputeHash` |
| Compute file hash | `ComputeFileHashAsync` |
| Compare hash | `VerifyHash` |

Encryption and decryption methods should keep `Encrypt`, `Decrypt`, `EncryptFileAsync`, and `DecryptFileAsync` where they actually perform reversible encryption.

## Implementation Rules

1. Do not copy old code mechanically. Port behavior into the new namespace and naming model.
2. Use file-scoped namespaces.
3. Keep nullable annotations enabled and satisfy nullable warnings.
4. Do not introduce obsolete APIs or suppress obsolete warnings.
5. Do not introduce DI abstractions, service registration APIs, or AOP abstractions into `Tw.Core`.
6. Prefer .NET BCL APIs over new runtime dependencies.
7. Keep public APIs cohesive and behavior-focused.
8. Use XML documentation for public types and public members.
9. Keep implementation comments only for security, compatibility, performance, or migration trade-offs.

## Cryptography Implementation Rules

1. Use `RandomNumberGenerator` APIs for random bytes and random integers.
2. Use `CryptographicOperations.FixedTimeEquals` for hash verification.
3. Use static hash APIs such as `MD5.HashData`, `SHA1.HashData`, `SHA256.HashData`, and matching async stream APIs where available.
4. Use `Rfc2898DeriveBytes.Pbkdf2` for PBKDF2 derivation.
5. Use `RSA.Create()` with current PEM, DER, and parameter import/export APIs.
6. Use `Aes.Create()`, `DES.Create()`, and `TripleDES.Create()` only if the current SDK does not report them as obsolete. If the compiler reports obsolete diagnostics, replace the implementation path with a non-obsolete API instead of suppressing diagnostics.
7. Public hashing APIs should not be named `Encrypt`.
8. Test vectors must be deterministic and contain no real secrets.

## Project And Package Design

`Tw.Core` should remain dependency-light. Runtime dependencies should only be added if the BCL cannot provide a required capability.

Test package versions should be added to `backend/dotnet/Build/Packages.Tests.props` through central package management. Candidate test dependencies:

1. `xunit`
2. `xunit.runner.visualstudio`
3. `Microsoft.NET.Test.Sdk`
4. `FluentAssertions`
5. `coverlet.collector` if the existing central configuration requires it

All versions must be resolved to current stable package versions during implementation and locked through `packages.lock.json`.

## TestBase Design

`Tw.TestBase` is a shared test support project, not a production dependency.

Initial responsibilities:

1. Temporary directory helpers.
2. Deterministic byte, string, and stream fixtures.
3. Shared cryptography test vectors.
4. Assertion helpers only when they remove repeated low-level setup noise.

`Tw.TestBase` must not contain production behavior or business logic.

## Tw.Core.Tests Design

`Tw.Core.Tests` should reference `Tw.Core` and `Tw.TestBase`.

Initial test coverage should include:

1. Guard validation success and failure paths.
2. Core string, collection, dictionary, stream, numeric, date, type, GUID, and object extension behavior.
3. Disposable helpers invoking actions exactly once where applicable.
4. Reflection helpers for attributes, interfaces, constructors, async method detection, and type filtering.
5. Current user and clock abstractions where behavior exists.
6. Secure random generator boundary validation.
7. Hashers with deterministic known vectors.
8. HMAC hashers with deterministic known vectors.
9. AES, DES, TripleDES round-trip behavior, invalid key or IV boundaries, and file/stream flows.
10. RSA key generation, round-trip encryption, signing, and verification.
11. PBKDF2 derivation and password verification.

## Verification Strategy

Implementation must run fresh verification before claiming completion:

1. `dotnet restore Tw.SmartPlatform.slnx`
2. `dotnet build Tw.SmartPlatform.slnx --no-restore`
3. Targeted `dotnet test` for `Tw.Core.Tests`
4. Full relevant backend `dotnet test` if additional test projects are added or solution wiring changes broadly

Failures must be fixed or documented with exact remaining risk.

## Risks And Mitigations

| Risk | Mitigation |
| --- | --- |
| Old code depends on DI or AOP helpers | Redesign the affected API to remove that dependency or exclude the affected member. |
| Old cryptography method names misrepresent hashing as encryption | Rename hashing APIs to `ComputeHash` and `VerifyHash`. |
| DES or TripleDES APIs may trigger obsolete diagnostics | Use non-obsolete implementation paths or stop and report the blocker before suppressing warnings. |
| Broad extension method migration may create ambiguous APIs | Keep method names clear, avoid duplicate overloads that do not add value, and validate with build and tests. |
| Test packages may be missing from central package management | Add current stable versions only in `Build/Packages.Tests.props` and restore lock files. |

## Acceptance Criteria

1. `Tw.Core` contains all approved non-DI, non-AOP migrated capabilities.
2. All namespaces, class names, and method names use `Tw.*` conventions and current business semantics.
3. No migrated production code uses obsolete .NET APIs or warning suppression for obsolete APIs.
4. `Tw.TestBase` exists before `Tw.Core.Tests`.
5. Tests cover migrated public behavior, including cryptography test vectors and error boundaries.
6. NuGet package versions are centrally managed and pinned to current stable versions.
7. Restore, build, and relevant tests pass before completion is claimed.
