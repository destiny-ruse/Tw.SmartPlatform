# Dotnet Framework P1 Foundation Runtime Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Split and implement the foundational runtime packages used by all later framework capabilities.

**Architecture:** Foundation packages stay independent from ASP.NET Core, ORM, CAP, Autofac, Castle, Quartz, Redis, and tests. Existing `Tw.Core` content is reduced to core primitives and exceptions while timing, threading, security, JSON, validation, UoW, ID generation, text templating, and Excel become separate packages. `Tw.Security` owns masking and write-back protection; observability, auditing, export, and error responses consume those security contracts.

**Tech Stack:** .NET 10 class libraries, xUnit, AwesomeAssertions, Newtonsoft.Json, Yitter.IdGenerator, Scriban 7.2.5, MiniExcel 1.45.0, DocumentFormat.OpenXml 3.5.1, Microsoft.Extensions.DependencyInjection.Abstractions

---

## File Structure

- Modify: `backend/dotnet/BuildingBlocks/src/Foundation/Tw.Core`
- Create: `backend/dotnet/BuildingBlocks/src/Foundation/Tw.Threading`
- Create: `backend/dotnet/BuildingBlocks/src/Foundation/Tw.Timing`
- Create: `backend/dotnet/BuildingBlocks/src/Foundation/Tw.ExceptionHandling`
- Create: `backend/dotnet/BuildingBlocks/src/Foundation/Tw.Security`
- Create: `backend/dotnet/BuildingBlocks/src/Foundation/Tw.Json.Abstractions`
- Create: `backend/dotnet/BuildingBlocks/src/Foundation/Tw.Json.Newtonsoft`
- Create: `backend/dotnet/BuildingBlocks/src/Foundation/Tw.Validation.Abstractions`
- Create: `backend/dotnet/BuildingBlocks/src/Foundation/Tw.Uow`
- Create: `backend/dotnet/BuildingBlocks/src/IdGeneration/Tw.IdGeneration`
- Create: `backend/dotnet/BuildingBlocks/src/IdGeneration/Tw.IdGeneration.Yitter`
- Create: `backend/dotnet/BuildingBlocks/src/TextTemplating/Tw.TextTemplating`
- Create: `backend/dotnet/BuildingBlocks/src/TextTemplating/Tw.TextTemplating.Scriban`
- Create: `backend/dotnet/BuildingBlocks/src/Excel/Tw.Excel`
- Create: `backend/dotnet/BuildingBlocks/src/Excel/Tw.Excel.MiniExcel`
- Create matching tests under `backend/dotnet/BuildingBlocks/tests/<Package>.Tests`
- Modify: `backend/dotnet/Tw.SmartPlatform.slnx`

### Task 1: Create Foundation Package Shells

**Files:**
- Create: listed package directories and `.csproj` files
- Create: matching `package-charter.yaml` files
- Create: matching test projects

- [ ] **Step 1: Create the standard runtime project template**

Use this `.csproj` content for abstraction packages without third-party dependencies:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>true</IsPackable>
  </PropertyGroup>
</Project>
```

- [ ] **Step 2: Create the standard test project template**

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
    <PackageReference Include="xunit.v3" />
    <PackageReference Include="xunit.runner.visualstudio" />
    <PackageReference Include="AwesomeAssertions" />
  </ItemGroup>
</Project>
```

- [ ] **Step 3: Add package charters**

Use this exact charter shape for `Tw.Threading`, changing package name and scope per package:

```yaml
schema_version: "1.0.0"
package: Tw.Threading
owner: platform-team
stability: experimental
compatibility: "experimental 阶段不承诺兼容"
responsibility: >
  取消令牌、异步辅助、后台执行上下文和线程安全工具。
in_scope:
  - 取消令牌 provider
  - 取消令牌覆盖作用域
  - 异步释放辅助
  - 线程安全工具
out_of_scope:
  - HTTP 请求上下文
  - 后台任务调度
  - 业务线程模型
public_capabilities:
  - Tw.Threading
dependency_rules:
  forbid:
    - "Microsoft.AspNetCore.*"
    - "SqlSugar*"
    - "DotNetCore.CAP*"
  allow:
    - "Microsoft.Extensions.DependencyInjection.Abstractions"
```

- [ ] **Step 4: Add projects to solution**

For each package and test project, add a `<Project Path="...">` entry under the matching `/BuildingBlocks/src/<Capability>/` or `/BuildingBlocks/tests/` folder in `backend/dotnet/Tw.SmartPlatform.slnx`.

- [ ] **Step 5: Build all new empty package shells**

Run: `dotnet build backend/dotnet/Tw.SmartPlatform.slnx`

