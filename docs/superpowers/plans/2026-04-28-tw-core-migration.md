# Tw.Core Migration Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Migrate the approved non-DI, non-AOP `Nebulaxis.Core` and `Nebulaxis.Security` capabilities into `backend/dotnet/BuildingBlocks/src/Tw.Core` with new `Tw.*` names, .NET 10 APIs, central package versions, and focused tests.

**Architecture:** Build the test foundation first, then migrate behavior in cohesive slices: core primitives, extensions, reflection, random utilities, hashing, reversible encryption, RSA/PBKDF2, and string convenience APIs. `Tw.Core` stays dependency-light and uses BCL APIs; `Tw.TestBase` holds deterministic fixtures and vectors; `Tw.Core.Tests` verifies public behavior and boundaries.

**Tech Stack:** .NET 10 (`net10.0`), C# latest, xUnit, FluentAssertions, central NuGet package management, `Tw.SmartPlatform.slnx`.

---

## Standards

Implementation must cite and follow these standards from the approved spec:

- `rules.naming-dotnet#rules`, version `1.1.0`, `docs/standards/rules/naming-dotnet.md`
- `rules.comments-dotnet#rules`, version `1.1.0`, `docs/standards/rules/comments-dotnet.md`
- `rules.repo-layout#rules`, version `1.1.0`, `docs/standards/rules/repo-layout.md`
- `rules.test-strategy#rules`, version `1.1.0`, `docs/standards/rules/test-strategy.md`
- `rules.test-data-mock#rules`, version `1.1.0`, `docs/standards/rules/test-data-mock.md`
- `rules.test-coverage#rules`, version `1.1.0`, `docs/standards/rules/test-coverage.md`
- `rules.dependency-policy#rules`, version `1.1.0`, `docs/standards/rules/dependency-policy.md`
- `processes.dependency-onboarding#rules`, version `1.0.0`, `docs/standards/processes/dependency-onboarding.md`

## Source And Scope

Use these old source roots as behavior references:

- `D:\WorkSpaces\Nebulaxis_Old\src\Core\Nebulaxis.Core`
- `D:\WorkSpaces\Nebulaxis_Old\src\Core\Nebulaxis.Security\Cryptography`

Use these exclusions exactly:

- Do not migrate `Core/Nebulaxis.Core/DependencyInjection/**`.
- Do not migrate `Core/Nebulaxis.DependencyInjection/**`.
- Do not migrate `Core/Nebulaxis.Core/DynamicProxy/**`.
- Do not migrate `Core/Nebulaxis.DynamicProxy.Castle/**`.
- Do not migrate `Core/Nebulaxis.Autofac/**` or `Core/Nebulaxis.Autofac.Castle/**`.
- Do not migrate `Core/Nebulaxis.Core/Attributes/InterceptAttribute.cs`, `LogAttribute.cs`, or `Interceptors/LogInterceptor.cs`.
- Do not migrate `Core/Nebulaxis.Core/Diagnostics/ErrorDefinition.cs`.
- Do not migrate `Core/Nebulaxis.Security/Cryptography/ECCEncryption.cs`.
- Ignore the invalid Windows device path `D:\WorkSpaces\Nebulaxis_Old\src\Core\Nebulaxis.Core\nul`.

Design decisions:

- `TwException` is concrete, not abstract.
- `TwConfigurationException` derives from `TwException`.
- `ICurrentUser` excludes `TenantId`; multi-tenancy stays outside `Tw.Core`.
- `TypeFinderExtensions` must not reference DI marker interfaces or DI attributes. Replace the old DI registration helper with reflection-only helpers.
- `StringCryptographyExtensions` is new behavior-backed API, not a wrapper around the empty old `StringEncryptionExtensions`.
- Hashing APIs use `ComputeHash`, `ComputeFileHashAsync`, and `VerifyHash`.
- Reversible encryption APIs use `Encrypt`, `Decrypt`, `EncryptFileAsync`, and `DecryptFileAsync`.
- Hash verification uses `CryptographicOperations.FixedTimeEquals` after normalizing hex to bytes.
- Public APIs include XML comments for all public types and public members.

## Package Versions

NuGet versions were checked against NuGet on 2026-04-28:

- `xunit` `2.9.3`: https://www.nuget.org/packages/xunit/2.9.3
- `xunit.runner.visualstudio` `3.1.5`: https://www.nuget.org/packages/xunit.runner.visualstudio/3.1.5
- `Microsoft.NET.Test.Sdk` `18.4.0`: https://www.nuget.org/packages/Microsoft.NET.Test.Sdk/18.4.0
- `FluentAssertions` `8.9.0`: https://www.nuget.org/packages/FluentAssertions/8.9.0
- `coverlet.collector` `10.0.0`: https://www.nuget.org/packages/coverlet.collector/10.0.0

## File Structure

Create production files:

- `backend/dotnet/BuildingBlocks/src/Tw.Core/Check.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.Core/Configuration/ConfigurationSectionAttribute.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.Core/Configuration/IConfigurableOptions.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.Core/Collections/ITypeList.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.Core/Collections/TypeList.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.Core/Context/ICurrentUser.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.Core/Exceptions/TwException.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.Core/Exceptions/TwConfigurationException.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.Core/Extensions/ByteArrayExtensions.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.Core/Extensions/CollectionExtensions.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.Core/Extensions/ComparableExtensions.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.Core/Extensions/DateTimeExtensions.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.Core/Extensions/DictionaryExtensions.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.Core/Extensions/EnumerableExtensions.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.Core/Extensions/ExceptionExtensions.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.Core/Extensions/GuidExtensions.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.Core/Extensions/ListExtensions.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.Core/Extensions/NumberExtensions.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.Core/Extensions/ObjectExtensions.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.Core/Extensions/StreamExtensions.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.Core/Extensions/StringExtensions.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.Core/Extensions/TypeExtensions.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.Core/Primitives/NamedAction.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.Core/Primitives/NamedActionList.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.Core/Primitives/NamedObject.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.Core/Primitives/NamedObjectList.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.Core/Primitives/NamedTypeSelector.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.Core/Primitives/NamedTypeSelectorListExtensions.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.Core/Primitives/NamedValue.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.Core/Reflection/ITypeFinder.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.Core/Reflection/TypeFinder.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.Core/Reflection/TypeFinderExtensions.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.Core/Reflection/ReflectionCache.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.Core/Security/Cryptography/HexEncoding.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.Core/Security/Cryptography/HashComparison.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.Core/Security/Cryptography/Md5Hasher.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.Core/Security/Cryptography/Sha1Hasher.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.Core/Security/Cryptography/Sha256Hasher.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.Core/Security/Cryptography/Sha384Hasher.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.Core/Security/Cryptography/Sha512Hasher.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.Core/Security/Cryptography/Sha3256Hasher.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.Core/Security/Cryptography/Sha3384Hasher.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.Core/Security/Cryptography/Sha3512Hasher.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.Core/Security/Cryptography/HmacMd5Hasher.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.Core/Security/Cryptography/HmacSha1Hasher.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.Core/Security/Cryptography/HmacSha256Hasher.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.Core/Security/Cryptography/HmacSha384Hasher.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.Core/Security/Cryptography/HmacSha512Hasher.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.Core/Security/Cryptography/HmacSha3256Hasher.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.Core/Security/Cryptography/HmacSha3384Hasher.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.Core/Security/Cryptography/HmacSha3512Hasher.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.Core/Security/Cryptography/AesCryptography.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.Core/Security/Cryptography/DesCryptography.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.Core/Security/Cryptography/TripleDesCryptography.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.Core/Security/Cryptography/RsaCryptography.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.Core/Security/Cryptography/RsaKeyPair.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.Core/Security/Cryptography/RsaDerKeyPair.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.Core/Security/Cryptography/Pbkdf2PasswordHasher.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.Core/Security/Cryptography/StringCryptographyExtensions.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.Core/Timing/IClock.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.Core/Utilities/AsyncDisposeFunc.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.Core/Utilities/DisposeAction.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.Core/Utilities/NullAsyncDisposable.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.Core/Utilities/NullDisposable.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.Core/Utilities/SecureRandomGenerator.cs`

Create test infrastructure:

- `backend/dotnet/BuildingBlocks/tests/Tw.TestBase/Tw.TestBase.csproj`
- `backend/dotnet/BuildingBlocks/tests/Tw.TestBase/TemporaryDirectory.cs`
- `backend/dotnet/BuildingBlocks/tests/Tw.TestBase/TestBytes.cs`
- `backend/dotnet/BuildingBlocks/tests/Tw.TestBase/TestStreams.cs`
- `backend/dotnet/BuildingBlocks/tests/Tw.TestBase/CryptoTestVectors.cs`
- `backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Tw.Core.Tests.csproj`

Create test files:

- `backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/CheckTests.cs`
- `backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/ConfigurationTests.cs`
- `backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/ContextAndTimingTests.cs`
- `backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/ExceptionTests.cs`
- `backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/PrimitiveTests.cs`
- `backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/TypeListTests.cs`
- `backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/DisposableTests.cs`
- `backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/SecureRandomGeneratorTests.cs`
- `backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Extensions/*Tests.cs`
- `backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Reflection/*Tests.cs`
- `backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Security/Cryptography/*Tests.cs`

Modify:

- `backend/dotnet/Build/Packages.Tests.props`
- `backend/dotnet/Tw.SmartPlatform.slnx`

Generated by restore:

- `backend/dotnet/BuildingBlocks/tests/Tw.TestBase/packages.lock.json`
- `backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/packages.lock.json`

---

### Task 1: Create Test Projects And Central Package Versions

**Files:**
- Modify: `backend/dotnet/Build/Packages.Tests.props`
- Modify: `backend/dotnet/Tw.SmartPlatform.slnx`
- Create: `backend/dotnet/BuildingBlocks/tests/Tw.TestBase/Tw.TestBase.csproj`
- Create: `backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Tw.Core.Tests.csproj`

- [ ] **Step 1: Write package versions**

Replace `backend/dotnet/Build/Packages.Tests.props` with:

```xml
<!-- Purpose: central package versions for unit and integration test projects. -->
<Project>
  <ItemGroup>
    <!-- Test framework -->
    <PackageVersion Include="xunit" Version="2.9.3" />
    <PackageVersion Include="xunit.runner.visualstudio" Version="3.1.5" />
    <PackageVersion Include="Microsoft.NET.Test.Sdk" Version="18.4.0" />

    <!-- Assertions -->
    <PackageVersion Include="FluentAssertions" Version="8.9.0" />

    <!-- Coverage -->
    <PackageVersion Include="coverlet.collector" Version="10.0.0" />
  </ItemGroup>
</Project>
```

- [ ] **Step 2: Create `Tw.TestBase` project**

Create `backend/dotnet/BuildingBlocks/tests/Tw.TestBase/Tw.TestBase.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

</Project>
```

- [ ] **Step 3: Create `Tw.Core.Tests` project**

Create `backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Tw.Core.Tests.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="xunit" />
    <PackageReference Include="xunit.runner.visualstudio">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
    <PackageReference Include="FluentAssertions" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\Tw.Core\Tw.Core.csproj" />
    <ProjectReference Include="..\Tw.TestBase\Tw.TestBase.csproj" />
  </ItemGroup>

</Project>
```

- [ ] **Step 4: Add projects to solution**

In `backend/dotnet/Tw.SmartPlatform.slnx`, replace:

```xml
  <Folder Name="/BuildingBlocks/tests/" />
```

with:

```xml
  <Folder Name="/BuildingBlocks/tests/">
    <Project Path="BuildingBlocks/tests/Tw.TestBase/Tw.TestBase.csproj" />
    <Project Path="BuildingBlocks/tests/Tw.Core.Tests/Tw.Core.Tests.csproj" />
  </Folder>
```

