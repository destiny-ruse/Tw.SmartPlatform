# P3 Options 装载 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 在 `Tw.DependencyInjection` 中自动发现、绑定、校验并诊断实现 `IConfigurableOptions` 的 Options 类型。

**Architecture:** P3 复用 P1 的程序集发现结果，只读取纳入扫描的程序集。Options 装载由 `OptionsBindingPlanner` 产出候选计划，`OptionsBindingExecutor` 写入 `IServiceCollection`，并把 `OptionsBindingReport` 注册为 singleton。该阶段不依赖 P2 的服务仲裁结果，也不引入 AOP、MVC 或 gRPC 承载。

**Tech Stack:** C# / .NET 10、Microsoft.Extensions.Options、Microsoft.Extensions.Options.ConfigurationExtensions、Microsoft.Extensions.Options.DataAnnotations、xunit、FluentAssertions。

---

## 文件结构

**修改：**
- `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Tw.DependencyInjection.csproj`
- `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/ServiceCollectionRegistrationExtensions.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/package-charter.yaml`
- `docs/shared-packages/dotnet/Tw.DependencyInjection/README.md`

**新增：**
- `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Configuration/OptionsBindingCandidate.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Configuration/OptionsBindingPlan.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Configuration/OptionsBindingPlanner.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Configuration/OptionsBindingExecutor.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Configuration/ConfigurableOptionsPostConfigure.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Diagnostics/OptionsBindingDiagnostic.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Diagnostics/OptionsBindingReport.cs`
- `backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests/Configuration/OptionsBindingPlannerTests.cs`
- `backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests/Configuration/OptionsBindingExecutorTests.cs`
- `docs/shared-packages/dotnet/Tw.DependencyInjection/options-binding.md`

## Task 1: 依赖与诊断模型

**Files:**
- Modify: `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Tw.DependencyInjection.csproj`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Diagnostics/OptionsBindingDiagnostic.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Diagnostics/OptionsBindingReport.cs`
- Test: `backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests/Configuration/OptionsBindingPlannerTests.cs`

- [ ] **Step 1: 写失败测试**

```csharp
using FluentAssertions;
using Microsoft.Extensions.Options;
using Tw.DependencyInjection.Diagnostics;
using Xunit;

namespace Tw.DependencyInjection.Tests.Configuration;

public class OptionsBindingPlannerTests
{
    [Fact]
    public void Report_ExposesOptionsBindingDiagnostics()
    {
        var item = new OptionsBindingDiagnostic(
            OptionsTypeName: "Sample.CacheOptions",
            SectionPath: "Cache",
            Name: Options.DefaultName,
            SectionExists: true,
            BindingStatus: "bound",
            ValidationStatus: "enabled",
            IsSensitive: false);

        var report = new OptionsBindingReport([item]);

        report.Items.Should().ContainSingle().Which.SectionPath.Should().Be("Cache");
    }
}
```

- [ ] **Step 2: 运行测试确认失败**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests/Tw.DependencyInjection.Tests.csproj --nologo`

Expected: 编译失败，`OptionsBindingDiagnostic` 与 `OptionsBindingReport` 不存在。

- [ ] **Step 3: 添加包引用**

在 `Tw.DependencyInjection.csproj` 的包引用中追加：

```xml
    <PackageReference Include="Microsoft.Extensions.Options.ConfigurationExtensions" />
    <PackageReference Include="Microsoft.Extensions.Options.DataAnnotations" />
```

- [ ] **Step 4: 新增诊断模型**

`OptionsBindingDiagnostic.cs`：

```csharp
namespace Tw.DependencyInjection.Diagnostics;

/// <summary>单个 Options 类型的绑定诊断项</summary>
public sealed record OptionsBindingDiagnostic(
    string OptionsTypeName,
    string SectionPath,
    string Name,
    bool SectionExists,
    string BindingStatus,
    string ValidationStatus,
    bool IsSensitive);
```

`OptionsBindingReport.cs`：

