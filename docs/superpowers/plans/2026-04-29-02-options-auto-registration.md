# Options Auto Registration Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build P0 options auto-registration on top of existing `IConfigurableOptions` and `ConfigurationSectionAttribute`.

**Architecture:** Options discovery reuses the assembly scan from the DI auto-registration core but remains container-neutral. The service collection layer binds discovered options through Microsoft Options APIs and leaves runtime reload, snapshot, and validation behavior to the standard Options pipeline.

**Tech Stack:** .NET 10, Tw.Core, Microsoft.Extensions.Configuration, Microsoft.Extensions.Options, DataAnnotations, xUnit, FluentAssertions

---

## Execution Order

Run after `2026-04-29-01-auto-registration-core.md`. This plan must pass before `2026-04-29-03-service-aop-castle.md`.

## Source Inputs

- Design sections: § 3.4, § 7, § 9.9, § 10.1 items 10-13
- Existing files: `backend/dotnet/BuildingBlocks/src/Tw.Core/Configuration/ConfigurationSectionAttribute.cs`, `backend/dotnet/BuildingBlocks/src/Tw.Core/Configuration/IConfigurableOptions.cs`
- Standards: `rules.configuration#rules` version `1.1.0`, `docs/standards/rules/configuration.md`; `rules.error-handling#rules` version `1.2.0`, `docs/standards/rules/error-handling.md`; `rules.test-strategy#rules` version `1.1.0`, `docs/standards/rules/test-strategy.md`

## File Structure

- Modify: `backend/dotnet/BuildingBlocks/src/Tw.Core/Configuration/ConfigurationSectionAttribute.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/Configuration/DisableOptionsRegistrationAttribute.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/DependencyInjection/Options/OptionsRegistrationDescriptor.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/DependencyInjection/Options/OptionsRegistrationScanner.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/DependencyInjection/Options/OptionsRegistrationExtensions.cs`
- Modify: `backend/dotnet/BuildingBlocks/src/Tw.Core/DependencyInjection/AutoRegistrationOptions.cs`
- Modify: `backend/dotnet/BuildingBlocks/src/Tw.Core/DependencyInjection/Autofac/AutofacAutoRegistrationExtensions.cs`
- Modify: `backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/ConfigurationTests.cs`
- Create: `backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/DependencyInjection/Options/OptionsRegistrationScannerTests.cs`
- Create: `backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/DependencyInjection/Options/OptionsRegistrationExtensionsTests.cs`

## Tasks

### Task 1: Extend Configuration Metadata

**Files:**
- Modify: `backend/dotnet/BuildingBlocks/src/Tw.Core/Configuration/ConfigurationSectionAttribute.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/Configuration/DisableOptionsRegistrationAttribute.cs`
- Modify: `backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/ConfigurationTests.cs`

- [ ] **Step 1: Update failing metadata tests**

Replace the `ConfigurationSectionAttribute_Targets_Classes` expectation and add option metadata assertions:

```csharp
[Fact]
public void ConfigurationSectionAttribute_Targets_Classes_And_Allows_Multiple()
{
    var usage = typeof(ConfigurationSectionAttribute)
        .GetCustomAttributes(typeof(AttributeUsageAttribute), inherit: false)
        .Should()
        .ContainSingle()
        .Subject
        .Should()
        .BeOfType<AttributeUsageAttribute>()
        .Subject;

    usage.ValidOn.Should().Be(AttributeTargets.Class);
    usage.AllowMultiple.Should().BeTrue();
    usage.Inherited.Should().BeTrue();
}

[Fact]
public void ConfigurationSectionAttribute_Stores_Options_Metadata()
{
    var attribute = new ConfigurationSectionAttribute("Redis:Primary")
    {
        OptionsName = "Primary",
        ValidateOnStart = true,
        DirectInject = true
    };

    attribute.Name.Should().Be("Redis:Primary");
    attribute.OptionsName.Should().Be("Primary");
    attribute.ValidateOnStart.Should().BeTrue();
    attribute.DirectInject.Should().BeTrue();
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run:

```powershell
dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Tw.Core.Tests.csproj --filter FullyQualifiedName~ConfigurationTests
```

Expected: FAIL because `AllowMultiple` is still false and metadata properties do not exist.

- [ ] **Step 3: Update `ConfigurationSectionAttribute`**

```csharp
namespace Tw.Core.Configuration;

/// <summary>标识应绑定到选项类型的配置节</summary>
/// <param name="name">调用方在选项绑定期间使用的非空配置节名称</param>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
public sealed class ConfigurationSectionAttribute(string name) : Attribute
{
    /// <summary>与被标注选项类型关联的配置节名称</summary>
    public string Name { get; } = Tw.Core.Check.NotNullOrWhiteSpace(name);