- [ ] **Step 5: Restore solution**

Run from `backend/dotnet`:

```powershell
dotnet restore .\Tw.SmartPlatform.slnx
```

Expected: restore succeeds and creates lock files for the two test projects.

- [ ] **Step 6: Commit**

```powershell
git add Build/Packages.Tests.props `
        Tw.SmartPlatform.slnx `
        BuildingBlocks/tests/Tw.TestBase/Tw.TestBase.csproj `
        BuildingBlocks/tests/Tw.TestBase/packages.lock.json `
        BuildingBlocks/tests/Tw.Core.Tests/Tw.Core.Tests.csproj `
        BuildingBlocks/tests/Tw.Core.Tests/packages.lock.json
git commit -m "test: add BuildingBlocks test projects"
```

---

### Task 2: Add Shared Test Fixtures

**Files:**
- Create: `backend/dotnet/BuildingBlocks/tests/Tw.TestBase/TemporaryDirectory.cs`
- Create: `backend/dotnet/BuildingBlocks/tests/Tw.TestBase/TestBytes.cs`
- Create: `backend/dotnet/BuildingBlocks/tests/Tw.TestBase/TestStreams.cs`
- Create: `backend/dotnet/BuildingBlocks/tests/Tw.TestBase/CryptoTestVectors.cs`

- [ ] **Step 1: Create temporary directory helper**

Create `TemporaryDirectory.cs`:

```csharp
using System.Text;

namespace Tw.TestBase;

public sealed class TemporaryDirectory : IDisposable
{
    public TemporaryDirectory()
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "tw-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path);
    }

    public string Path { get; }

    public string GetPath(string fileName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        return System.IO.Path.Combine(Path, fileName);
    }

    public FileInfo WriteAllText(string fileName, string contents, Encoding? encoding = null)
    {
        var filePath = GetPath(fileName);
        File.WriteAllText(filePath, contents, encoding ?? Encoding.UTF8);
        return new FileInfo(filePath);
    }

    public void Dispose()
    {
        if (Directory.Exists(Path))
        {
            Directory.Delete(Path, recursive: true);
        }
    }
}
```

- [ ] **Step 2: Create deterministic bytes and streams**

Create `TestBytes.cs`:

```csharp
using System.Text;

namespace Tw.TestBase;

public static class TestBytes
{
    public const string Text = "abc";
    public const string LongText = "The quick brown fox jumps over the lazy dog";
    public const string HmacKey = "key";
    public const string AesKey16 = "0123456789abcdef";
    public const string DesKey8 = "12345678";
    public const string TripleDesKey24 = "0123456789abcdef01234567";

    public static byte[] AbcUtf8 => Encoding.UTF8.GetBytes(Text);

    public static byte[] LongTextUtf8 => Encoding.UTF8.GetBytes(LongText);

    public static byte[] HmacKeyUtf8 => Encoding.UTF8.GetBytes(HmacKey);
}
```

Create `TestStreams.cs`:

```csharp
using System.Text;

namespace Tw.TestBase;

public static class TestStreams
{
    public static MemoryStream FromText(string value, Encoding? encoding = null)
    {
        return new MemoryStream((encoding ?? Encoding.UTF8).GetBytes(value));
    }
}
```

- [ ] **Step 3: Create cryptography vectors**

Create `CryptoTestVectors.cs`:

```csharp
namespace Tw.TestBase;

public static class CryptoTestVectors
{
    public const string Md5Abc = "900150983cd24fb0d6963f7d28e17f72";
    public const string Sha1Abc = "a9993e364706816aba3e25717850c26c9cd0d89d";
    public const string Sha256Abc = "ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad";
    public const string Sha384Abc = "cb00753f45a35e8bb5a03d699ac65007272c32ab0eded1631a8b605a43ff5bed8086072ba1e7cc2358baeca134c825a7";
    public const string Sha512Abc = "ddaf35a193617abacc417349ae20413112e6fa4e89a97ea20a9eeee64b55d39a2192992a274fc1a836ba3c23a3feebbd454d4423643ce80e2a9ac94fa54ca49f";
    public const string Sha3256Abc = "3a985da74fe225b2045c172d6bd390bd855f086e3e9d525b46bfe24511431532";
    public const string Sha3384Abc = "ec01498288516fc926459f58e2c6ad8df9b473cb0fc08c2596da7cf0e49be4b298d88cea927ac7f539f1edf228376d25";
    public const string Sha3512Abc = "b751850b1a57168a5693cd924b6b096e08f621827444f70d884f5d0240d2712e10e116e9192af3c91a7ec57647e3934057340b4cf408d5a56592f8274eec53f0";

    public const string HmacMd5Fox = "80070713463e7749b90c2dc24911e275";
    public const string HmacSha1Fox = "de7c9b85b8b78aa6bc8a7a36f70a90701c9db4d9";
    public const string HmacSha256Fox = "f7bc83f430538424b13298e6aa6fb143ef4d59a14946175997479dbc2d1a3cd8";
    public const string HmacSha384Fox = "d7f4727e2c0b39ae0f1e40cc96f60242d5b7801841cea6fc592c5d3e1ae50700582a96cf35e1e554995fe4e03381c237";
    public const string HmacSha512Fox = "b42af09057bac1e2d41708e48a902e09b5ff7f12ab428a4fe86653c73dd248fb82f948a549f7b791a5b41915ee4d1ec3935357e4e2317250d0372afa2ebeeb3a";
    public const string HmacSha3256Fox = "8c6e0683409427f8931711b10ca92a506eb1fafa48fadd66d76126f47ac2c333";
    public const string HmacSha3384Fox = "aa739ad9fcdf9be4a04f06680ade7a1bd1e01a0af64accb04366234cf9f6934a0f8589772f857681fcde8acc256091a2";
    public const string HmacSha3512Fox = "237a35049c40b3ef5ddd960b3dc893d8284953b9a4756611b1b61bffcf53edd979f93547db714b06ef0a692062c609b70208ab8d4a280ceee40ed8100f293063";
}
```

- [ ] **Step 4: Build TestBase**

Run from `backend/dotnet`:

```powershell
dotnet build .\BuildingBlocks\tests\Tw.TestBase\Tw.TestBase.csproj --no-restore
```

Expected: build succeeds.

- [ ] **Step 5: Commit**

```powershell
git add BuildingBlocks/tests/Tw.TestBase
git commit -m "test: add shared test fixtures"
```

---

### Task 3: Migrate Guard, Configuration, Context, Timing, And Exceptions

**Files:**
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/Check.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/Configuration/ConfigurationSectionAttribute.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/Configuration/IConfigurableOptions.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/Context/ICurrentUser.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/Timing/IClock.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/Exceptions/TwException.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/Exceptions/TwConfigurationException.cs`
- Test: `backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/CheckTests.cs`
- Test: `backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/ConfigurationTests.cs`
- Test: `backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/ContextAndTimingTests.cs`
- Test: `backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/ExceptionTests.cs`

- [ ] **Step 1: Write failing tests**

Create `CheckTests.cs` with tests for:

```csharp
using FluentAssertions;
using Tw.Core;

namespace Tw.Core.Tests;

public sealed class CheckTests
{
    [Fact]
    public void NotNull_Returns_Value()
    {
        var value = new object();
        Check.NotNull(value).Should().BeSameAs(value);
    }

    [Fact]
    public void NotNull_Throws_For_Null()
    {
        object? value = null;
        var act = () => Check.NotNull(value);
        act.Should().Throw<ArgumentNullException>().WithParameterName(nameof(value));
    }

    [Fact]
    public void NotNullOrWhiteSpace_Throws_For_Whitespace()
    {
        var act = () => Check.NotNullOrWhiteSpace(" ");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Positive_Throws_For_Zero()
    {
        var act = () => Check.Positive(0);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void AssignableTo_Throws_For_Unassignable_Type()
    {
        var act = () => Check.AssignableTo<IDisposable>(typeof(string));
        act.Should().Throw<ArgumentException>();
    }
}
```

Create the other test files with assertions:

```csharp
using FluentAssertions;
using Tw.Core.Configuration;
using Tw.Core.Context;
using Tw.Core.Exceptions;
using Tw.Core.Timing;

namespace Tw.Core.Tests;

public sealed class ConfigurationTests
{
    [Fact]
    public void ConfigurationSectionAttribute_Stores_Name()
    {
        new ConfigurationSectionAttribute("Auth").Name.Should().Be("Auth");
    }

    [Fact]
    public void ConfigurableOptions_Is_Marker_Interface()
    {
        typeof(IConfigurableOptions).GetMembers().Should().BeEmpty();
    }
}

public sealed class ContextAndTimingTests
{
    [Fact]
    public void CurrentUser_Does_Not_Expose_TenantId()
    {
        typeof(ICurrentUser).GetProperty("TenantId").Should().BeNull();
    }

    [Fact]
    public void Clock_Interface_Exposes_Normalization_Contract()
    {
        typeof(IClock).GetMethod(nameof(IClock.Normalize)).Should().NotBeNull();
    }
}

public sealed class ExceptionTests
{
    [Fact]
    public void TwException_Is_Concrete_Exception_Base()
    {
        var exception = new TwException("failure");

        exception.Should().BeAssignableTo<Exception>();
        exception.Message.Should().Be("failure");
    }

    [Fact]
    public void TwConfigurationException_Derives_From_TwException()
    {
        new TwConfigurationException("bad config").Should().BeAssignableTo<TwException>();
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run:

```powershell
dotnet test .\BuildingBlocks\tests\Tw.Core.Tests\Tw.Core.Tests.csproj --no-restore --filter "FullyQualifiedName~CheckTests|FullyQualifiedName~ConfigurationTests|FullyQualifiedName~ContextAndTimingTests|FullyQualifiedName~ExceptionTests"
```

Expected: tests fail because production types are missing.

- [ ] **Step 3: Implement production APIs**

Implement `Check` from old `Check.cs` with namespace `Tw.Core`, file-scoped namespace, XML comments, and these public members:

```csharp
public static T NotNull<T>(T? value, [CallerArgumentExpression(nameof(value))] string? parameterName = null);
public static string NotNullOrWhiteSpace(string? value, [CallerArgumentExpression(nameof(value))] string? parameterName = null);
public static string NotNullOrEmpty(string? value, [CallerArgumentExpression(nameof(value))] string? parameterName = null);
public static IEnumerable<T> NotNullOrEmpty<T>(IEnumerable<T>? value, [CallerArgumentExpression(nameof(value))] string? parameterName = null);
public static ICollection<T> NotNullOrEmpty<T>(ICollection<T>? value, [CallerArgumentExpression(nameof(value))] string? parameterName = null);
public static int Positive(int value, [CallerArgumentExpression(nameof(value))] string? parameterName = null);
public static long Positive(long value, [CallerArgumentExpression(nameof(value))] string? parameterName = null);
public static int NonNegative(int value, [CallerArgumentExpression(nameof(value))] string? parameterName = null);
public static int InRange(int value, int min, int max, [CallerArgumentExpression(nameof(value))] string? parameterName = null);
public static Type AssignableTo<TBaseType>(Type? type, [CallerArgumentExpression(nameof(type))] string? parameterName = null);
public static Type AssignableTo(Type? type, Type baseType, [CallerArgumentExpression(nameof(type))] string? parameterName = null);
```

Implement configuration, context, timing, and exceptions with these declarations:

```csharp
namespace Tw.Core.Configuration;
public sealed class ConfigurationSectionAttribute(string name) : Attribute
{
    public string Name { get; } = Tw.Core.Check.NotNullOrWhiteSpace(name);
}
public interface IConfigurableOptions;