```csharp
namespace Tw.DependencyInjection.Diagnostics;

/// <summary>Options 自动装载诊断报告</summary>
public sealed class OptionsBindingReport
{
    /// <summary>初始化 Options 绑定诊断报告</summary>
    /// <param name="items">绑定诊断项</param>
    public OptionsBindingReport(IReadOnlyList<OptionsBindingDiagnostic> items)
    {
        Items = items;
    }

    /// <summary>Options 绑定诊断项</summary>
    public IReadOnlyList<OptionsBindingDiagnostic> Items { get; }
}
```

- [ ] **Step 5: 运行测试确认通过**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests/Tw.DependencyInjection.Tests.csproj --nologo`

Expected: PASS。

## Task 2: Options 发现与路径推导

**Files:**
- Create: `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Configuration/OptionsBindingCandidate.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Configuration/OptionsBindingPlan.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Configuration/OptionsBindingPlanner.cs`
- Test: `backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests/Configuration/OptionsBindingPlannerTests.cs`

- [ ] **Step 1: 追加失败测试**

```csharp
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Tw.Configuration.Abstractions;
using Tw.DependencyInjection.Configuration;

private sealed class CacheOptions : IConfigurableOptions
{
    public string Endpoint { get; set; } = string.Empty;
}

[OptionsSection("Tw:Redis")]
[OptionsName("primary")]
private sealed class RedisOptions : IConfigurableOptions;

[DisableOptionsBinding]
private sealed class DisabledOptions : IConfigurableOptions;

[Fact]
public void Plan_DiscoversOptionsAndInfersPathAndName()
{
    var configuration = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Cache:Endpoint"] = "localhost",
            ["Tw:Redis:Endpoint"] = "redis",
        })
        .Build();

    var plan = OptionsBindingPlanner.Plan(
        assemblies: [typeof(CacheOptions).Assembly],
        typesByAssemblyName: new Dictionary<string, IReadOnlyList<Type>>(StringComparer.Ordinal)
        {
            [typeof(CacheOptions).Assembly.GetName().Name!] =
                [typeof(CacheOptions), typeof(RedisOptions), typeof(DisabledOptions)],
        },
        configuration);

    plan.Candidates.Should().Contain(c => c.OptionsType == typeof(CacheOptions)
        && c.SectionPath == "Cache"
        && c.Name == Options.DefaultName);
    plan.Candidates.Should().Contain(c => c.OptionsType == typeof(RedisOptions)
        && c.SectionPath == "Tw:Redis"
        && c.Name == "primary");
    plan.Candidates.Should().NotContain(c => c.OptionsType == typeof(DisabledOptions));
}
```

- [ ] **Step 2: 运行测试确认失败**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests/Tw.DependencyInjection.Tests.csproj --nologo`

Expected: 编译失败，`OptionsBindingPlanner` 不存在。

- [ ] **Step 3: 实现计划模型**

```csharp
using Tw.DependencyInjection.Diagnostics;

namespace Tw.DependencyInjection.Configuration;

internal sealed record OptionsBindingCandidate(
    Type OptionsType,
    string SectionPath,
    string Name,
    bool SectionExists,
    bool IsSensitive,
    Type? ValidatorType);

internal sealed record OptionsBindingPlan(
    IReadOnlyList<OptionsBindingCandidate> Candidates,
    OptionsBindingReport Report);
```

- [ ] **Step 4: 实现 Planner**

`OptionsBindingPlanner.Plan` 必须执行以下规则：
- 只扫描 `typesByAssemblyName` 中属于 `assemblies` 的类型。
- 类型必须是非抽象类、具备公共无参构造函数、实现 `IConfigurableOptions`、未标记 `[DisableOptionsBinding]`。
- 默认路径为类型名去掉 `Options` 后缀；`[OptionsSection]` 覆盖默认路径。
- `[OptionsName]` 未出现时使用 `Options.DefaultName`。
- 实现 `IConfigurableOptions<TOptions>` 时，`TOptions` 必须等于自身类型，否则抛出 `ServiceRegistrationException`。
- 同一 `OptionsType + Name` 或同一 `SectionPath + Name` 重复时抛出 `ServiceRegistrationException`。
- 诊断不输出配置值，只输出路径、状态和敏感标记。