    /// <summary>命名选项名称，空值表示默认选项实例</summary>
    public string? OptionsName { get; init; }

    /// <summary>是否在启动阶段执行选项验证</summary>
    public bool? ValidateOnStart { get; init; }

    /// <summary>是否允许构造函数直接注入选项类型本体</summary>
    public bool DirectInject { get; init; }
}
```

- [ ] **Step 4: Add disable attribute**

```csharp
namespace Tw.Core.Configuration;

/// <summary>声明当前类型不参与配置选项自动注册</summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class DisableOptionsRegistrationAttribute : Attribute;
```

- [ ] **Step 5: Run configuration tests**

Run:

```powershell
dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Tw.Core.Tests.csproj --filter FullyQualifiedName~ConfigurationTests
```

Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add backend/dotnet/BuildingBlocks/src/Tw.Core/Configuration backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/ConfigurationTests.cs
git commit -m "feat: extend configuration section metadata"
```

### Task 2: Discover Options Registration Descriptors

**Files:**
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/DependencyInjection/Options/OptionsRegistrationDescriptor.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/DependencyInjection/Options/OptionsRegistrationScanner.cs`
- Modify: `backend/dotnet/BuildingBlocks/src/Tw.Core/DependencyInjection/AutoRegistrationOptions.cs`
- Test: `backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/DependencyInjection/Options/OptionsRegistrationScannerTests.cs`

- [ ] **Step 1: Write failing scanner tests**

```csharp
[Fact]
public void Scan_Finds_Configurable_Options_From_Library_Assembly()
{
    var options = new AutoRegistrationOptions();
    options.AddAssemblyOf<RedisOptions>();

    var descriptors = OptionsRegistrationScanner.Scan(options);

    descriptors.Should().ContainSingle(descriptor =>
        descriptor.OptionsType == typeof(RedisOptions) &&
        descriptor.SectionName == "Tw:Redis" &&
        descriptor.OptionsName is null);
}

[Fact]
public void Scan_Fails_When_Default_Option_Is_Duplicated()
{
    var options = new AutoRegistrationOptions();
    options.AddAssemblyOf<DuplicateDefaultOptions>();

    var action = () => OptionsRegistrationScanner.Scan(options);

    action.Should().Throw<AutoRegistrationException>()
        .WithMessage("*DuplicateDefaultOptions*默认命名实例*");
}

[ConfigurationSection("Tw:Redis")]
private sealed class RedisOptions : IConfigurableOptions
{
    public string ConnectionString { get; init; } = "";
}

[ConfigurationSection("First")]
[ConfigurationSection("Second")]
private sealed class DuplicateDefaultOptions : IConfigurableOptions
{
    public string Value { get; init; } = "";
}
```

- [ ] **Step 2: Run scanner tests to verify they fail**

Run:

```powershell
dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Tw.Core.Tests.csproj --filter FullyQualifiedName~OptionsRegistrationScannerTests
```

Expected: FAIL because scanner files do not exist.

- [ ] **Step 3: Create descriptor**

```csharp
namespace Tw.Core.DependencyInjection.Options;

/// <summary>描述一个应注册到 Microsoft Options 管线的选项实例</summary>
public sealed record OptionsRegistrationDescriptor(
    Type OptionsType,
    string SectionName,
    string? OptionsName,
    bool ValidateOnStart,
    bool DirectInject);
```

- [ ] **Step 4: Extend auto-registration options**

Add these members to `AutoRegistrationOptions`:

```csharp
/// <summary>是否启用配置选项自动注册</summary>
public bool OptionsAutoRegistrationEnabled { get; private set; } = true;

/// <summary>测试与生产环境默认是否启动时验证选项</summary>
public bool ValidateOptionsOnStartByDefault { get; private set; } = true;

/// <summary>启用配置选项自动注册</summary>
public AutoRegistrationOptions EnableOptionsAutoRegistration()
{
    OptionsAutoRegistrationEnabled = true;
    return this;
}

/// <summary>禁用配置选项自动注册</summary>
public AutoRegistrationOptions DisableOptionsAutoRegistration()
{
    OptionsAutoRegistrationEnabled = false;
    return this;
}