Expected: build succeeds after all `ProjectReference` paths are valid.

- [ ] **Step 6: Commit**

```bash
git add backend/dotnet/BuildingBlocks/src backend/dotnet/BuildingBlocks/tests backend/dotnet/Tw.SmartPlatform.slnx
git commit -m "feat: add foundation runtime package shells"
```

### Task 2: Move Cancellation And Async Utilities From Tw.Core To Tw.Threading

**Files:**
- Move: `backend/dotnet/BuildingBlocks/src/Foundation/Tw.Core/Context/*Cancellation*` to `backend/dotnet/BuildingBlocks/src/Foundation/Tw.Threading/Cancellation`
- Move: `backend/dotnet/BuildingBlocks/src/Foundation/Tw.Core/Utilities/AsyncDisposeFunc.cs` to `backend/dotnet/BuildingBlocks/src/Foundation/Tw.Threading/Async`
- Move: `backend/dotnet/BuildingBlocks/src/Foundation/Tw.Core/Utilities/NullAsyncDisposable.cs` to `backend/dotnet/BuildingBlocks/src/Foundation/Tw.Threading/Async`
- Create: `backend/dotnet/BuildingBlocks/tests/Tw.Threading.Tests/Cancellation/CancellationTokenProviderTests.cs`

- [ ] **Step 1: Write the failing test**

```csharp
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Tw.Threading;

namespace Tw.Threading.Tests.Cancellation;

public sealed class CancellationTokenProviderTests
{
    [Fact]
    public void AddCancellationTokenProvider_RegistersDefaultProvider()
    {
        var services = new ServiceCollection();

        services.AddCancellationTokenProvider();

        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<ICancellationTokenProvider>()
            .Token
            .Should()
            .Be(CancellationToken.None);
    }
}
```

- [ ] **Step 2: Run the test to verify it fails**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Threading.Tests/Tw.Threading.Tests.csproj --filter CancellationTokenProviderTests`

Expected: FAIL because `Tw.Threading` does not expose the moved provider yet.

- [ ] **Step 3: Move the implementation and namespace**

Change moved files to use:

```csharp
namespace Tw.Threading;
```

The public extension method remains:

```csharp
public static IServiceCollection AddCancellationTokenProvider(this IServiceCollection services)
```

- [ ] **Step 4: Update references**

Replace `using Tw.Context;` with:

```csharp
using Tw.Threading;
```

- [ ] **Step 5: Run tests**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Threading.Tests/Tw.Threading.Tests.csproj`

Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add backend/dotnet/BuildingBlocks/src/Foundation/Tw.Core backend/dotnet/BuildingBlocks/src/Foundation/Tw.Threading backend/dotnet/BuildingBlocks/tests
git commit -m "refactor: move cancellation primitives to threading package"
```

### Task 3: Create Timing Package

**Files:**
- Create: `backend/dotnet/BuildingBlocks/src/Foundation/Tw.Timing/IClock.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Foundation/Tw.Timing/SystemClock.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Foundation/Tw.Timing/FixedClock.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Foundation/Tw.Timing/TimingServiceCollectionExtensions.cs`
- Create: `backend/dotnet/BuildingBlocks/tests/Tw.Timing.Tests/SystemClockTests.cs`

- [ ] **Step 1: Write tests**

```csharp
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Tw.Timing;

namespace Tw.Timing.Tests;

public sealed class SystemClockTests
{
    [Fact]
    public void FixedClock_ReturnsConfiguredInstant()
    {
        var instant = new DateTimeOffset(2026, 7, 9, 10, 0, 0, TimeSpan.FromHours(8));
        IClock clock = new FixedClock(instant);

        clock.Now.Should().Be(instant);
    }