namespace Tw.Core.Context;
public interface ICurrentUser
{
    bool IsAuthenticated { get; }
    Guid? Id { get; }
    string? UserName { get; }
    string? Name { get; }
    string? SurName { get; }
    string? Email { get; }
    bool EmailVerified { get; }
    string? PhoneNumber { get; }
    bool PhoneNumberVerified { get; }
    string[] Roles { get; }
    Claim? FindClaim(string claimType);
    Claim[] FindClaims(string claimType);
    Claim[] GetAllClaims();
    bool IsInRole(string roleName);
}

namespace Tw.Core.Timing;
public interface IClock
{
    DateTime Now { get; }
    DateTimeKind Kind { get; }
    bool SupportsMultipleTimezone { get; }
    DateTime Normalize(DateTime dateTime);
    DateTime ConvertToUserTime(DateTime utcDateTime);
    DateTimeOffset ConvertToUserTime(DateTimeOffset dateTimeOffset);
    DateTime ConvertToUtc(DateTime dateTime);
}

namespace Tw.Core.Exceptions;
public class TwException : Exception
{
    public TwException();
    public TwException(string message);
    public TwException(string message, Exception innerException);
}
public class TwConfigurationException : TwException
{
    public TwConfigurationException(string message);
    public TwConfigurationException(string message, Exception innerException);
}
```

- [ ] **Step 4: Run focused tests**

Run:

```powershell
dotnet test .\BuildingBlocks\tests\Tw.Core.Tests\Tw.Core.Tests.csproj --no-restore --filter "FullyQualifiedName~CheckTests|FullyQualifiedName~ConfigurationTests|FullyQualifiedName~ContextAndTimingTests|FullyQualifiedName~ExceptionTests"
```

Expected: tests pass.

- [ ] **Step 5: Commit**

```powershell
git add BuildingBlocks/src/Tw.Core/Check.cs `
        BuildingBlocks/src/Tw.Core/Configuration `
        BuildingBlocks/src/Tw.Core/Context `
        BuildingBlocks/src/Tw.Core/Timing `
        BuildingBlocks/src/Tw.Core/Exceptions `
        BuildingBlocks/tests/Tw.Core.Tests/CheckTests.cs `
        BuildingBlocks/tests/Tw.Core.Tests/ConfigurationTests.cs `
        BuildingBlocks/tests/Tw.Core.Tests/ContextAndTimingTests.cs `
        BuildingBlocks/tests/Tw.Core.Tests/ExceptionTests.cs
git commit -m "feat: add core guard and base abstractions"
```

---

### Task 4: Migrate Primitives And Type Lists

**Files:**
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/Primitives/*.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/Collections/ITypeList.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/Collections/TypeList.cs`
- Test: `backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/PrimitiveTests.cs`
- Test: `backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/TypeListTests.cs`

- [ ] **Step 1: Write failing primitive tests**

Create tests asserting:

```csharp
using FluentAssertions;
using Tw.Core.Collections;
using Tw.Core.Primitives;

namespace Tw.Core.Tests;

public sealed class PrimitiveTests
{
    [Fact]
    public void NamedObject_Rejects_Blank_Name()
    {
        var act = () => new NamedObject(" ");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void NamedAction_Stores_Action()
    {
        var called = false;
        var action = new NamedAction<object>("run", _ => called = true);

        action.Action(new object());

        called.Should().BeTrue();
        action.Name.Should().Be("run");
    }

    [Fact]
    public void NamedValue_Stores_Name_And_Value()
    {
        var value = new NamedValue<int>("answer", 42);

        value.Name.Should().Be("answer");
        value.Value.Should().Be(42);
    }

    [Fact]
    public void NamedTypeSelectorList_Adds_Exact_Type_Matcher()
    {
        var selectors = new List<NamedTypeSelector>();
        selectors.Add("strings", typeof(string));

        selectors[0].Predicate(typeof(string)).Should().BeTrue();
        selectors[0].Predicate(typeof(int)).Should().BeFalse();
    }
}

public sealed class TypeListTests
{
    [Fact]
    public void TypeList_Accepts_Assignable_Types()
    {
        var list = new TypeList<IDisposable>();

        list.Add(typeof(MemoryStream));

        list.Should().Contain(typeof(MemoryStream));
    }

    [Fact]
    public void TypeList_Rejects_Unassignable_Types()
    {
        var list = new TypeList<IDisposable>();

        var act = () => list.Add(typeof(string));

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void TryAdd_Returns_False_For_Duplicate_Type()
    {
        var list = new TypeList<IDisposable>();

        list.TryAdd<MemoryStream>().Should().BeTrue();
        list.TryAdd<MemoryStream>().Should().BeFalse();
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

```powershell
dotnet test .\BuildingBlocks\tests\Tw.Core.Tests\Tw.Core.Tests.csproj --no-restore --filter "FullyQualifiedName~PrimitiveTests|FullyQualifiedName~TypeListTests"
```

Expected: tests fail because types are missing.

- [ ] **Step 3: Implement primitive files**

Port these old files into `Tw.Core.Primitives`:

| Old file | New file | Rename |
| --- | --- | --- |
| `NamedAction.cs` | `Primitives/NamedAction.cs` | keep type name |
| `NamedActionList.cs` | `Primitives/NamedActionList.cs` | keep type name |
| `NamedObject.cs` | `Primitives/NamedObject.cs` | keep type name |
| `NamedObjectList.cs` | `Primitives/NamedObjectList.cs` | keep type name |
| `NamedTypeSelector.cs` | `Primitives/NamedTypeSelector.cs` | keep type name |
| `NamedTypeSelectorListExtensions.cs` | `Primitives/NamedTypeSelectorListExtensions.cs` | keep type name |
| `NameValue.cs` | `Primitives/NamedValue.cs` | `NameValue` to `NamedValue` |

Public declarations:

```csharp
public class NamedObject(string name);
public class NamedObjectList<T> : List<T> where T : NamedObject;
public class NamedAction<T>(string name, Action<T> action) : NamedObject(name);
public class NamedActionList<T> : NamedObjectList<NamedAction<T>>;
public class NamedTypeSelector(string name, Func<Type, bool> predicate);
public static class NamedTypeSelectorListExtensions;
[Serializable] public class NamedValue : NamedValue<string>;
[Serializable] public class NamedValue<T>;
```

- [ ] **Step 4: Implement type list files**

Port `Collections/ITypeList.cs` and `Collections/TypeList.cs` into `Tw.Core.Collections` with these members:

```csharp
public interface ITypeList : ITypeList<object>;
public interface ITypeList<in TBaseType> : IList<Type>
{
    void Add<T>() where T : TBaseType;
    bool TryAdd<T>() where T : TBaseType;
    bool Contains<T>() where T : TBaseType;
    bool Remove<T>() where T : TBaseType;
}

public class TypeList : TypeList<object>, ITypeList;
public class TypeList<TBaseType> : ITypeList<TBaseType>;
```

- [ ] **Step 5: Run focused tests**

```powershell
dotnet test .\BuildingBlocks\tests\Tw.Core.Tests\Tw.Core.Tests.csproj --no-restore --filter "FullyQualifiedName~PrimitiveTests|FullyQualifiedName~TypeListTests"
```

Expected: tests pass.

- [ ] **Step 6: Commit**

```powershell
git add BuildingBlocks/src/Tw.Core/Primitives `
        BuildingBlocks/src/Tw.Core/Collections `
        BuildingBlocks/tests/Tw.Core.Tests/PrimitiveTests.cs `
        BuildingBlocks/tests/Tw.Core.Tests/TypeListTests.cs
git commit -m "feat: add core primitives and type lists"
```

---

### Task 5: Migrate Disposable Helpers And Secure Random Generator

**Files:**
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/Utilities/DisposeAction.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/Utilities/AsyncDisposeFunc.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/Utilities/NullDisposable.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/Utilities/NullAsyncDisposable.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/Utilities/SecureRandomGenerator.cs`
- Test: `backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/DisposableTests.cs`
- Test: `backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/SecureRandomGeneratorTests.cs`

- [ ] **Step 1: Write failing disposable tests**

Create tests:

```csharp
using FluentAssertions;
using Tw.Core.Utilities;

namespace Tw.Core.Tests;

public sealed class DisposableTests
{
    [Fact]
    public void DisposeAction_Invokes_Action_Once()
    {
        var count = 0;
        var disposable = new DisposeAction(() => count++);

        disposable.Dispose();
        disposable.Dispose();

        count.Should().Be(1);
    }

    [Fact]
    public async Task AsyncDisposeFunc_Invokes_Function_Once()
    {
        var count = 0;
        var disposable = new AsyncDisposeFunc(() =>
        {
            count++;
            return Task.CompletedTask;
        });

        await disposable.DisposeAsync();
        await disposable.DisposeAsync();

        count.Should().Be(1);
    }

    [Fact]
    public async Task Null_Disposables_Are_Safe()
    {
        NullDisposable.Instance.Dispose();
        await NullAsyncDisposable.Instance.DisposeAsync();
    }
}
```

- [ ] **Step 2: Write failing random generator tests**

Create tests for lengths, ranges, composition, and boundaries:

```csharp
using FluentAssertions;
using Tw.Core.Utilities;

namespace Tw.Core.Tests;

public sealed class SecureRandomGeneratorTests
{
    [Fact]
    public void GetInt_Returns_Value_In_Range()
    {
        var value = SecureRandomGenerator.GetInt(10, 20);
        value.Should().BeInRange(10, 19);
    }