/// <summary>配置默认启动验证策略</summary>
public AutoRegistrationOptions UseValidateOptionsOnStartByDefault(bool enabled)
{
    ValidateOptionsOnStartByDefault = enabled;
    return this;
}
```

- [ ] **Step 5: Create scanner**

Implement `OptionsRegistrationScanner.Scan(AutoRegistrationOptions options)` with these rules:

```text
1. Return an empty list when OptionsAutoRegistrationEnabled is false.
2. Reuse TypeFinder over AutoRegistrationOptions.Assemblies.
3. Keep non-abstract class or record class types not marked DisableOptionsRegistrationAttribute.
4. Library assembly candidates must implement IConfigurableOptions or have ConfigurationSectionAttribute.
5. Section name comes from ConfigurationSectionAttribute.Name when present; otherwise remove Options or Settings suffix from type name.
6. Multiple attributes on the same type are allowed.
7. More than one OptionsName == null on the same type throws AutoRegistrationException.
8. Repeated non-null OptionsName on the same type throws AutoRegistrationException.
9. ValidateOnStart uses attribute override when set; otherwise AutoRegistrationOptions.ValidateOptionsOnStartByDefault.
10. DirectInject is copied from the attribute, and descriptors with non-null OptionsName force DirectInject to false.
```

- [ ] **Step 6: Run scanner tests**

Run:

```powershell
dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Tw.Core.Tests.csproj --filter FullyQualifiedName~OptionsRegistrationScannerTests
```

Expected: PASS.

- [ ] **Step 7: Commit**

```bash
git add backend/dotnet/BuildingBlocks/src/Tw.Core/DependencyInjection/AutoRegistrationOptions.cs backend/dotnet/BuildingBlocks/src/Tw.Core/DependencyInjection/Options backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/DependencyInjection/Options/OptionsRegistrationScannerTests.cs
git commit -m "feat: discover options auto registration descriptors"
```

### Task 3: Register Options Into IServiceCollection

**Files:**
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/DependencyInjection/Options/OptionsRegistrationExtensions.cs`
- Modify: `backend/dotnet/BuildingBlocks/src/Tw.Core/DependencyInjection/Autofac/AutofacAutoRegistrationExtensions.cs`
- Test: `backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/DependencyInjection/Options/OptionsRegistrationExtensionsTests.cs`

- [ ] **Step 1: Write failing service collection tests**

```csharp
[Fact]
public void AddAutoRegistration_Binds_Options_And_Allows_Direct_Inject()
{
    var configuration = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Tw:Payment:MerchantId"] = "merchant-001"
        })
        .Build();

    var services = new ServiceCollection();
    services.AddSingleton<IConfiguration>(configuration);
    services.AddAutoRegistration(options => options.AddAssemblyOf<PaymentOptions>());

    using var provider = services.BuildServiceProvider(validateScopes: true);

    provider.GetRequiredService<IOptionsMonitor<PaymentOptions>>().CurrentValue.MerchantId
        .Should().Be("merchant-001");
    provider.GetRequiredService<PaymentOptions>().MerchantId
        .Should().Be("merchant-001");
}

[ConfigurationSection("Tw:Payment", DirectInject = true, ValidateOnStart = true)]
private sealed class PaymentOptions : IConfigurableOptions
{
    [Required]
    public string MerchantId { get; init; } = "";
}
```

- [ ] **Step 2: Run test to verify it fails**

Run:

```powershell
dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Tw.Core.Tests.csproj --filter AddAutoRegistration_Binds_Options_And_Allows_Direct_Inject
```

Expected: FAIL because options are not registered by `AddAutoRegistration`.

- [ ] **Step 3: Create registration extension**

Implement `OptionsRegistrationExtensions.AddAutoRegisteredOptions(IServiceCollection services, IConfiguration configuration, AutoRegistrationOptions options)` so it:

```text
1. Calls OptionsRegistrationScanner.Scan(options).
2. For each descriptor, invokes services.AddOptions<TOptions>(name).Bind(configuration.GetSection(sectionName)).
3. Calls ValidateDataAnnotations for every descriptor.
4. Calls ValidateOnStart only when descriptor.ValidateOnStart is true.
5. Registers IValidateOptions<TOptions> implementations found in scanned assemblies as singleton services.
6. For DirectInject default descriptors, registers TOptions as transient factory resolving IOptionsMonitor<TOptions>.CurrentValue.
7. Never registers direct TOptions injection for named options.
```

- [ ] **Step 4: Wire from `AddAutoRegistration`**

Update `AutofacAutoRegistrationExtensions.AddAutoRegistration` so it resolves an `IConfiguration` already present in `IServiceCollection` via singleton descriptor when available, and calls `AddAutoRegisteredOptions`. If no `IConfiguration` is registered and options descriptors exist, throw:

```csharp
throw new AutoRegistrationException("启用配置选项自动注册时必须先注册 IConfiguration。");
```