- [ ] **Step 5: 运行测试确认通过**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests/Tw.DependencyInjection.Tests.csproj --nologo`

Expected: PASS。

## Task 3: 绑定、校验与 PostConfigure

**Files:**
- Create: `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Configuration/OptionsBindingExecutor.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/Configuration/ConfigurableOptionsPostConfigure.cs`
- Test: `backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests/Configuration/OptionsBindingExecutorTests.cs`

- [ ] **Step 1: 写失败测试**

```csharp
using System.ComponentModel.DataAnnotations;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Tw.Configuration.Abstractions;
using Tw.DependencyInjection.Configuration;
using Xunit;

namespace Tw.DependencyInjection.Tests.Configuration;

public class OptionsBindingExecutorTests
{
    private sealed class CacheOptions : IConfigurableOptions<CacheOptions>
    {
        [Required]
        public string Endpoint { get; set; } = string.Empty;
        public string EffectiveEndpoint { get; set; } = string.Empty;

        public void PostConfigure(CacheOptions options, IConfiguration configuration)
        {
            options.EffectiveEndpoint = configuration["Endpoint"] ?? options.Endpoint;
        }
    }

    [Fact]
    public void Apply_BindsValidatesAndRunsPostConfigure()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Cache:Endpoint"] = "localhost",
            })
            .Build();
        var services = new ServiceCollection();
        var candidate = new OptionsBindingCandidate(
            typeof(CacheOptions),
            "Cache",
            Options.DefaultName,
            SectionExists: true,
            IsSensitive: false,
            ValidatorType: null);

        OptionsBindingExecutor.Apply(services, configuration, [candidate]);
        using var provider = services.BuildServiceProvider(validateScopes: true);

        var options = provider.GetRequiredService<IOptions<CacheOptions>>().Value;

        options.Endpoint.Should().Be("localhost");
        options.EffectiveEndpoint.Should().Be("localhost");
    }
}
```

- [ ] **Step 2: 运行测试确认失败**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests/Tw.DependencyInjection.Tests.csproj --nologo`

Expected: 编译失败，`OptionsBindingExecutor` 不存在。

- [ ] **Step 3: 实现 PostConfigure 包装器**

```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Tw.Configuration.Abstractions;

namespace Tw.DependencyInjection.Configuration;

internal sealed class ConfigurableOptionsPostConfigure<TOptions> : IPostConfigureOptions<TOptions>
    where TOptions : class, IConfigurableOptions<TOptions>
{
    private readonly string _name;
    private readonly IConfiguration _section;

    public ConfigurableOptionsPostConfigure(string name, IConfiguration section)
    {
        _name = name;
        _section = section;
    }

    public void PostConfigure(string? name, TOptions options)
    {
        if (name == _name)
        {
            options.PostConfigure(options, _section);
        }
    }
}
```

- [ ] **Step 4: 实现 Executor**

`OptionsBindingExecutor.Apply` 使用反射调用泛型私有方法：

```csharp
private static void ApplyOne<TOptions>(
    IServiceCollection services,
    IConfiguration configuration,
    OptionsBindingCandidate candidate)
    where TOptions : class, IConfigurableOptions
{
    var section = configuration.GetSection(candidate.SectionPath);
    if (!candidate.SectionExists)
    {
        throw new ServiceRegistrationException($"必填配置节缺失: {candidate.SectionPath}");
    }

    services.AddOptions<TOptions>(candidate.Name)
        .Bind(section)
        .ValidateDataAnnotations()
        .ValidateOnStart();

    if (candidate.ValidatorType is not null)
    {
        services.AddSingleton(typeof(IValidateOptions<TOptions>), candidate.ValidatorType);
    }

    if (typeof(IConfigurableOptions<TOptions>).IsAssignableFrom(typeof(TOptions)))
    {
        var wrapperType = typeof(ConfigurableOptionsPostConfigure<>).MakeGenericType(typeof(TOptions));
        services.AddSingleton(typeof(IPostConfigureOptions<TOptions>),
            Activator.CreateInstance(wrapperType, candidate.Name, section)!);
    }
}
```