    [Fact]
    public void AddTiming_RegistersSystemClock()
    {
        var services = new ServiceCollection();

        services.AddTiming();

        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<IClock>().Should().BeOfType<SystemClock>();
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Timing.Tests/Tw.Timing.Tests.csproj --filter SystemClockTests`

Expected: FAIL because `IClock` and implementations do not exist.

- [ ] **Step 3: Implement timing types**

```csharp
namespace Tw.Timing;

public interface IClock
{
    DateTimeOffset Now { get; }
}

public sealed class SystemClock : IClock
{
    public DateTimeOffset Now => DateTimeOffset.UtcNow;
}

public sealed class FixedClock(DateTimeOffset now) : IClock
{
    public DateTimeOffset Now { get; } = now;
}
```

- [ ] **Step 4: Implement DI registration**

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Tw.Timing;

public static class TimingServiceCollectionExtensions
{
    public static IServiceCollection AddTiming(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddSingleton<IClock, SystemClock>();
        return services;
    }
}
```

- [ ] **Step 5: Run tests**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Timing.Tests/Tw.Timing.Tests.csproj`

Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add backend/dotnet/BuildingBlocks/src/Foundation/Tw.Timing backend/dotnet/BuildingBlocks/tests/Tw.Timing.Tests
git commit -m "feat: add timing primitives"
```

### Task 4: Create Exception Handling And Validation Abstractions

**Files:**
- Create: `backend/dotnet/BuildingBlocks/src/Foundation/Tw.ExceptionHandling/ErrorDescriptor.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Foundation/Tw.ExceptionHandling/IExceptionToErrorMapper.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Foundation/Tw.ExceptionHandling/DefaultExceptionToErrorMapper.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Foundation/Tw.Validation.Abstractions/ValidationError.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Foundation/Tw.Validation.Abstractions/ValidationException.cs`
- Create: tests under `Tw.ExceptionHandling.Tests` and `Tw.Validation.Abstractions.Tests`

- [ ] **Step 1: Write exception mapping test**

```csharp
using AwesomeAssertions;
using Tw.ExceptionHandling;

namespace Tw.ExceptionHandling.Tests;

public sealed class DefaultExceptionToErrorMapperTests
{
    [Fact]
    public void Map_UnknownException_ReturnsSystemError()
    {
        var mapper = new DefaultExceptionToErrorMapper();

        var error = mapper.Map(new InvalidOperationException("boom"));

        error.Code.Should().Be("SYSTEM:999999");
        error.Message.Should().Be("系统异常");
        error.Category.Should().Be(ErrorCategory.System);
    }
}
```

- [ ] **Step 2: Write validation exception test**

```csharp
using AwesomeAssertions;
using Tw.Validation.Abstractions;

namespace Tw.Validation.Abstractions.Tests;

public sealed class ValidationExceptionTests
{
    [Fact]
    public void Constructor_StoresValidationErrors()
    {
        var errors = new[] { new ValidationError("name", "VALIDATION:000001", "名称不能为空") };

        var exception = new ValidationException(errors);

        exception.Errors.Should().ContainSingle().Which.FieldPath.Should().Be("name");
    }
}
```

- [ ] **Step 3: Implement public models**

```csharp
namespace Tw.ExceptionHandling;

public enum ErrorCategory
{
    Validation,
    Authentication,
    Authorization,
    Business,
    NotFound,
    Conflict,
    Dependency,
    System
}

public sealed record ErrorDescriptor(string Code, string Message, ErrorCategory Category);

public interface IExceptionToErrorMapper
{
    ErrorDescriptor Map(Exception exception);
}
```

```csharp
namespace Tw.Validation.Abstractions;

public sealed record ValidationError(string FieldPath, string Code, string Message);

public sealed class ValidationException : Exception
{
    public ValidationException(IEnumerable<ValidationError> errors)
        : base("输入验证失败")
    {
        Errors = errors.ToArray();
    }

    public IReadOnlyList<ValidationError> Errors { get; }
}
```

- [ ] **Step 4: Run tests**

Run: `dotnet test backend/dotnet/Tw.SmartPlatform.slnx --filter "FullyQualifiedName~ExceptionHandling|FullyQualifiedName~Validation"`

Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add backend/dotnet/BuildingBlocks/src/Foundation/Tw.ExceptionHandling backend/dotnet/BuildingBlocks/src/Foundation/Tw.Validation.Abstractions backend/dotnet/BuildingBlocks/tests
git commit -m "feat: add exception and validation foundation contracts"
```

### Task 5: Create JSON Abstractions And Newtonsoft Implementation

**Files:**
- Create: `backend/dotnet/BuildingBlocks/src/Foundation/Tw.Json.Abstractions/IJsonSerializer.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Foundation/Tw.Json.Abstractions/JsonSerializerOptions.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Foundation/Tw.Json.Newtonsoft/NewtonsoftJsonSerializer.cs`
- Create: `backend/dotnet/BuildingBlocks/tests/Tw.Json.Newtonsoft.Tests/NewtonsoftJsonSerializerTests.cs`

- [ ] **Step 1: Write JSON long ID test**

```csharp
using AwesomeAssertions;
using Tw.Json.Newtonsoft;

namespace Tw.Json.Newtonsoft.Tests;

public sealed class NewtonsoftJsonSerializerTests
{
    private sealed record Sample(long Id);

    [Fact]
    public void Serialize_WritesLongIdAsString()
    {
        var serializer = new NewtonsoftJsonSerializer();

        var json = serializer.Serialize(new Sample(9007199254740993L));

        json.Should().Contain("\"id\":\"9007199254740993\"");
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Json.Newtonsoft.Tests/Tw.Json.Newtonsoft.Tests.csproj`

Expected: FAIL because serializer does not exist.

- [ ] **Step 3: Implement abstraction**

```csharp
namespace Tw.Json.Abstractions;

public interface IJsonSerializer
{
    string Serialize<T>(T value);

    T? Deserialize<T>(string json);
}
```

- [ ] **Step 4: Implement Newtonsoft serializer**

```csharp
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Tw.Json.Abstractions;

namespace Tw.Json.Newtonsoft;

public sealed class NewtonsoftJsonSerializer : IJsonSerializer
{
    private static readonly JsonSerializerSettings Settings = new()
    {
        ContractResolver = new CamelCasePropertyNamesContractResolver(),
        ReferenceLoopHandling = ReferenceLoopHandling.Error,
        TypeNameHandling = TypeNameHandling.None
    };

    public string Serialize<T>(T value)
    {
        return JsonConvert.SerializeObject(value, Settings);
    }

    public T? Deserialize<T>(string json)
    {
        return JsonConvert.DeserializeObject<T>(json, Settings);
    }
}
```

- [ ] **Step 5: Add long converter before marking the test green**

Add a `JsonConverter<long>` and `JsonConverter<long?>` to `Settings.Converters`. The converter writes `writer.WriteValue(value.ToString(CultureInfo.InvariantCulture))` and reads decimal strings with `long.TryParse`.

- [ ] **Step 6: Run tests**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Json.Newtonsoft.Tests/Tw.Json.Newtonsoft.Tests.csproj`

Expected: PASS.

- [ ] **Step 7: Commit**

```bash
git add backend/dotnet/BuildingBlocks/src/Foundation/Tw.Json.Abstractions backend/dotnet/BuildingBlocks/src/Foundation/Tw.Json.Newtonsoft backend/dotnet/BuildingBlocks/tests/Tw.Json.Newtonsoft.Tests
git commit -m "feat: add json abstraction and newtonsoft serializer"
```

### Task 6: Create Work Unit Contracts

**Files:**
- Create: `backend/dotnet/BuildingBlocks/src/Foundation/Tw.Uow/IUnitOfWork.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Foundation/Tw.Uow/IUnitOfWorkManager.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Foundation/Tw.Uow/UnitOfWorkOptions.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Foundation/Tw.Uow/UnitOfWorkTransactionBehavior.cs`
- Create: `backend/dotnet/BuildingBlocks/tests/Tw.Uow.Tests/UnitOfWorkOptionsTests.cs`

- [ ] **Step 1: Write UoW options test**

```csharp
using AwesomeAssertions;
using Tw.Uow;

namespace Tw.Uow.Tests;

public sealed class UnitOfWorkOptionsTests
{
    [Fact]
    public void DefaultOptions_UseRequiredTransactionalBehavior()
    {
        var options = UnitOfWorkOptions.Default;

        options.Scope.Should().Be(UnitOfWorkScope.Required);
        options.TransactionBehavior.Should().Be(UnitOfWorkTransactionBehavior.Transactional);
    }
}
```

- [ ] **Step 2: Implement contracts**

```csharp
namespace Tw.Uow;

public enum UnitOfWorkScope
{
    Required,
    RequiresNew,
    Suppress
}

public enum UnitOfWorkTransactionBehavior
{
    NonTransactional,
    Transactional
}

public sealed record UnitOfWorkOptions(UnitOfWorkScope Scope, UnitOfWorkTransactionBehavior TransactionBehavior)
{
    public static UnitOfWorkOptions Default { get; } = new(UnitOfWorkScope.Required, UnitOfWorkTransactionBehavior.Transactional);
}

public interface IUnitOfWork : IAsyncDisposable
{
    CancellationToken CancellationToken { get; }

    Task CommitAsync(CancellationToken cancellationToken = default);

    Task RollbackAsync(CancellationToken cancellationToken = default);
}

public interface IUnitOfWorkManager
{
    IUnitOfWork? Current { get; }

    Task<IUnitOfWork> BeginAsync(UnitOfWorkOptions options, CancellationToken cancellationToken = default);
}
```

- [ ] **Step 3: Run tests**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Uow.Tests/Tw.Uow.Tests.csproj`

Expected: PASS.

- [ ] **Step 4: Commit**

```bash
git add backend/dotnet/BuildingBlocks/src/Foundation/Tw.Uow backend/dotnet/BuildingBlocks/tests/Tw.Uow.Tests
git commit -m "feat: add unit of work contracts"
```

### Task 7: Create ID Generation Packages

**Files:**
- Create: `backend/dotnet/BuildingBlocks/src/IdGeneration/Tw.IdGeneration/IIdGenerator.cs`
- Create: `backend/dotnet/BuildingBlocks/src/IdGeneration/Tw.IdGeneration.Yitter/YitterIdGenerator.cs`
- Create: `backend/dotnet/BuildingBlocks/src/IdGeneration/Tw.IdGeneration.Yitter/IdGenerationServiceCollectionExtensions.cs`
- Create: `backend/dotnet/BuildingBlocks/tests/Tw.IdGeneration.Yitter.Tests/YitterIdGeneratorTests.cs`

- [ ] **Step 1: Write ID generator test**

```csharp
using AwesomeAssertions;
using Tw.IdGeneration.Yitter;

namespace Tw.IdGeneration.Yitter.Tests;

public sealed class YitterIdGeneratorTests
{
    [Fact]
    public void NewId_ReturnsPositiveLong()
    {
        var generator = YitterIdGenerator.CreateForWorker(1);

        var id = generator.NewId();

        id.Should().BePositive();
    }
}
```

- [ ] **Step 2: Implement abstraction**

```csharp
namespace Tw.IdGeneration;

public interface IIdGenerator
{
    long NewId();
}
```

- [ ] **Step 3: Implement Yitter adapter**

```csharp
using Tw.IdGeneration;
using Yitter.IdGenerator;

namespace Tw.IdGeneration.Yitter;

public sealed class YitterIdGenerator : IIdGenerator
{
    private YitterIdGenerator()
    {
    }

    public static YitterIdGenerator CreateForWorker(ushort workerId)
    {
        YitIdHelper.SetIdGenerator(new IdGeneratorOptions(workerId));
        return new YitterIdGenerator();
    }

    public long NewId()
    {
        return YitIdHelper.NextId();
    }
}
```

- [ ] **Step 4: Run tests**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.IdGeneration.Yitter.Tests/Tw.IdGeneration.Yitter.Tests.csproj`

Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add backend/dotnet/BuildingBlocks/src/IdGeneration backend/dotnet/BuildingBlocks/tests/Tw.IdGeneration.Yitter.Tests
git commit -m "feat: add id generation abstraction and yitter adapter"
```

### Task 8: Implement Security Masking And Write-Back Protection

**Files:**
- Create: `backend/dotnet/BuildingBlocks/src/Foundation/Tw.Security/DataMasking/IDataMasker.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Foundation/Tw.Security/DataMasking/IDataMaskingRule.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Foundation/Tw.Security/DataMasking/IDataMaskingPolicyProvider.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Foundation/Tw.Security/DataMasking/ISensitiveValueDetector.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Foundation/Tw.Security/DataMasking/SensitiveDataAttribute.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Foundation/Tw.Security/DataMasking/SensitiveDataKind.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Foundation/Tw.Security/DataMasking/DefaultDataMasker.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Foundation/Tw.Security/DataMasking/MaskWriteBackGuard.cs`
- Test: `backend/dotnet/BuildingBlocks/tests/Tw.Security.Tests/DataMasking/DefaultDataMaskerTests.cs`
- Test: `backend/dotnet/BuildingBlocks/tests/Tw.Security.Tests/DataMasking/MaskWriteBackGuardTests.cs`

- [ ] **Step 1: Write masking tests**

```csharp
using AwesomeAssertions;
using Tw.Security.DataMasking;

namespace Tw.Security.Tests.DataMasking;

public sealed class DefaultDataMaskerTests
{
    [Fact]
    public void Mask_Phone_HidesMiddleDigits()
    {
        var masker = DefaultDataMasker.CreateDefault();

        var masked = masker.Mask("13800138000", SensitiveDataKind.PhoneNumber);

        masked.Should().Be("138****8000");
    }

    [Fact]
    public void Mask_Token_DoesNotExposeRawValue()
    {
        var masker = DefaultDataMasker.CreateDefault();

        var masked = masker.Mask("token-abcdef", SensitiveDataKind.Token);

        masked.Should().Be("***");
    }
}
```

- [ ] **Step 2: Write write-back protection test**

```csharp
using AwesomeAssertions;
using Tw.Security.DataMasking;

namespace Tw.Security.Tests.DataMasking;

public sealed class MaskWriteBackGuardTests
{
    [Fact]
    public void EnsureNotMaskedValue_RejectsMaskedPhoneWriteBack()
    {
        var guard = new MaskWriteBackGuard(DefaultDataMasker.CreateDefault());

        var act = () => guard.EnsureNotMaskedValue("138****8000", SensitiveDataKind.PhoneNumber);

        act.Should().Throw<MaskedValueWriteBackException>()
            .WithMessage("不能把脱敏值写回敏感字段");
    }
}
```

- [ ] **Step 3: Implement contracts**

```csharp
namespace Tw.Security.DataMasking;

public enum SensitiveDataKind
{
    Unknown,
    PhoneNumber,
    IdentityNumber,
    Email,
    Password,
    Token,
    ConnectionString,
    CertificatePrivateKey,
    RawSensitivePayload,
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
public sealed class SensitiveDataAttribute : Attribute
{
    public SensitiveDataAttribute(SensitiveDataKind kind)
    {
        Kind = kind;
    }

    public SensitiveDataKind Kind { get; }
}

public interface IDataMasker
{
    string Mask(string? value, SensitiveDataKind kind);
}

public interface IDataMaskingRule
{
    bool CanMask(SensitiveDataKind kind);

    string Mask(string? value);
}

public interface IDataMaskingPolicyProvider
{
    IReadOnlyList<IDataMaskingRule> GetRules();
}

public interface ISensitiveValueDetector
{
    bool IsMaskedValue(string? value, SensitiveDataKind kind);
}
```

- [ ] **Step 4: Implement default masker and write-back guard**

```csharp
namespace Tw.Security.DataMasking;

public sealed class DefaultDataMasker : IDataMasker, ISensitiveValueDetector
{
    public static DefaultDataMasker CreateDefault() => new();

    public string Mask(string? value, SensitiveDataKind kind)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return kind switch
        {
            SensitiveDataKind.PhoneNumber when value.Length >= 11 =>
                string.Concat(value.AsSpan(0, 3), "****", value.AsSpan(value.Length - 4, 4)),
            SensitiveDataKind.Email => MaskEmail(value),
            SensitiveDataKind.IdentityNumber when value.Length >= 8 =>
                string.Concat(value.AsSpan(0, 3), "********", value.AsSpan(value.Length - 4, 4)),
            SensitiveDataKind.Unknown => "***",
            _ => "***",
        };
    }

    public bool IsMaskedValue(string? value, SensitiveDataKind kind)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return value.Contains('*', StringComparison.Ordinal);
    }

    private static string MaskEmail(string value)
    {
        var at = value.IndexOf('@', StringComparison.Ordinal);
        return at <= 1 ? "***" : string.Concat(value.AsSpan(0, 1), "***", value.AsSpan(at));
    }
}

public sealed class MaskedValueWriteBackException : Exception
{
    public MaskedValueWriteBackException()
        : base("不能把脱敏值写回敏感字段")
    {
    }
}

public sealed class MaskWriteBackGuard(ISensitiveValueDetector detector)
{
    public void EnsureNotMaskedValue(string? value, SensitiveDataKind kind)
    {
        if (detector.IsMaskedValue(value, kind))
        {
            throw new MaskedValueWriteBackException();
        }
    }
}
```

- [ ] **Step 5: Run tests**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Security.Tests/Tw.Security.Tests.csproj --filter "DataMasker|MaskWriteBackGuard"`

Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add backend/dotnet/BuildingBlocks/src/Foundation/Tw.Security backend/dotnet/BuildingBlocks/tests/Tw.Security.Tests
git commit -m "feat: add security masking and writeback protection"
```

### Task 9: Create Text Templating Packages

**Files:**
- Create: `backend/dotnet/BuildingBlocks/src/TextTemplating/Tw.TextTemplating/TemplateSourceKind.cs`
- Create: `backend/dotnet/BuildingBlocks/src/TextTemplating/Tw.TextTemplating/TemplateRenderRequest.cs`
- Create: `backend/dotnet/BuildingBlocks/src/TextTemplating/Tw.TextTemplating/TemplateRenderResult.cs`
- Create: `backend/dotnet/BuildingBlocks/src/TextTemplating/Tw.TextTemplating/TemplateDiagnostic.cs`
- Create: `backend/dotnet/BuildingBlocks/src/TextTemplating/Tw.TextTemplating/ITemplateRenderer.cs`
- Create: `backend/dotnet/BuildingBlocks/src/TextTemplating/Tw.TextTemplating.Scriban/ScribanTemplateRenderer.cs`
- Create: `backend/dotnet/BuildingBlocks/src/TextTemplating/Tw.TextTemplating.Scriban/TemplateFileAccessPolicy.cs`
- Test: `backend/dotnet/BuildingBlocks/tests/Tw.TextTemplating.Tests/TemplateRenderRequestTests.cs`
- Test: `backend/dotnet/BuildingBlocks/tests/Tw.TextTemplating.Scriban.Tests/TemplateFileAccessPolicyTests.cs`

- [ ] **Step 1: Write abstraction test**

```csharp
using AwesomeAssertions;
using Tw.TextTemplating;

namespace Tw.TextTemplating.Tests;

public sealed class TemplateRenderRequestTests
{
    [Fact]
    public void Create_FileTemplate_StoresSourceAndVariables()
    {
        var request = new TemplateRenderRequest(
            TemplateSourceKind.File,
            "invoices/monthly.sbn",
            new Dictionary<string, object?> { ["tenantId"] = "tenant-a" });

        request.SourceKind.Should().Be(TemplateSourceKind.File);
        request.Source.Should().Be("invoices/monthly.sbn");
        request.Variables["tenantId"].Should().Be("tenant-a");
    }
}
```

- [ ] **Step 2: Implement abstractions**

```csharp
namespace Tw.TextTemplating;

public enum TemplateSourceKind
{
    String,
    File,
    EmbeddedResource,
    Configuration,
}

public sealed record TemplateRenderRequest(
    TemplateSourceKind SourceKind,
    string Source,
    IReadOnlyDictionary<string, object?> Variables);

public sealed record TemplateDiagnostic(string Code, string Message, string? MemberName, int? Line, int? Column);

public sealed record TemplateRenderResult(bool Success, string? Content, IReadOnlyList<TemplateDiagnostic> Diagnostics)
{
    public static TemplateRenderResult Succeeded(string content) => new(true, content, Array.Empty<TemplateDiagnostic>());

    public static TemplateRenderResult Failed(params TemplateDiagnostic[] diagnostics) => new(false, null, diagnostics);
}

public interface ITemplateRenderer
{
    Task<TemplateRenderResult> RenderAsync(TemplateRenderRequest request, CancellationToken cancellationToken);
}
```

- [ ] **Step 3: Write file access policy test**

```csharp
using AwesomeAssertions;
using Tw.TextTemplating.Scriban;

namespace Tw.TextTemplating.Scriban.Tests;

public sealed class TemplateFileAccessPolicyTests
{
    [Fact]
    public void Validate_RejectsPathOutsideRegisteredRoot()
    {
        var policy = new TemplateFileAccessPolicy(["D:/app/templates"]);

        var act = () => policy.Validate("D:/app/secrets/key.sbn");

        act.Should().Throw<TemplateFileAccessException>()
            .WithMessage("模板文件只能从注册的模板根目录读取");
    }
}
```

- [ ] **Step 4: Implement Scriban safety boundary**

```csharp
namespace Tw.TextTemplating.Scriban;

public sealed class TemplateFileAccessException : Exception
{
    public TemplateFileAccessException()
        : base("模板文件只能从注册的模板根目录读取")
    {
    }
}

public sealed class TemplateFileAccessPolicy
{
    private readonly string[] _allowedRoots;

    public TemplateFileAccessPolicy(IEnumerable<string> allowedRoots)
    {
        _allowedRoots = allowedRoots.Select(Path.GetFullPath).ToArray();
    }

    public string Validate(string path)
    {
        var fullPath = Path.GetFullPath(path);
        if (_allowedRoots.Any(root => fullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase)))
        {
            return fullPath;
        }

        throw new TemplateFileAccessException();
    }
}
```

`ScribanTemplateRenderer` must disable arbitrary file include, dangerous member access, reflection write, process access, network access, file-system access outside registered roots, and dependency-injection container access. Rendering accepts only the variables present in `TemplateRenderRequest.Variables` plus read-only culture, clock, tenant, and user values supplied by framework context abstractions.

- [ ] **Step 5: Run tests**

Run:

```powershell
dotnet test backend/dotnet/BuildingBlocks/tests/Tw.TextTemplating.Tests/Tw.TextTemplating.Tests.csproj --nologo
dotnet test backend/dotnet/BuildingBlocks/tests/Tw.TextTemplating.Scriban.Tests/Tw.TextTemplating.Scriban.Tests.csproj --nologo
```

Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add backend/dotnet/BuildingBlocks/src/TextTemplating backend/dotnet/BuildingBlocks/tests/Tw.TextTemplating*
git commit -m "feat: add text templating abstractions and scriban adapter"
```

### Task 10: Create Excel Packages

**Files:**
- Create: `backend/dotnet/BuildingBlocks/src/Excel/Tw.Excel/ExcelColumnDefinition.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Excel/Tw.Excel/ExcelTemplateDefinition.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Excel/Tw.Excel/ExcelImportError.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Excel/Tw.Excel/IExcelImporter.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Excel/Tw.Excel/IExcelExporter.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Excel/Tw.Excel/FormulaInjectionProtector.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Excel/Tw.Excel.MiniExcel/MiniExcelExporter.cs`
- Test: `backend/dotnet/BuildingBlocks/tests/Tw.Excel.Tests/FormulaInjectionProtectorTests.cs`
- Test: `backend/dotnet/BuildingBlocks/tests/Tw.Excel.Tests/ExcelTemplateDefinitionTests.cs`

- [ ] **Step 1: Write formula-injection test**

```csharp
using AwesomeAssertions;
using Tw.Excel;

namespace Tw.Excel.Tests;

public sealed class FormulaInjectionProtectorTests
{
    [Theory]
    [InlineData("=cmd|'/C calc'!A0")]
    [InlineData("+SUM(A1:A2)")]
    [InlineData("-10+20")]
    [InlineData("@user")]
    public void Protect_PrefixesFormulaLikeUserText(string value)
    {
        FormulaInjectionProtector.Protect(value).Should().Be("'" + value);
    }
}
```

- [ ] **Step 2: Write dynamic column limit test**

```csharp
using AwesomeAssertions;
using Tw.Excel;

namespace Tw.Excel.Tests;

public sealed class ExcelTemplateDefinitionTests
{
    [Fact]
    public void Create_RejectsDynamicColumnCountOverLimit()
    {
        var columns = Enumerable.Range(0, 101)
            .Select(index => new ExcelColumnDefinition($"dynamic_{index}", $"动态列 {index}", "string", IsDynamic: true))
            .ToArray();

        var act = () => ExcelTemplateDefinition.Create("invoice", columns, maxDynamicColumns: 100);

        act.Should().Throw<ExcelTemplateException>()
            .WithMessage("动态列数量超过配置上限");
    }
}
```

- [ ] **Step 3: Implement Excel contracts**

```csharp
namespace Tw.Excel;

public sealed record ExcelColumnDefinition(
    string FieldName,
    string HeaderPath,
    string DataType,
    bool Required = false,
    bool IsDynamic = false);

public sealed record ExcelImportError(int RowNumber, string ColumnName, string FieldPath, string Code, string Message);

public sealed class ExcelTemplateException : Exception
{
    public ExcelTemplateException(string message) : base(message)
    {
    }
}

public sealed record ExcelTemplateDefinition(string Name, IReadOnlyList<ExcelColumnDefinition> Columns)
{
    public static ExcelTemplateDefinition Create(string name, IReadOnlyList<ExcelColumnDefinition> columns, int maxDynamicColumns)
    {
        if (columns.Count(column => column.IsDynamic) > maxDynamicColumns)
        {
            throw new ExcelTemplateException("动态列数量超过配置上限");
        }

        return new ExcelTemplateDefinition(name, columns);
    }
}

public interface IExcelImporter
{
    Task<IReadOnlyList<ExcelImportError>> ValidateAsync(Stream stream, ExcelTemplateDefinition template, CancellationToken cancellationToken);
}

public interface IExcelExporter
{
    Task ExportBlankTemplateAsync(Stream stream, ExcelTemplateDefinition template, CancellationToken cancellationToken);
}
```

- [ ] **Step 4: Implement formula protector**

```csharp
namespace Tw.Excel;

public static class FormulaInjectionProtector
{
    private static readonly char[] FormulaPrefixes = ['=', '+', '-', '@'];

    public static string Protect(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return FormulaPrefixes.Contains(value[0]) ? "'" + value : value;
    }
}
```

`MiniExcelExporter` must use MiniExcel for streaming writes and DocumentFormat.OpenXml for merged multi-level headers, hidden-sheet dropdown values, OpenXML data validation, and blank template post-processing. Business code consumes `Tw.Excel` contracts only and must not call MiniExcel APIs directly.

- [ ] **Step 5: Run tests**

Run:

```powershell
dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Excel.Tests/Tw.Excel.Tests.csproj --nologo
dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Excel.MiniExcel.Tests/Tw.Excel.MiniExcel.Tests.csproj --nologo
```

Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add backend/dotnet/BuildingBlocks/src/Excel backend/dotnet/BuildingBlocks/tests/Tw.Excel*
git commit -m "feat: add excel abstractions and miniexcel adapter"
```

## Plan Self-Review

- Spec coverage: foundation package split, threading, timing, exception handling, security masking and write-back protection, validation, JSON, UoW, ID generation, text templating, Excel, package charters, and tests are covered.
- Placeholder scan: no placeholder tokens are present.
- Type consistency: package names and namespaces match final design names.