    [Fact]
    public void GetInt_Throws_When_Min_Is_Not_Less_Than_Max()
    {
        var act = () => SecureRandomGenerator.GetInt(5, 5);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void GetBytes_Returns_Requested_Length()
    {
        SecureRandomGenerator.GetBytes(16).Should().HaveCount(16);
    }

    [Fact]
    public void GetStrongPassword_Contains_Required_Categories()
    {
        var password = SecureRandomGenerator.GetStrongPassword(24);

        password.Should().ContainMatch("*[a-z]*");
        password.Should().ContainMatch("*[A-Z]*");
        password.Should().ContainMatch("*[0-9]*");
        password.Should().ContainAny("!@#$%^&*()_+-=[]{}|;:,.<>?");
    }

    [Fact]
    public void GetRandomElements_Returns_Unique_Selection()
    {
        var values = Enumerable.Range(1, 10).ToList();

        var selected = SecureRandomGenerator.GetRandomElements(values, 4);

        selected.Should().HaveCount(4).And.OnlyHaveUniqueItems();
        selected.Should().OnlyContain(values.Contains);
    }
}
```

- [ ] **Step 3: Run tests to verify they fail**

```powershell
dotnet test .\BuildingBlocks\tests\Tw.Core.Tests\Tw.Core.Tests.csproj --no-restore --filter "FullyQualifiedName~DisposableTests|FullyQualifiedName~SecureRandomGeneratorTests"
```

Expected: tests fail because utilities are missing.

- [ ] **Step 4: Implement utility APIs**

Port old `DisposeAction.cs`, `AsyncDisposeFunc.cs`, `NullDisposable.cs`, `NullAsyncDisposable.cs`, and `Utilities/RandomHelper.cs`.

Rename `RandomHelper` to `SecureRandomGenerator` and expose:

```csharp
public static int GetInt(int minValue, int maxValue);
public static int GetInt(int maxValue);
public static long GetLong(long minValue, long maxValue);
public static double GetDouble();
public static double GetDouble(double minValue, double maxValue);
public static bool GetBool();
public static byte[] GetBytes(int length);
public static string GetString(int length, string? chars = null);
public static string GetNumericString(int length);
public static string GetAlphaString(int length, bool upperCase = true);
public static string GetAlphanumericString(int length);
public static string GetStrongPassword(int length = 16, bool includeSpecialChars = true);
public static string Shuffle(string value);
public static T GetRandomElement<T>(IList<T> collection);
public static IList<T> GetRandomElements<T>(IList<T> collection, int count);
public static IList<T> Shuffle<T>(IList<T> collection);
```

Use `RandomNumberGenerator.GetInt32`, `RandomNumberGenerator.Fill`, and argument validation. Fix old source string-literal encoding defects while preserving behavior.

- [ ] **Step 5: Run focused tests**

```powershell
dotnet test .\BuildingBlocks\tests\Tw.Core.Tests\Tw.Core.Tests.csproj --no-restore --filter "FullyQualifiedName~DisposableTests|FullyQualifiedName~SecureRandomGeneratorTests"
```

Expected: tests pass.

- [ ] **Step 6: Commit**

```powershell
git add BuildingBlocks/src/Tw.Core/Utilities `
        BuildingBlocks/tests/Tw.Core.Tests/DisposableTests.cs `
        BuildingBlocks/tests/Tw.Core.Tests/SecureRandomGeneratorTests.cs
git commit -m "feat: add disposable helpers and secure random generator"
```

---

### Task 6: Migrate Basic Extension Methods

**Files:**
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/Extensions/ByteArrayExtensions.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/Extensions/ComparableExtensions.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/Extensions/DateTimeExtensions.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/Extensions/ExceptionExtensions.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/Extensions/GuidExtensions.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/Extensions/NumberExtensions.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/Extensions/ObjectExtensions.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/Extensions/StringExtensions.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/Extensions/TypeExtensions.cs`
- Test: `backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Extensions/BasicExtensionsTests.cs`

- [ ] **Step 1: Write failing tests**

Create `Extensions/BasicExtensionsTests.cs`:

```csharp
using FluentAssertions;
using Tw.Core.Extensions;

namespace Tw.Core.Tests.Extensions;

public sealed class BasicExtensionsTests
{
    [Fact]
    public void ByteArray_ToHexString_Returns_Lowercase_By_Default()
    {
        new byte[] { 0x0a, 0xff }.ToHexString().Should().Be("0aff");
    }

    [Fact]
    public void String_Casing_And_Truncation_Work()
    {
        "hello".ToPascalCase().Should().Be("Hello");
        "Hello".ToCamelCase().Should().Be("hello");
        "HelloWorld".ToSnakeCase().Should().Be("hello_world");
        "abcdef".Left(3).Should().Be("abc");
        "abcdef".Right(2).Should().Be("ef");
        "abcdef".Truncate(3).Should().Be("abc");
    }

    [Fact]
    public void DateTime_Boundaries_Work()
    {
        var value = new DateTime(2026, 4, 28, 13, 10, 9);

        value.StartOfDay().Should().Be(new DateTime(2026, 4, 28));
        value.StartOfMonth().Should().Be(new DateTime(2026, 4, 1));
        value.StartOfYear().Should().Be(new DateTime(2026, 1, 1));
    }

    [Fact]
    public void Number_And_Guid_Extensions_Work()
    {
        4.IsEven().Should().BeTrue();
        5.IsOdd().Should().BeTrue();
        12.Clamp(1, 10).Should().Be(10);
        Guid.Empty.IsNullOrEmpty().Should().BeTrue();
    }

    [Fact]
    public void Type_Extensions_Return_Assignability_And_Base_Classes()
    {
        typeof(MemoryStream).IsAssignableTo<Stream>().Should().BeTrue();
        typeof(MemoryStream).GetBaseClasses().Should().Contain(typeof(Stream));
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

```powershell
dotnet test .\BuildingBlocks\tests\Tw.Core.Tests\Tw.Core.Tests.csproj --no-restore --filter FullyQualifiedName~BasicExtensionsTests
```

Expected: tests fail because extension methods are missing.

- [ ] **Step 3: Implement extension files**

Port the matching old files into `Tw.Core.Extensions` and keep these public members:

```csharp
public static string ToHexString(this byte[] bytes, bool useUpperCase = false);
public static bool IsBetween<T>(this T value, T minInclusiveValue, T maxInclusiveValue) where T : IComparable<T>;
public static long ToUnixTimestamp(this DateTime dateTime);
public static long ToUnixTimestampMilliseconds(this DateTime dateTime);
public static DateTime FromUnixTimestamp(this long timestamp);
public static DateTime FromUnixTimestampMilliseconds(this long timestampMilliseconds);
public static DateTime StartOfDay(this DateTime dateTime);
public static DateTime EndOfDay(this DateTime dateTime);
public static DateTime StartOfWeek(this DateTime dateTime);
public static DateTime EndOfWeek(this DateTime dateTime);
public static DateTime StartOfMonth(this DateTime dateTime);
public static DateTime EndOfMonth(this DateTime dateTime);
public static DateTime StartOfYear(this DateTime dateTime);
public static DateTime EndOfYear(this DateTime dateTime);
public static bool IsWeekend(this DateTime dateTime);
public static bool IsWeekday(this DateTime dateTime);
public static int CalculateAge(this DateTime birthDate);
public static bool IsToday(this DateTime dateTime);
public static bool IsPast(this DateTime dateTime);
public static bool IsFuture(this DateTime dateTime);
public static string ToFriendlyString(this DateTime dateTime);
public static void ReThrow(this Exception exception);
public static bool IsNullOrEmpty(this Guid? value);
public static string ToNString(this Guid value);
public static bool IsEven(this int source);
public static bool IsOdd(this int source);
public static bool IsEven(this long source);
public static bool IsOdd(this long source);
public static int Clamp(this int source, int min, int max);
public static long Clamp(this long source, long min, long max);
public static double Clamp(this double source, double min, double max);
public static decimal Clamp(this decimal source, decimal min, decimal max);
public static string ToFileSize(this long source, int decimalPlaces = 2);
public static double Round(this double source, int decimals = 0);
public static decimal Round(this decimal source, int decimals = 0);
public static string ToPercentage(this double source, int decimals = 2);
public static string ToPercentage(this decimal source, int decimals = 2);
public static T As<T>(this object obj) where T : class;
public static T To<T>(this object obj) where T : struct;
public static bool IsIn<T>(this T item, params T[] list);
public static bool IsIn<T>(this T item, IEnumerable<T> items);
public static T If<T>(this T obj, bool condition, Func<T, T> func);
public static T If<T>(this T obj, bool condition, Action<T> action);
public static bool IsNullOrEmpty(this string? str);
public static bool IsNullOrWhiteSpace(this string? str);
public static string? ToPascalCase(this string? str);
public static string? ToCamelCase(this string? str);
public static string? ToSnakeCase(this string? str);
public static string EnsureEndsWith(this string? str, char c, StringComparison comparisonType = StringComparison.Ordinal);
public static string EnsureStartsWith(this string? str, char c, StringComparison comparisonType = StringComparison.Ordinal);
public static string Left(this string str, int len);
public static string Right(this string str, int len);
public static string? NormalizeLineEndings(this string? str);
public static int NthIndexOf(this string? str, char c, int n);
public static string? RemovePostFix(this string? str, params string[] postFixes);
public static string? RemovePostFix(this string? str, StringComparison comparisonType, params string[] postFixes);
public static string? RemovePreFix(this string? str, params string[] preFixes);
public static string? RemovePreFix(this string? str, StringComparison comparisonType, params string[] preFixes);
public static string? ReplaceFirst(this string? str, string search, string replace, StringComparison comparisonType = StringComparison.Ordinal);
public static string[] Split(this string? str, string separator);
public static string[] Split(this string? str, string separator, StringSplitOptions options);
public static string[] SplitToLines(this string? str);
public static string[] SplitToLines(this string? str, StringSplitOptions options);
public static byte[] GetBytes(this string? str);
public static byte[] GetBytes(this string? str, Encoding encoding);
public static string? Truncate(this string? str, int maxLength);
public static string? TruncateFromBeginning(this string? str, int maxLength);
public static string? TruncateWithPostfix(this string? str, int maxLength);
public static string? TruncateWithPostfix(this string? str, int maxLength, string postfix);
public static IEnumerable<string> Chunk(this string? value, int chunkSize);
public static string? FormatWith(this string? template, params object?[] args);
public static string? RemoveWhiteSpace(this string? value);
public static string? Reverse(this string? value);
public static string? ToBase64(this string? value, Encoding? encoding = null);
public static string? FromBase64(this string? value, Encoding? encoding = null);
public static string GetFullNameWithAssemblyName(this Type type);
public static bool IsAssignableTo<TTarget>(this Type type);
public static bool IsAssignableTo(this Type type, Type targetType);
public static Type[] GetBaseClasses(this Type type, bool includeObject = true);
public static Type[] GetBaseClasses(this Type type, Type stoppingType, bool includeObject = true);
```

- [ ] **Step 4: Run focused tests**

```powershell
dotnet test .\BuildingBlocks\tests\Tw.Core.Tests\Tw.Core.Tests.csproj --no-restore --filter FullyQualifiedName~BasicExtensionsTests
```

Expected: tests pass.

- [ ] **Step 5: Commit**

```powershell
git add BuildingBlocks/src/Tw.Core/Extensions `
        BuildingBlocks/tests/Tw.Core.Tests/Extensions/BasicExtensionsTests.cs
git commit -m "feat: add basic core extensions"
```

---

### Task 7: Migrate Collection, Dictionary, Enumerable, List, And Stream Extensions

**Files:**
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/Extensions/CollectionExtensions.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/Extensions/DictionaryExtensions.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/Extensions/EnumerableExtensions.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/Extensions/ListExtensions.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/Extensions/StreamExtensions.cs`
- Test: `backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Extensions/CollectionExtensionsTests.cs`
- Test: `backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Extensions/StreamExtensionsTests.cs`

- [ ] **Step 1: Write failing tests**

Create focused tests:

```csharp
using FluentAssertions;
using Tw.Core.Extensions;

namespace Tw.Core.Tests.Extensions;

public sealed class CollectionExtensionsTests
{
    [Fact]
    public void AddIfNotContains_Adds_Only_New_Items()
    {
        var values = new List<int> { 1 };

        values.AddIfNotContains(1).Should().BeFalse();
        values.AddIfNotContains(2).Should().BeTrue();

        values.Should().Equal(1, 2);
    }

    [Fact]
    public void Dictionary_GetOrAdd_Uses_Factory_Once()
    {
        var values = new Dictionary<string, int>();
        var first = values.GetOrAdd("a", () => 1);
        var second = values.GetOrAdd("a", () => 2);

        first.Should().Be(1);
        second.Should().Be(1);
    }

    [Fact]
    public async Task Enumerable_ForEachParallelAsync_Processes_All_Items()
    {
        var values = new List<int>();

        await Enumerable.Range(1, 4).ForEachParallelAsync(async value =>
        {
            await Task.Yield();
            lock (values)
            {
                values.Add(value);
            }
        });

        values.Should().BeEquivalentTo([1, 2, 3, 4]);
    }