- [ ] **Step 5: 运行测试确认通过**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests/Tw.DependencyInjection.Tests.csproj --nologo`

Expected: PASS。

## Task 4: 接入 AddServiceRegistration

**Files:**
- Modify: `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/ServiceCollectionRegistrationExtensions.cs`
- Test: `backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests/Hosting/AddServiceRegistrationIntegrationTests.cs`

- [ ] **Step 1: 写失败集成测试**

在 `AddServiceRegistrationIntegrationTests.cs` 增加受控测试类型和断言：

```csharp
private sealed class IntegrationCacheOptions : IConfigurableOptions
{
    [Required]
    public string Endpoint { get; set; } = string.Empty;
}

[Fact]
public void AddServiceRegistration_BindsOptionsAndRegistersOptionsReport()
{
    var services = new ServiceCollection();
    var configuration = ConfigurationForThisTestAssembly(
        new Dictionary<string, string?>
        {
            ["IntegrationCache:Endpoint"] = "localhost",
        });

    services.AddServiceRegistration(configuration, new TestAssemblySource(typeof(IntegrationCacheOptions).Assembly));
    using var provider = services.BuildServiceProvider(validateScopes: true);

    provider.GetRequiredService<IOptions<IntegrationCacheOptions>>().Value.Endpoint.Should().Be("localhost");
    provider.GetRequiredService<OptionsBindingReport>().Items.Should()
        .ContainSingle(item => item.SectionPath == "IntegrationCache");
}
```

- [ ] **Step 2: 运行测试确认失败**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests/Tw.DependencyInjection.Tests.csproj --nologo`

Expected: 测试失败，`IOptions<IntegrationCacheOptions>` 未注册。

- [ ] **Step 3: 在入口中接入 Options**

在 `AddServiceRegistration` 获取 `typesByAssemblyName` 后、服务注册规划前增加：

```csharp
var optionsPlan = OptionsBindingPlanner.Plan(
    discovery.OrderedAssemblies,
    typesByAssemblyName,
    configuration);
OptionsBindingExecutor.Apply(services, configuration, optionsPlan.Candidates);
services.TryAddSingleton(optionsPlan.Report);
```

- [ ] **Step 4: 运行测试确认通过**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests/Tw.DependencyInjection.Tests.csproj --nologo`

Expected: PASS。

## Task 5: 文档、charter 与收尾验证

**Files:**
- Modify: `backend/dotnet/BuildingBlocks/src/Tw.DependencyInjection/package-charter.yaml`
- Modify: `docs/shared-packages/dotnet/Tw.DependencyInjection/README.md`
- Create: `docs/shared-packages/dotnet/Tw.DependencyInjection/options-binding.md`

- [ ] **Step 1: 更新 charter**

在 `in_scope` 增加：

```yaml
  - Options 自动发现、绑定、启动校验、后置配置与诊断报告
```

- [ ] **Step 2: 更新 README 索引**

```markdown
- [配置与 Options 自动装载](options-binding.md)：发现 `IConfigurableOptions`、绑定配置节、启动校验、命名 Options 与诊断报告（P3 落地）。
```

- [ ] **Step 3: 创建 How-to**

`options-binding.md` 必须包含：能力定位、`AddServiceRegistration(builder.Configuration)` 入口、默认路径推导、`[OptionsSection]`、`[OptionsName]`、DataAnnotations、`IValidateOptions<TOptions>`、`PostConfigure`、敏感配置诊断边界，并声明诊断报告不输出配置值。

- [ ] **Step 4: 运行验证**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests/Tw.DependencyInjection.Tests.csproj --nologo`

Expected: PASS。

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Tw.Core.Tests.csproj --nologo`

Expected: PASS。