- [ ] **Step 5: Run service collection tests**

Run:

```powershell
dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Tw.Core.Tests.csproj --filter FullyQualifiedName~OptionsRegistrationExtensionsTests
```

Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add backend/dotnet/BuildingBlocks/src/Tw.Core/DependencyInjection/Options/OptionsRegistrationExtensions.cs backend/dotnet/BuildingBlocks/src/Tw.Core/DependencyInjection/Autofac/AutofacAutoRegistrationExtensions.cs backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/DependencyInjection/Options/OptionsRegistrationExtensionsTests.cs
git commit -m "feat: register discovered options"
```

### Task 4: Cover Named Options and Validation Failures

**Files:**
- Test: `backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/DependencyInjection/Options/OptionsRegistrationExtensionsTests.cs`

- [ ] **Step 1: Add named options test**

```csharp
[Fact]
public void AddAutoRegistration_Registers_Named_Options_Without_Direct_Inject()
{
    var configuration = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Redis:Primary:ConnectionString"] = "primary",
            ["Redis:Replica:ConnectionString"] = "replica"
        })
        .Build();

    var services = new ServiceCollection();
    services.AddSingleton<IConfiguration>(configuration);
    services.AddAutoRegistration(options => options.AddAssemblyOf<NamedRedisOptions>());

    using var provider = services.BuildServiceProvider(validateScopes: true);
    var monitor = provider.GetRequiredService<IOptionsMonitor<NamedRedisOptions>>();

    monitor.Get("Primary").ConnectionString.Should().Be("primary");
    monitor.Get("Replica").ConnectionString.Should().Be("replica");
    provider.GetService<NamedRedisOptions>().Should().BeNull();
}

[ConfigurationSection("Redis:Primary", OptionsName = "Primary", DirectInject = true)]
[ConfigurationSection("Redis:Replica", OptionsName = "Replica", DirectInject = true)]
private sealed class NamedRedisOptions : IConfigurableOptions
{
    [Required]
    public string ConnectionString { get; init; } = "";
}
```

- [ ] **Step 2: Add validation failure test**

```csharp
[Fact]
public void AddAutoRegistration_ValidateOnStart_Fails_With_Safe_Message()
{
    var configuration = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Tw:Invalid:Secret"] = "plain-secret"
        })
        .Build();

    var services = new ServiceCollection();
    services.AddSingleton<IConfiguration>(configuration);
    services.AddAutoRegistration(options => options.AddAssemblyOf<InvalidOptions>());

    var action = () =>
    {
        using var provider = services.BuildServiceProvider(validateScopes: true);
        provider.GetRequiredService<IOptions<InvalidOptions>>().Value.ToString();
    };

    action.Should().Throw<OptionsValidationException>()
        .WithMessage("*InvalidOptions*RequiredValue*")
        .And.Message.Should().NotContain("plain-secret");
}

[ConfigurationSection("Tw:Invalid", ValidateOnStart = true)]
private sealed class InvalidOptions : IConfigurableOptions
{
    [Required]
    public string RequiredValue { get; init; } = "";

    public string Secret { get; init; } = "";
}
```

- [ ] **Step 3: Run options tests**

Run:

```powershell
dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Tw.Core.Tests.csproj --filter FullyQualifiedName~OptionsRegistration
```

Expected: PASS.

- [ ] **Step 4: Commit**

```bash
git add backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/DependencyInjection/Options/OptionsRegistrationExtensionsTests.cs
git commit -m "test: cover named options and validation failures"
```

### Task 5: Verify P0 Options Acceptance

**Files:**
- Verify: `backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Tw.Core.Tests.csproj`

- [ ] **Step 1: Run focused options tests**

Run:

```powershell
dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Tw.Core.Tests.csproj --filter "FullyQualifiedName~ConfigurationTests|FullyQualifiedName~OptionsRegistration"
```

Expected: PASS.

- [ ] **Step 2: Run full core tests**

Run:

```powershell
dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Tw.Core.Tests.csproj
```

Expected: PASS.

## Self-Review Checklist

- [ ] `ConfigurationSectionAttribute` supports multiple declarations and records `OptionsName`, `ValidateOnStart`, and `DirectInject`.
- [ ] Existing single attribute usage remains valid.
- [ ] Scanner rejects duplicate default names and duplicate named instances.
- [ ] Direct injection uses `IOptionsMonitor<TOptions>.CurrentValue`.
- [ ] Named options are available through monitor or snapshot `Get(name)` and are not direct-injected as `TOptions`.
- [ ] Validation failures expose type, section, and failing field without exposing secret values.