    [Fact]
    public void List_MoveItem_Moves_Matching_Item()
    {
        var values = new List<int> { 1, 2, 3 };

        values.MoveItem(value => value == 3, 0);

        values.Should().Equal(3, 1, 2);
    }
}

public sealed class StreamExtensionsTests
{
    [Fact]
    public async Task ReadAndWriteTextAsync_RoundTrips_Text()
    {
        await using var stream = new MemoryStream();

        await stream.WriteTextAsync("hello");
        stream.Position = 0;

        var text = await stream.ReadAllTextAsync();
        text.Should().Be("hello");
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

```powershell
dotnet test .\BuildingBlocks\tests\Tw.Core.Tests\Tw.Core.Tests.csproj --no-restore --filter "FullyQualifiedName~CollectionExtensionsTests|FullyQualifiedName~StreamExtensionsTests"
```

Expected: tests fail because methods are missing.

- [ ] **Step 3: Implement extension members**

Port all public members from the old files listed in this task. Preserve nullable annotations, avoid ambiguous overloads beyond the old source, and use `ArgumentNullException.ThrowIfNull` where it keeps behavior clear.

Key public members:

```csharp
public static bool AddIfNotContains<T>(this ICollection<T> source, T item);
public static IEnumerable<T> AddIfNotContains<T>(this ICollection<T> source, IEnumerable<T> items);
public static bool AddIfNotContains<T>(this ICollection<T> source, Func<T, bool> predicate, Func<T> itemFactory);
public static IList<T> RemoveAll<T>(this ICollection<T> source, Func<T, bool> predicate);
public static void RemoveAll<T>(this ICollection<T> source, IEnumerable<T> items);
public static TValue? GetOrDefault<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key) where TKey : notnull;
public static TValue? GetOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key);
public static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, Func<TKey, TValue> factory);
public static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, Func<TValue> factory);
public static TValue? GetOrDefault<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> dictionary, TKey key);
public static TValue? GetOrDefault<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> dictionary, TKey key) where TKey : notnull;
public static TValue GetOrAdd<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> dictionary, TKey key, Func<TValue> factory) where TKey : notnull;
public static dynamic ConvertToDynamicObject(this Dictionary<string, object> dictionary);
public static bool IsNullOrEmpty<T>(this IEnumerable<T>? source);
public static string JoinAsString(this IEnumerable<string> source, string? separator);
public static string JoinAsString<T>(this IEnumerable<T> source, string? separator);
public static void ForEach<T>(this IEnumerable<T> source, Action<T> action);
public static void ForEach<T>(this IEnumerable<T> source, Action<T, int> action);
public static Task ForEachAsync<T>(this IEnumerable<T> source, Func<T, Task> action);
public static Task ForEachParallelAsync<T>(this IEnumerable<T> source, Func<T, Task> action, int maxDegreeOfParallelism = 0, CancellationToken cancellationToken = default);
public static IEnumerable<IEnumerable<T>> Batch<T>(this IEnumerable<T> source, int batchSize);
public static IEnumerable<T> WhereIf<T>(this IEnumerable<T> source, bool condition, Func<T, bool> predicate);
public static IEnumerable<T> WhereIf<T>(this IEnumerable<T> source, bool condition, Func<T, int, bool> predicate);
public static IEnumerable<T> PageBy<T>(this IEnumerable<T> source, int pageNumber, int pageSize);
public static IReadOnlyCollection<T> AsReadOnlyCollection<T>(this IEnumerable<T> source);
public static IReadOnlyList<T> AsReadOnly<T>(this IList<T> source);
public static void InsertRange<T>(this IList<T> source, int index, IEnumerable<T> items);
public static int FindIndex<T>(this IList<T> source, Predicate<T> selector);
public static void AddFirst<T>(this IList<T> source, T item);
public static void AddLast<T>(this IList<T> source, T item);
public static void InsertAfter<T>(this IList<T> source, T existingItem, T item);
public static void InsertAfter<T>(this IList<T> source, Predicate<T> selector, T item);
public static void InsertBefore<T>(this IList<T> source, T existingItem, T item);
public static void InsertBefore<T>(this IList<T> source, Predicate<T> selector, T item);
public static void ReplaceWhile<T>(this IList<T> source, Predicate<T> selector, T item);
public static void ReplaceWhile<T>(this IList<T> source, Predicate<T> selector, Func<T, T> itemFactory);
public static void ReplaceOne<T>(this IList<T> source, Predicate<T> selector, T item);
public static void ReplaceOne<T>(this IList<T> source, Predicate<T> selector, Func<T, T> itemFactory);
public static void ReplaceOne<T>(this IList<T> source, T item, T replaceWith);
public static T GetOrAdd<T>(this IList<T> source, Func<T, bool> selector, Func<T> factory);
public static void MoveItem<T>(this List<T> source, Predicate<T> selector, int targetIndex);
public static byte[] GetAllBytes(this Stream stream);
public static Task<byte[]> GetAllBytesAsync(this Stream stream, CancellationToken cancellationToken = default);
public static Task CopyToAsyncFromBeginning(this Stream stream, Stream destination, CancellationToken cancellationToken = default);
public static MemoryStream CreateMemoryStream(this Stream stream);
public static Task<string> ReadAllTextAsync(this Stream stream, Encoding? encoding = null, CancellationToken cancellationToken = default);
public static void WriteText(this Stream stream, string text, Encoding? encoding = null);
public static Task WriteTextAsync(this Stream stream, string text, Encoding? encoding = null, CancellationToken cancellationToken = default);
public static Stream ResetPosition(this Stream stream);
```

- [ ] **Step 4: Run focused tests**

```powershell
dotnet test .\BuildingBlocks\tests\Tw.Core.Tests\Tw.Core.Tests.csproj --no-restore --filter "FullyQualifiedName~CollectionExtensionsTests|FullyQualifiedName~StreamExtensionsTests"
```

Expected: tests pass.

- [ ] **Step 5: Commit**

```powershell
git add BuildingBlocks/src/Tw.Core/Extensions/CollectionExtensions.cs `
        BuildingBlocks/src/Tw.Core/Extensions/DictionaryExtensions.cs `
        BuildingBlocks/src/Tw.Core/Extensions/EnumerableExtensions.cs `
        BuildingBlocks/src/Tw.Core/Extensions/ListExtensions.cs `
        BuildingBlocks/src/Tw.Core/Extensions/StreamExtensions.cs `
        BuildingBlocks/tests/Tw.Core.Tests/Extensions/CollectionExtensionsTests.cs `
        BuildingBlocks/tests/Tw.Core.Tests/Extensions/StreamExtensionsTests.cs
git commit -m "feat: add collection and stream extensions"
```

---

### Task 8: Migrate Reflection Helpers

**Files:**
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/Reflection/ITypeFinder.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/Reflection/TypeFinder.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/Reflection/TypeFinderExtensions.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/Reflection/ReflectionCache.cs`
- Test: `backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Reflection/ReflectionCacheTests.cs`
- Test: `backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Reflection/TypeFinderTests.cs`

- [ ] **Step 1: Write failing tests**

Create tests:

```csharp
using System.Reflection;
using FluentAssertions;
using Tw.Core.Reflection;

namespace Tw.Core.Tests.Reflection;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
file sealed class ReflectionTestAttribute : Attribute;

file interface IReflectionService;
file sealed class ReflectionService : IReflectionService;
file abstract class AbstractReflectionService : IReflectionService;

public sealed class ReflectionCacheTests
{
    [ReflectionTest]
    private static Task<int> AsyncMethod() => Task.FromResult(1);

    [Fact]
    public void HasAttribute_Uses_Member_Metadata()
    {
        typeof(ReflectionService).Should().NotBeNull();
        typeof(ReflectionCacheTests).GetMethod(nameof(AsyncMethod), BindingFlags.NonPublic | BindingFlags.Static)!
            .HasAttribute<ReflectionTestAttribute>()
            .Should().BeTrue();
    }

    [Fact]
    public void GetAsyncResultType_Returns_Task_Result_Type()
    {
        var method = typeof(ReflectionCacheTests).GetMethod(nameof(AsyncMethod), BindingFlags.NonPublic | BindingFlags.Static)!;

        method.GetAsyncResultType().Should().Be(typeof(int));
    }
}

public sealed class TypeFinderTests
{
    [Fact]
    public void FindTypes_Returns_Non_Abstract_Assignable_Types()
    {
        var finder = new TypeFinder([typeof(ReflectionService).Assembly]);

        finder.FindTypes<IReflectionService>().Should().Contain(typeof(ReflectionService));
        finder.FindTypes<IReflectionService>().Should().NotContain(typeof(AbstractReflectionService));
    }

    [Fact]
    public void TypeFinderExtensions_Do_Not_Require_DependencyInjection()
    {
        typeof(TypeFinderExtensions).Assembly.GetReferencedAssemblies()
            .Any(name => name.Name == "Microsoft.Extensions.DependencyInjection.Abstractions")
            .Should().BeFalse();
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

```powershell
dotnet test .\BuildingBlocks\tests\Tw.Core.Tests\Tw.Core.Tests.csproj --no-restore --filter "FullyQualifiedName~ReflectionCacheTests|FullyQualifiedName~TypeFinderTests"
```

Expected: tests fail because reflection types are missing.

- [ ] **Step 3: Implement reflection APIs**

Port `ITypeFinder.cs`, `TypeFinder.cs`, and `ReflectionCache.cs` into `Tw.Core.Reflection`.

Replace the old DI-specific `TypeFinderExtensions.GetTypesToRegister()` with reflection-only helpers:

```csharp
public static class TypeFinderExtensions
{
    public static IEnumerable<Type> FindConcreteTypes(this ITypeFinder typeFinder);
    public static IEnumerable<Type> FindConcreteTypesAssignableTo<TBaseType>(this ITypeFinder typeFinder);
    public static IEnumerable<Type> FindConcreteTypesAssignableTo(this ITypeFinder typeFinder, Type baseType);
}
```

Implementation rules:

- `TypeFinder` keeps skip prefixes `System`, `Microsoft`, and `Windows`.
- `FindTypes(Type baseType)` returns non-abstract, non-interface types assignable to `baseType`.
- `ReflectionTypeLoadException` handling keeps non-null loaded types.
- `ReflectionCache.GetStatistics()` and `CacheStatistics` remain compiled only under `#if DEBUG`.
- No production reference to DI marker interfaces, service registration attributes, or `Microsoft.Extensions.DependencyInjection.Abstractions`.

- [ ] **Step 4: Run focused tests**

```powershell
dotnet test .\BuildingBlocks\tests\Tw.Core.Tests\Tw.Core.Tests.csproj --no-restore --filter "FullyQualifiedName~ReflectionCacheTests|FullyQualifiedName~TypeFinderTests"
```

Expected: tests pass.

- [ ] **Step 5: Commit**

```powershell
git add BuildingBlocks/src/Tw.Core/Reflection `
        BuildingBlocks/tests/Tw.Core.Tests/Reflection
git commit -m "feat: add reflection helpers"
```

---

### Task 9: Migrate Non-HMAC Hashers

**Files:**
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/Security/Cryptography/HexEncoding.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/Security/Cryptography/HashComparison.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/Security/Cryptography/Md5Hasher.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/Security/Cryptography/Sha1Hasher.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/Security/Cryptography/Sha256Hasher.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/Security/Cryptography/Sha384Hasher.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/Security/Cryptography/Sha512Hasher.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/Security/Cryptography/Sha3256Hasher.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/Security/Cryptography/Sha3384Hasher.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/Security/Cryptography/Sha3512Hasher.cs`
- Test: `backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Security/Cryptography/HasherTests.cs`

- [ ] **Step 1: Write failing hasher tests**

Create `HasherTests.cs`:

```csharp
using FluentAssertions;
using Tw.Core.Security.Cryptography;
using Tw.TestBase;

namespace Tw.Core.Tests.Security.Cryptography;

public sealed class HasherTests
{
    [Theory]
    [InlineData(nameof(Md5Hasher), CryptoTestVectors.Md5Abc)]
    [InlineData(nameof(Sha1Hasher), CryptoTestVectors.Sha1Abc)]
    [InlineData(nameof(Sha256Hasher), CryptoTestVectors.Sha256Abc)]
    [InlineData(nameof(Sha384Hasher), CryptoTestVectors.Sha384Abc)]
    [InlineData(nameof(Sha512Hasher), CryptoTestVectors.Sha512Abc)]
    [InlineData(nameof(Sha3256Hasher), CryptoTestVectors.Sha3256Abc)]
    [InlineData(nameof(Sha3384Hasher), CryptoTestVectors.Sha3384Abc)]
    [InlineData(nameof(Sha3512Hasher), CryptoTestVectors.Sha3512Abc)]
    public void ComputeHash_Returns_Known_Vector(string hasherName, string expected)
    {
        var actual = hasherName switch
        {
            nameof(Md5Hasher) => Md5Hasher.ComputeHash(TestBytes.Text),
            nameof(Sha1Hasher) => Sha1Hasher.ComputeHash(TestBytes.Text),
            nameof(Sha256Hasher) => Sha256Hasher.ComputeHash(TestBytes.Text),
            nameof(Sha384Hasher) => Sha384Hasher.ComputeHash(TestBytes.Text),
            nameof(Sha512Hasher) => Sha512Hasher.ComputeHash(TestBytes.Text),
            nameof(Sha3256Hasher) => Sha3256Hasher.ComputeHash(TestBytes.Text),
            nameof(Sha3384Hasher) => Sha3384Hasher.ComputeHash(TestBytes.Text),
            nameof(Sha3512Hasher) => Sha3512Hasher.ComputeHash(TestBytes.Text),
            _ => throw new InvalidOperationException(hasherName)
        };

        actual.Should().Be(expected);
    }

    [Fact]
    public void VerifyHash_Returns_False_For_Different_Hash()
    {
        Sha256Hasher.VerifyHash(TestBytes.Text, CryptoTestVectors.Sha512Abc).Should().BeFalse();
    }

    [Fact]
    public async Task ComputeFileHashAsync_Reads_Stream()
    {
        await using var stream = TestStreams.FromText(TestBytes.Text);

        var hash = await Sha256Hasher.ComputeFileHashAsync(stream);

        hash.Should().Be(CryptoTestVectors.Sha256Abc);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

```powershell
dotnet test .\BuildingBlocks\tests\Tw.Core.Tests\Tw.Core.Tests.csproj --no-restore --filter FullyQualifiedName~HasherTests
```

Expected: tests fail because hasher classes are missing.

- [ ] **Step 3: Implement shared helpers**

Create internal helper APIs:

```csharp
namespace Tw.Core.Security.Cryptography;

internal static class HexEncoding
{
    public static string ToHex(byte[] bytes, bool useUpperCase = false);
    public static byte[] FromHex(string hex);
}

internal static class HashComparison
{
    public static bool FixedTimeEqualsHex(string expectedHash, string actualHash);
}
```

Use `Convert.ToHexString`, `Convert.FromHexString`, lowercase normalization, and `CryptographicOperations.FixedTimeEquals`.

- [ ] **Step 4: Implement hasher classes**

Port behavior from old `MD5Encryption.cs`, `SHA1Encryption.cs`, `SHA256Encryption.cs`, `SHA384Encryption.cs`, `SHA512Encryption.cs`, `SHA3_256Encryption.cs`, `SHA3_384Encryption.cs`, and `SHA3_512Encryption.cs`.

Expose these method families on every hasher:

```csharp
public static string ComputeHash(string input, bool useUpperCase = false, Encoding? encoding = null);
public static string ComputeHash(byte[] bytes, bool useUpperCase = false);
public static Task<string> ComputeFileHashAsync(string filePath, bool useUpperCase = false, CancellationToken cancellationToken = default);
public static Task<string> ComputeFileHashAsync(Stream stream, bool useUpperCase = false, CancellationToken cancellationToken = default);
public static bool VerifyHash(string input, string hash, Encoding? encoding = null);
public static bool VerifyHash(byte[] bytes, string hash);
```

`Md5Hasher` also supports short 16-character hashes:

```csharp
public static string ComputeHash(string input, bool useUpperCase = false, bool useShortHash = false, Encoding? encoding = null);
public static string ComputeHash(byte[] bytes, bool useUpperCase = false, bool useShortHash = false);
public static Task<string> ComputeFileHashAsync(string filePath, bool useUpperCase = false, bool useShortHash = false, CancellationToken cancellationToken = default);
public static Task<string> ComputeFileHashAsync(Stream stream, bool useUpperCase = false, bool useShortHash = false, CancellationToken cancellationToken = default);
public static bool VerifyHash(string input, string hash, bool useShortHash = false, Encoding? encoding = null);
public static bool VerifyHash(byte[] bytes, string hash, bool useShortHash = false);
```

- [ ] **Step 5: Run focused tests**

```powershell
dotnet test .\BuildingBlocks\tests\Tw.Core.Tests\Tw.Core.Tests.csproj --no-restore --filter FullyQualifiedName~HasherTests
```

Expected: tests pass.

- [ ] **Step 6: Commit**

```powershell
git add BuildingBlocks/src/Tw.Core/Security/Cryptography/*Hasher.cs `
        BuildingBlocks/src/Tw.Core/Security/Cryptography/HexEncoding.cs `
        BuildingBlocks/src/Tw.Core/Security/Cryptography/HashComparison.cs `
        BuildingBlocks/tests/Tw.Core.Tests/Security/Cryptography/HasherTests.cs
git commit -m "feat: add cryptographic hashers"
```

---

### Task 10: Migrate HMAC Hashers

**Files:**
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/Security/Cryptography/HmacMd5Hasher.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/Security/Cryptography/HmacSha1Hasher.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/Security/Cryptography/HmacSha256Hasher.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/Security/Cryptography/HmacSha384Hasher.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/Security/Cryptography/HmacSha512Hasher.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/Security/Cryptography/HmacSha3256Hasher.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/Security/Cryptography/HmacSha3384Hasher.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/Security/Cryptography/HmacSha3512Hasher.cs`
- Test: `backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Security/Cryptography/HmacHasherTests.cs`

- [ ] **Step 1: Write failing HMAC tests**

Create tests:

```csharp
using FluentAssertions;
using Tw.Core.Security.Cryptography;
using Tw.TestBase;

namespace Tw.Core.Tests.Security.Cryptography;

public sealed class HmacHasherTests
{
    [Theory]
    [InlineData(nameof(HmacMd5Hasher), CryptoTestVectors.HmacMd5Fox)]
    [InlineData(nameof(HmacSha1Hasher), CryptoTestVectors.HmacSha1Fox)]
    [InlineData(nameof(HmacSha256Hasher), CryptoTestVectors.HmacSha256Fox)]
    [InlineData(nameof(HmacSha384Hasher), CryptoTestVectors.HmacSha384Fox)]
    [InlineData(nameof(HmacSha512Hasher), CryptoTestVectors.HmacSha512Fox)]
    [InlineData(nameof(HmacSha3256Hasher), CryptoTestVectors.HmacSha3256Fox)]
    [InlineData(nameof(HmacSha3384Hasher), CryptoTestVectors.HmacSha3384Fox)]
    [InlineData(nameof(HmacSha3512Hasher), CryptoTestVectors.HmacSha3512Fox)]
    public void ComputeHash_Returns_Known_Vector(string hasherName, string expected)
    {
        var actual = hasherName switch
        {
            nameof(HmacMd5Hasher) => HmacMd5Hasher.ComputeHash(TestBytes.HmacKey, TestBytes.LongText),
            nameof(HmacSha1Hasher) => HmacSha1Hasher.ComputeHash(TestBytes.HmacKey, TestBytes.LongText),
            nameof(HmacSha256Hasher) => HmacSha256Hasher.ComputeHash(TestBytes.HmacKey, TestBytes.LongText),
            nameof(HmacSha384Hasher) => HmacSha384Hasher.ComputeHash(TestBytes.HmacKey, TestBytes.LongText),
            nameof(HmacSha512Hasher) => HmacSha512Hasher.ComputeHash(TestBytes.HmacKey, TestBytes.LongText),
            nameof(HmacSha3256Hasher) => HmacSha3256Hasher.ComputeHash(TestBytes.HmacKey, TestBytes.LongText),
            nameof(HmacSha3384Hasher) => HmacSha3384Hasher.ComputeHash(TestBytes.HmacKey, TestBytes.LongText),
            nameof(HmacSha3512Hasher) => HmacSha3512Hasher.ComputeHash(TestBytes.HmacKey, TestBytes.LongText),
            _ => throw new InvalidOperationException(hasherName)
        };

        actual.Should().Be(expected);
    }

    [Fact]
    public void VerifyHash_Returns_True_For_Matching_Hmac()
    {
        HmacSha256Hasher.VerifyHash(TestBytes.HmacKey, TestBytes.LongText, CryptoTestVectors.HmacSha256Fox)
            .Should().BeTrue();
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

```powershell
dotnet test .\BuildingBlocks\tests\Tw.Core.Tests\Tw.Core.Tests.csproj --no-restore --filter FullyQualifiedName~HmacHasherTests
```

Expected: tests fail because HMAC classes are missing.

- [ ] **Step 3: Implement HMAC classes**

Port behavior from old `HMAC*.cs` files. Use the same method families as non-HMAC hashers, with `key` as the first parameter:

```csharp
public static string ComputeHash(string key, string input, bool useUpperCase = false, Encoding? encoding = null);
public static string ComputeHash(byte[] key, byte[] bytes, bool useUpperCase = false);
public static Task<string> ComputeFileHashAsync(string key, string filePath, bool useUpperCase = false, Encoding? encoding = null, CancellationToken cancellationToken = default);
public static Task<string> ComputeFileHashAsync(byte[] key, string filePath, bool useUpperCase = false, CancellationToken cancellationToken = default);
public static Task<string> ComputeFileHashAsync(string key, Stream stream, bool useUpperCase = false, Encoding? encoding = null, CancellationToken cancellationToken = default);
public static Task<string> ComputeFileHashAsync(byte[] key, Stream stream, bool useUpperCase = false, CancellationToken cancellationToken = default);
public static bool VerifyHash(string key, string input, string hash, Encoding? encoding = null);
public static bool VerifyHash(byte[] key, byte[] bytes, string hash);
```

`HmacMd5Hasher` also supports `useShortHash` in the same positions as `Md5Hasher`.

- [ ] **Step 4: Run focused tests**

```powershell
dotnet test .\BuildingBlocks\tests\Tw.Core.Tests\Tw.Core.Tests.csproj --no-restore --filter FullyQualifiedName~HmacHasherTests
```

Expected: tests pass.

- [ ] **Step 5: Commit**

```powershell
git add BuildingBlocks/src/Tw.Core/Security/Cryptography/Hmac*Hasher.cs `
        BuildingBlocks/tests/Tw.Core.Tests/Security/Cryptography/HmacHasherTests.cs
git commit -m "feat: add HMAC hashers"
```

---

### Task 11: Migrate Symmetric Cryptography

**Files:**
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/Security/Cryptography/AesCryptography.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/Security/Cryptography/DesCryptography.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/Security/Cryptography/TripleDesCryptography.cs`
- Test: `backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Security/Cryptography/SymmetricCryptographyTests.cs`

- [ ] **Step 1: Write failing symmetric encryption tests**

Create tests:

```csharp
using FluentAssertions;
using System.Security.Cryptography;
using Tw.Core.Extensions;
using Tw.Core.Security.Cryptography;
using Tw.TestBase;

namespace Tw.Core.Tests.Security.Cryptography;

public sealed class SymmetricCryptographyTests
{
    [Fact]
    public void AesCryptography_RoundTrips_String()
    {
        var encrypted = AesCryptography.Encrypt(TestBytes.LongText, TestBytes.AesKey16);

        var decrypted = AesCryptography.Decrypt(encrypted, TestBytes.AesKey16);

        decrypted.Should().Be(TestBytes.LongText);
    }

    [Fact]
    public void AesCryptography_Rejects_Invalid_Key_Length()
    {
        var act = () => AesCryptography.Encrypt(TestBytes.AbcUtf8, [1, 2, 3]);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void DesCryptography_RoundTrips_String()
    {
        var encrypted = DesCryptography.Encrypt(TestBytes.LongText, TestBytes.DesKey8);
        DesCryptography.Decrypt(encrypted, TestBytes.DesKey8).Should().Be(TestBytes.LongText);
    }

    [Fact]
    public void TripleDesCryptography_RoundTrips_String()
    {
        var encrypted = TripleDesCryptography.Encrypt(TestBytes.LongText, TestBytes.TripleDesKey24);
        TripleDesCryptography.Decrypt(encrypted, TestBytes.TripleDesKey24).Should().Be(TestBytes.LongText);
    }

    [Fact]
    public async Task AesCryptography_RoundTrips_Stream()
    {
        await using var stream = TestStreams.FromText(TestBytes.LongText);

        var encrypted = await AesCryptography.EncryptFileAsync(stream, TestBytes.AesKey16.GetBytes());
        await using var encryptedStream = new MemoryStream(encrypted);
        var decrypted = await AesCryptography.DecryptFileAsync(encryptedStream, TestBytes.AesKey16.GetBytes());

        decrypted.Should().Equal(TestBytes.LongTextUtf8);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

```powershell
dotnet test .\BuildingBlocks\tests\Tw.Core.Tests\Tw.Core.Tests.csproj --no-restore --filter FullyQualifiedName~SymmetricCryptographyTests
```

Expected: tests fail because cryptography classes are missing.

- [ ] **Step 3: Implement symmetric cryptography APIs**

Port old `AESEncryption.cs`, `DESEncryption.cs`, and `TripleDESEncryption.cs` into these classes:

```csharp
public static class AesCryptography;
public static class DesCryptography;
public static class TripleDesCryptography;
```

Expose these methods on each class:

```csharp
public static string Encrypt(string input, string key, byte[]? iv = null, CipherMode mode = CipherMode.CBC, PaddingMode padding = PaddingMode.PKCS7, bool isKeyBase64 = false, Encoding? encoding = null);
public static byte[] Encrypt(byte[] bytes, byte[] key, byte[]? iv = null, CipherMode mode = CipherMode.CBC, PaddingMode padding = PaddingMode.PKCS7);
public static string Decrypt(string input, string key, byte[]? iv = null, CipherMode mode = CipherMode.CBC, PaddingMode padding = PaddingMode.PKCS7, bool isKeyBase64 = false, Encoding? encoding = null);
public static byte[] Decrypt(byte[] bytes, byte[] key, byte[]? iv = null, CipherMode mode = CipherMode.CBC, PaddingMode padding = PaddingMode.PKCS7);
public static Task<byte[]> EncryptFileAsync(string filePath, byte[] key, byte[]? iv = null, CipherMode mode = CipherMode.CBC, PaddingMode padding = PaddingMode.PKCS7, CancellationToken cancellationToken = default);
public static Task<byte[]> EncryptFileAsync(Stream stream, byte[] key, byte[]? iv = null, CipherMode mode = CipherMode.CBC, PaddingMode padding = PaddingMode.PKCS7, CancellationToken cancellationToken = default);
public static Task<byte[]> DecryptFileAsync(string filePath, byte[] key, byte[]? iv = null, CipherMode mode = CipherMode.CBC, PaddingMode padding = PaddingMode.PKCS7, CancellationToken cancellationToken = default);
public static Task<byte[]> DecryptFileAsync(Stream stream, byte[] key, byte[]? iv = null, CipherMode mode = CipherMode.CBC, PaddingMode padding = PaddingMode.PKCS7, CancellationToken cancellationToken = default);
```

Key and IV boundaries:

- AES key: 16, 24, or 32 bytes; IV: 16 bytes.
- DES key: 8 bytes; IV: 8 bytes.
- TripleDES key: 16 or 24 bytes; IV: 8 bytes.
- In non-ECB mode with `iv == null`, prefix generated IV to ciphertext and extract it during decrypt.
- If `DES.Create()` or `TripleDES.Create()` produces obsolete compiler diagnostics in .NET 10, stop after recording the exact diagnostic and replace with a non-obsolete BCL-supported path before committing.

- [ ] **Step 4: Run focused tests**

```powershell
dotnet test .\BuildingBlocks\tests\Tw.Core.Tests\Tw.Core.Tests.csproj --no-restore --filter FullyQualifiedName~SymmetricCryptographyTests
```

Expected: tests pass and build has no obsolete warning suppressions.

- [ ] **Step 5: Commit**

```powershell
git add BuildingBlocks/src/Tw.Core/Security/Cryptography/AesCryptography.cs `
        BuildingBlocks/src/Tw.Core/Security/Cryptography/DesCryptography.cs `
        BuildingBlocks/src/Tw.Core/Security/Cryptography/TripleDesCryptography.cs `
        BuildingBlocks/tests/Tw.Core.Tests/Security/Cryptography/SymmetricCryptographyTests.cs
git commit -m "feat: add symmetric cryptography helpers"
```

---

### Task 12: Migrate RSA And PBKDF2

**Files:**
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/Security/Cryptography/RsaKeyPair.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/Security/Cryptography/RsaDerKeyPair.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/Security/Cryptography/RsaCryptography.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/Security/Cryptography/Pbkdf2PasswordHasher.cs`
- Test: `backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Security/Cryptography/RsaCryptographyTests.cs`
- Test: `backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Security/Cryptography/Pbkdf2PasswordHasherTests.cs`

- [ ] **Step 1: Write failing RSA tests**

Create tests:

```csharp
using FluentAssertions;
using Tw.Core.Security.Cryptography;
using Tw.TestBase;

namespace Tw.Core.Tests.Security.Cryptography;

public sealed class RsaCryptographyTests
{
    [Fact]
    public void RsaCryptography_RoundTrips_String_With_Pem_Key()
    {
        var keys = RsaCryptography.GenerateKeyPair();

        var encrypted = RsaCryptography.Encrypt(TestBytes.LongText, keys.PublicKeyPem);
        var decrypted = RsaCryptography.Decrypt(encrypted, keys.PrivateKeyPem);

        decrypted.Should().Be(TestBytes.LongText);
    }

    [Fact]
    public void RsaCryptography_Signs_And_Verifies()
    {
        var keys = RsaCryptography.GenerateKeyPair();

        var signature = RsaCryptography.Sign(TestBytes.LongText, keys.PrivateKeyPem);

        RsaCryptography.VerifySignature(TestBytes.LongText, signature, keys.PublicKeyPem).Should().BeTrue();
        RsaCryptography.VerifySignature("changed", signature, keys.PublicKeyPem).Should().BeFalse();
    }

    [Fact]
    public void RsaCryptography_RoundTrips_Bytes_With_Der_Key()
    {
        var keys = RsaCryptography.GenerateDerKeyPair();

        var encrypted = RsaCryptography.Encrypt(TestBytes.AbcUtf8, keys.PublicKeyDer);
        var decrypted = RsaCryptography.Decrypt(encrypted, keys.PrivateKeyDer);

        decrypted.Should().Equal(TestBytes.AbcUtf8);
    }
}
```

- [ ] **Step 2: Write failing PBKDF2 tests**

Create tests:

```csharp
using System.Security.Cryptography;
using FluentAssertions;
using Tw.Core.Security.Cryptography;
using Tw.TestBase;

namespace Tw.Core.Tests.Security.Cryptography;

public sealed class Pbkdf2PasswordHasherTests
{
    [Fact]
    public void DeriveKey_Returns_Deterministic_Key_For_Salt()
    {
        var salt = Convert.FromBase64String("c2FsdHNhbHRzYWx0MTIzNA==");

        var first = Pbkdf2PasswordHasher.DeriveKey("password", salt, iterations: 10_000, keyLength: 32, HashAlgorithmName.SHA256);
        var second = Pbkdf2PasswordHasher.DeriveKey("password", salt, iterations: 10_000, keyLength: 32, HashAlgorithmName.SHA256);

        first.Should().Be(second);
    }

    [Fact]
    public void HashPassword_And_VerifyPassword_RoundTrip()
    {
        var hashed = Pbkdf2PasswordHasher.HashPassword("correct horse battery staple");

        Pbkdf2PasswordHasher.VerifyPassword("correct horse battery staple", hashed).Should().BeTrue();
        Pbkdf2PasswordHasher.VerifyPassword("wrong", hashed).Should().BeFalse();
    }

    [Fact]
    public void GenerateSalt_Rejects_Short_Length()
    {
        var act = () => Pbkdf2PasswordHasher.GenerateSalt(7);
        act.Should().Throw<ArgumentException>();
    }
}
```

- [ ] **Step 3: Run tests to verify they fail**

```powershell
dotnet test .\BuildingBlocks\tests\Tw.Core.Tests\Tw.Core.Tests.csproj --no-restore --filter "FullyQualifiedName~RsaCryptographyTests|FullyQualifiedName~Pbkdf2PasswordHasherTests"
```

Expected: tests fail because types are missing.

- [ ] **Step 4: Implement RSA APIs**

Create key records:

```csharp
namespace Tw.Core.Security.Cryptography;

public readonly record struct RsaKeyPair(string PublicKeyPem, string PrivateKeyPem);
public readonly record struct RsaDerKeyPair(byte[] PublicKeyDer, byte[] PrivateKeyDer);
```

Implement `RsaCryptography`:

```csharp
public static RsaKeyPair GenerateKeyPair(int keySize = 2048);
public static RsaDerKeyPair GenerateDerKeyPair(int keySize = 2048);
public static string Encrypt(string input, string publicKeyPem, RSAEncryptionPadding? padding = null, Encoding? encoding = null);
public static byte[] Encrypt(byte[] bytes, string publicKeyPem, RSAEncryptionPadding? padding = null);
public static byte[] Encrypt(byte[] bytes, byte[] publicKeyDer, RSAEncryptionPadding? padding = null);
public static string Decrypt(string input, string privateKeyPem, RSAEncryptionPadding? padding = null, Encoding? encoding = null);
public static byte[] Decrypt(byte[] bytes, string privateKeyPem, RSAEncryptionPadding? padding = null);
public static byte[] Decrypt(byte[] bytes, byte[] privateKeyDer, RSAEncryptionPadding? padding = null);
public static string Sign(string input, string privateKeyPem, HashAlgorithmName? hashAlgorithm = null, RSASignaturePadding? padding = null, Encoding? encoding = null);
public static byte[] Sign(byte[] bytes, string privateKeyPem, HashAlgorithmName? hashAlgorithm = null, RSASignaturePadding? padding = null);
public static bool VerifySignature(string input, string signature, string publicKeyPem, HashAlgorithmName? hashAlgorithm = null, RSASignaturePadding? padding = null, Encoding? encoding = null);
public static bool VerifySignature(byte[] bytes, byte[] signature, string publicKeyPem, HashAlgorithmName? hashAlgorithm = null, RSASignaturePadding? padding = null);
```

Use `RSA.Create()`, `ExportRSAPublicKeyPem`, `ExportRSAPrivateKeyPem`, `ExportRSAPublicKey`, `ExportRSAPrivateKey`, `ImportFromPem`, `ImportRSAPublicKey`, and `ImportRSAPrivateKey`. Do not use XML key import/export.

- [ ] **Step 5: Implement PBKDF2 APIs**

Port old `PBKDF2Encryption.cs` into `Pbkdf2PasswordHasher`:

```csharp
public static string DeriveKey(string password, byte[] salt, int iterations = 100000, int keyLength = 32, HashAlgorithmName? hashAlgorithm = null, Encoding? encoding = null);
public static byte[] DeriveKey(byte[] password, byte[] salt, int iterations = 100000, int keyLength = 32, HashAlgorithmName? hashAlgorithm = null);
public static string GenerateSalt(int length = 16);
public static byte[] GenerateSaltBytes(int length = 16);
public static string HashPassword(string password, int iterations = 100000, int keyLength = 32, int saltLength = 16, HashAlgorithmName? hashAlgorithm = null, Encoding? encoding = null);
public static bool VerifyPassword(string password, string hashedPassword, int iterations = 100000, HashAlgorithmName? hashAlgorithm = null, Encoding? encoding = null);
public static string DeriveKeyToHex(string password, byte[] salt, int iterations = 100000, int keyLength = 32, bool useUpperCase = false, HashAlgorithmName? hashAlgorithm = null, Encoding? encoding = null);
```

Use `Rfc2898DeriveBytes.Pbkdf2` and `CryptographicOperations.FixedTimeEquals`.

- [ ] **Step 6: Run focused tests**

```powershell
dotnet test .\BuildingBlocks\tests\Tw.Core.Tests\Tw.Core.Tests.csproj --no-restore --filter "FullyQualifiedName~RsaCryptographyTests|FullyQualifiedName~Pbkdf2PasswordHasherTests"
```

Expected: tests pass.

- [ ] **Step 7: Commit**

```powershell
git add BuildingBlocks/src/Tw.Core/Security/Cryptography/RsaKeyPair.cs `
        BuildingBlocks/src/Tw.Core/Security/Cryptography/RsaDerKeyPair.cs `
        BuildingBlocks/src/Tw.Core/Security/Cryptography/RsaCryptography.cs `
        BuildingBlocks/src/Tw.Core/Security/Cryptography/Pbkdf2PasswordHasher.cs `
        BuildingBlocks/tests/Tw.Core.Tests/Security/Cryptography/RsaCryptographyTests.cs `
        BuildingBlocks/tests/Tw.Core.Tests/Security/Cryptography/Pbkdf2PasswordHasherTests.cs
git commit -m "feat: add RSA and PBKDF2 cryptography helpers"
```

---

### Task 13: Add String Cryptography Extensions

**Files:**
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/Security/Cryptography/StringCryptographyExtensions.cs`
- Test: `backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Security/Cryptography/StringCryptographyExtensionsTests.cs`

- [ ] **Step 1: Write failing tests**

Create tests:

```csharp
using FluentAssertions;
using Tw.Core.Security.Cryptography;
using Tw.TestBase;

namespace Tw.Core.Tests.Security.Cryptography;

public sealed class StringCryptographyExtensionsTests
{
    [Fact]
    public void Hash_Convenience_Methods_Use_Backend_Hashers()
    {
        TestBytes.Text.ComputeSha256Hash().Should().Be(CryptoTestVectors.Sha256Abc);
        TestBytes.Text.VerifySha256Hash(CryptoTestVectors.Sha256Abc).Should().BeTrue();
    }

    [Fact]
    public void Hmac_Convenience_Methods_Use_Backend_Hashers()
    {
        TestBytes.LongText.ComputeHmacSha256Hash(TestBytes.HmacKey).Should().Be(CryptoTestVectors.HmacSha256Fox);
        TestBytes.LongText.VerifyHmacSha256Hash(TestBytes.HmacKey, CryptoTestVectors.HmacSha256Fox).Should().BeTrue();
    }

    [Fact]
    public void Aes_Convenience_Methods_RoundTrip()
    {
        var encrypted = TestBytes.LongText.EncryptWithAes(TestBytes.AesKey16);

        encrypted.DecryptWithAes(TestBytes.AesKey16).Should().Be(TestBytes.LongText);
    }

    [Fact]
    public void Rsa_Convenience_Methods_Sign_And_Verify()
    {
        var keys = RsaCryptography.GenerateKeyPair();

        var signature = TestBytes.LongText.SignWithRsa(keys.PrivateKeyPem);

        TestBytes.LongText.VerifyRsaSignature(signature, keys.PublicKeyPem).Should().BeTrue();
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

```powershell
dotnet test .\BuildingBlocks\tests\Tw.Core.Tests\Tw.Core.Tests.csproj --no-restore --filter FullyQualifiedName~StringCryptographyExtensionsTests
```

Expected: tests fail because extension methods are missing.

- [ ] **Step 3: Implement extension methods**

Create behavior-backed extension methods in `Tw.Core.Security.Cryptography.StringCryptographyExtensions`.

Hashing:

```csharp
ComputeMd5Hash, ComputeSha1Hash, ComputeSha256Hash, ComputeSha384Hash, ComputeSha512Hash, ComputeSha3256Hash, ComputeSha3384Hash, ComputeSha3512Hash
VerifyMd5Hash, VerifySha1Hash, VerifySha256Hash, VerifySha384Hash, VerifySha512Hash, VerifySha3256Hash, VerifySha3384Hash, VerifySha3512Hash
```

HMAC:

```csharp
ComputeHmacMd5Hash, ComputeHmacSha1Hash, ComputeHmacSha256Hash, ComputeHmacSha384Hash, ComputeHmacSha512Hash, ComputeHmacSha3256Hash, ComputeHmacSha3384Hash, ComputeHmacSha3512Hash
VerifyHmacMd5Hash, VerifyHmacSha1Hash, VerifyHmacSha256Hash, VerifyHmacSha384Hash, VerifyHmacSha512Hash, VerifyHmacSha3256Hash, VerifyHmacSha3384Hash, VerifyHmacSha3512Hash
```

Reversible encryption, signatures, and passwords:

```csharp
EncryptWithAes, DecryptWithAes, EncryptWithDes, DecryptWithDes, EncryptWithTripleDes, DecryptWithTripleDes, EncryptWithRsa, DecryptWithRsa
SignWithRsa, VerifyRsaSignature, HashPasswordWithPbkdf2, VerifyPbkdf2Password
```

Each method delegates to the corresponding public class from Tasks 9 through 12 and keeps the same optional parameters where they are useful to callers.

- [ ] **Step 4: Run focused tests**

```powershell
dotnet test .\BuildingBlocks\tests\Tw.Core.Tests\Tw.Core.Tests.csproj --no-restore --filter FullyQualifiedName~StringCryptographyExtensionsTests
```

Expected: tests pass.

- [ ] **Step 5: Commit**

```powershell
git add BuildingBlocks/src/Tw.Core/Security/Cryptography/StringCryptographyExtensions.cs `
        BuildingBlocks/tests/Tw.Core.Tests/Security/Cryptography/StringCryptographyExtensionsTests.cs
git commit -m "feat: add string cryptography extensions"
```

---

### Task 14: Final Verification And Migration Audit

**Files:**
- Review: `backend/dotnet/BuildingBlocks/src/Tw.Core/**/*.cs`
- Review: `backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/**/*.cs`
- Review: `backend/dotnet/BuildingBlocks/tests/Tw.TestBase/**/*.cs`
- Review: `backend/dotnet/Build/Packages.Tests.props`
- Review: `backend/dotnet/Tw.SmartPlatform.slnx`

- [ ] **Step 1: Search for excluded names and namespaces**

Run from repo root:

```powershell
rg -n "Nebulaxis|DependencyInjection|DynamicProxy|Autofac|AspectCore|Castle|Interceptor|ErrorDefinition|TenantId|CoreException|ECCEncryption|StringEncryptionExtensions" backend/dotnet/BuildingBlocks/src/Tw.Core backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests backend/dotnet/BuildingBlocks/tests/Tw.TestBase
```

Expected: no output, except test names or comments only when they explicitly assert absence. Remove any production reference found by this search.

- [ ] **Step 2: Search for obsolete warning suppressions**

Run:

```powershell
rg -n "SYSLIB|Obsolete|#pragma warning disable|NoWarn" backend/dotnet/BuildingBlocks/src/Tw.Core backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests
```

Expected: no output. Replace suppressed obsolete paths with non-obsolete BCL APIs.

- [ ] **Step 3: Restore**

Run from `backend/dotnet`:

```powershell
dotnet restore .\Tw.SmartPlatform.slnx
```

Expected: restore succeeds and lock files are current.

- [ ] **Step 4: Build**

Run:

```powershell
dotnet build .\Tw.SmartPlatform.slnx --no-restore
```

Expected: build succeeds with no nullable errors and no obsolete diagnostics.

- [ ] **Step 5: Run Tw.Core tests**

Run:

```powershell
dotnet test .\BuildingBlocks\tests\Tw.Core.Tests\Tw.Core.Tests.csproj --no-build
```

Expected: all `Tw.Core.Tests` tests pass.

- [ ] **Step 6: Run solution tests**

Run:

```powershell
dotnet test .\Tw.SmartPlatform.slnx --no-build
```

Expected: all discovered solution tests pass. If only `Tw.Core.Tests` exists under `BuildingBlocks/tests`, this still verifies solution wiring.

- [ ] **Step 7: Inspect git status**

Run from repo root:

```powershell
git status --short
```

Expected: only intended migration files, test files, package props, solution, and lock files are changed.

- [ ] **Step 8: Commit final verification fixes**

If Steps 1 through 7 required changes, commit them:

```powershell
git add backend/dotnet/BuildingBlocks/src/Tw.Core `
        backend/dotnet/BuildingBlocks/tests/Tw.TestBase `
        backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests `
        backend/dotnet/Build/Packages.Tests.props `
        backend/dotnet/Tw.SmartPlatform.slnx
git commit -m "test: verify Tw.Core migration"
```

Expected: commit succeeds when verification changed files. If no files changed, `git status --short` contains no uncommitted migration work and no final commit is needed.

## Self-Review

Spec coverage:

- Analyze all old source under `D:\WorkSpaces\Nebulaxis_Old\src`: covered by Source And Scope and Task 14 search audit.
- Migrate approved `Nebulaxis.Core` capabilities excluding DI/AOP: covered by Tasks 3 through 8 and Task 14 exclusion search.
- Migrate approved `Nebulaxis.Security` cryptography: covered by Tasks 9 through 13.
- Rename namespaces/classes/methods to `Tw.*`: covered by File Structure, each task namespace, and Task 14 search audit.
- Use non-obsolete .NET 10 APIs: covered by Tasks 9 through 12 and Task 14 obsolete search/build.
- Create `Tw.TestBase` before `Tw.Core.Tests`: covered by Task 1 order and Task 2.
- Use central stable package versions: covered by Package Versions and Task 1.
- Exclude `ErrorDefinition`, DI, AOP, and ECC stub: covered by Source And Scope and Task 14 search audit.
- Remove `TenantId` from `ICurrentUser`: covered by Task 3 tests.
- Add `StringCryptographyExtensions` as new behavior-backed API: covered by Task 13.

Placeholder scan:

- The plan contains concrete paths, commands, expected results, public API names, and test examples.
- The implementation tasks intentionally reference old source file paths as behavior references because this is a migration plan and the source material is local and fixed.

Type consistency:

- `NamedValue` replaces old `NameValue`.
- `TwConfigurationException` derives from `TwException`.
- Hashers consistently use `ComputeHash`, `ComputeFileHashAsync`, and `VerifyHash`.
- HMAC hashers use the same method names with `key` as the first parameter.
- `RsaCryptography.VerifySignature` matches `StringCryptographyExtensions.VerifyRsaSignature`.
- Symmetric classes consistently use `AesCryptography`, `DesCryptography`, and `TripleDesCryptography`.
