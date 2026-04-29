# Cross-Cutting Quality Gates Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Close the P0/P1 quality gates for diagnostics, exception safety, XML comments, localization checks, and verification evidence.

**Architecture:** Diagnostics are generated during scan, sort, registration, and chain computation, then surfaced through a small report object that tests can assert. Quality gates remain automated where practical and documented only where automation cannot reliably prove a constraint.

**Tech Stack:** .NET 10, Tw.Core, Tw.AspNetCore, xUnit, FluentAssertions

---

## Execution Order

Run after `2026-04-29-04-aspnetcore-mvc-adapter.md`. This plan must pass before `2026-04-29-06-grpc-adapter-p2.md`.

## Source Inputs

- Design sections: § 8, § 10.4, § 11
- Standards: `rules.comments-dotnet#rules` version `1.3.0`, `docs/standards/rules/comments-dotnet.md`; `rules.error-handling#rules` version `1.2.0`, `docs/standards/rules/error-handling.md`; `rules.test-strategy#rules` version `1.1.0`, `docs/standards/rules/test-strategy.md`; `rules.dependency-policy#rules` version `1.1.0`, `docs/standards/rules/dependency-policy.md`

## File Structure

- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/DependencyInjection/Diagnostics/AutoRegistrationDiagnosticReport.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/DependencyInjection/Diagnostics/AutoRegistrationDiagnosticSink.cs`
- Modify: `backend/dotnet/BuildingBlocks/src/Tw.Core/DependencyInjection/AutoRegistrationAssemblySelector.cs`
- Modify: `backend/dotnet/BuildingBlocks/src/Tw.Core/DependencyInjection/AssemblyTopologySorter.cs`
- Modify: `backend/dotnet/BuildingBlocks/src/Tw.Core/DependencyInjection/Autofac/AutoRegistrationModule.cs`
- Modify: `backend/dotnet/BuildingBlocks/src/Tw.Core/Aop/ServiceChainCache.cs`
- Modify: `backend/dotnet/BuildingBlocks/src/Tw.AspNetCore/Aop/Mvc/EntryChainCache.cs`
- Modify: `backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/SourceLocalizationRulesTests.cs`
- Create: `backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/DependencyInjection/Diagnostics/AutoRegistrationDiagnosticTests.cs`
- Create: `backend/dotnet/BuildingBlocks/tests/Tw.AspNetCore.Tests/SourceLocalizationRulesTests.cs`
- Modify: `backend/dotnet/BuildingBlocks/src/Tw.Core/README.md`
- Modify: `backend/dotnet/BuildingBlocks/src/Tw.AspNetCore/README.md`
- Modify: `docs/superpowers/dependencies/2026-04-29-autofac-castle-grpc-admission.md`

## Tasks

### Task 1: Add Diagnostic Report API

**Files:**
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/DependencyInjection/Diagnostics/AutoRegistrationDiagnosticReport.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/DependencyInjection/Diagnostics/AutoRegistrationDiagnosticSink.cs`
- Test: `backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/DependencyInjection/Diagnostics/AutoRegistrationDiagnosticTests.cs`

- [ ] **Step 1: Write failing diagnostic tests**

```csharp
[Fact]
public void DiagnosticSink_Records_Phase_Durations_And_Warnings()
{
    var sink = new AutoRegistrationDiagnosticSink();

    sink.RecordPhase("Scan", TimeSpan.FromMilliseconds(42), "12 assemblies, 1847 types");
    sink.Warn("仅暴露具体类型 PureWorker，默认不启用 Castle 类代理。");

    var report = sink.Build();

    report.Phases.Should().ContainSingle(phase =>
        phase.Name == "Scan" &&
        phase.Elapsed == TimeSpan.FromMilliseconds(42) &&
        phase.Detail == "12 assemblies, 1847 types");
    report.Warnings.Should().ContainSingle().Which.Should().Contain("PureWorker");
}
```

- [ ] **Step 2: Run test to verify it fails**

Run:

```powershell
dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Tw.Core.Tests.csproj --filter DiagnosticSink_Records_Phase_Durations_And_Warnings
```

Expected: FAIL because diagnostic types do not exist.

- [ ] **Step 3: Create diagnostic report types**

```csharp
namespace Tw.Core.DependencyInjection.Diagnostics;

/// <summary>描述自动注册一个启动阶段的耗时和细节</summary>
public sealed record AutoRegistrationDiagnosticPhase(string Name, TimeSpan Elapsed, string Detail);

/// <summary>描述自动注册启动期诊断结果</summary>
public sealed record AutoRegistrationDiagnosticReport(
    IReadOnlyList<AutoRegistrationDiagnosticPhase> Phases,
    IReadOnlyList<string> Warnings);

/// <summary>收集自动注册启动期诊断信息</summary>
public sealed class AutoRegistrationDiagnosticSink
{
    private readonly List<AutoRegistrationDiagnosticPhase> _phases = [];
    private readonly List<string> _warnings = [];

    /// <summary>记录一个启动阶段耗时</summary>
    public void RecordPhase(string name, TimeSpan elapsed, string detail)
    {
        _phases.Add(new AutoRegistrationDiagnosticPhase(name, elapsed, detail));
    }

    /// <summary>记录一个启动警告</summary>
    public void Warn(string message)
    {
        _warnings.Add(Check.NotNullOrWhiteSpace(message));
    }

    /// <summary>生成不可变诊断报告</summary>
    public AutoRegistrationDiagnosticReport Build()
    {
        return new AutoRegistrationDiagnosticReport(_phases.ToArray(), _warnings.ToArray());
    }
}
```

- [ ] **Step 4: Run diagnostic tests**

Run:

```powershell
dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Tw.Core.Tests.csproj --filter FullyQualifiedName~AutoRegistrationDiagnosticTests
```

Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add backend/dotnet/BuildingBlocks/src/Tw.Core/DependencyInjection/Diagnostics backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/DependencyInjection/Diagnostics
git commit -m "feat: add auto registration diagnostics"
```

### Task 2: Emit Scan, Sort, Register, and AOP Diagnostics

**Files:**
- Modify: `backend/dotnet/BuildingBlocks/src/Tw.Core/DependencyInjection/AutoRegistrationAssemblySelector.cs`
- Modify: `backend/dotnet/BuildingBlocks/src/Tw.Core/DependencyInjection/AssemblyTopologySorter.cs`
- Modify: `backend/dotnet/BuildingBlocks/src/Tw.Core/DependencyInjection/Autofac/AutoRegistrationModule.cs`
- Modify: `backend/dotnet/BuildingBlocks/src/Tw.Core/Aop/ServiceChainCache.cs`
- Modify: `backend/dotnet/BuildingBlocks/src/Tw.AspNetCore/Aop/Mvc/EntryChainCache.cs`
- Test: `backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/DependencyInjection/Diagnostics/AutoRegistrationDiagnosticTests.cs`

- [ ] **Step 1: Add failing phase coverage test**

```csharp
[Fact]
public void AutoRegistrationModule_Reports_Expected_Phases()
{
    var services = new ServiceCollection();
    var diagnostics = new AutoRegistrationDiagnosticSink();
    services.AddSingleton(diagnostics);
    services.AddAutoRegistration(options => options.AddAssemblyOf<DiagnosticService>());

    var builder = new ContainerBuilder();
    builder.Populate(services);
    builder.UseAutoRegistration(services);

    using var container = builder.Build();
    var report = diagnostics.Build();

    report.Phases.Select(phase => phase.Name)
        .Should()
        .Contain(["Scan", "Sort", "Register", "AOP", "Total"]);
}

private interface IDiagnosticService;
private sealed class DiagnosticService : IDiagnosticService, IScopedDependency;
```

- [ ] **Step 2: Run test to verify it fails**

Run:

```powershell
dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Tw.Core.Tests.csproj --filter AutoRegistrationModule_Reports_Expected_Phases
```

Expected: FAIL because module code does not record phases.

- [ ] **Step 3: Wire diagnostics through registration**

Update the scanner, topology sorter, Autofac module, and chain caches so they:

```text
1. Accept AutoRegistrationDiagnosticSink as an optional dependency.
2. Record Scan, Sort, Register, AOP, and Total phases with elapsed time and counts.
3. Warn when concrete-only services have a non-empty Service chain and class proxy is not enabled.
4. Warn when Controller classes carry InterceptAttribute because Entry Adapter ignores it.
5. Warn when a method contains more than one CancellationToken parameter.
6. Keep all diagnostics at startup computation time.
```

- [ ] **Step 4: Run diagnostics tests**

Run:

```powershell
dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Tw.Core.Tests.csproj --filter FullyQualifiedName~AutoRegistrationDiagnosticTests
dotnet test backend/dotnet/BuildingBlocks/tests/Tw.AspNetCore.Tests/Tw.AspNetCore.Tests.csproj --filter FullyQualifiedName~MvcEntryChainCacheTests
```

Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add backend/dotnet/BuildingBlocks/src/Tw.Core/DependencyInjection backend/dotnet/BuildingBlocks/src/Tw.Core/Aop backend/dotnet/BuildingBlocks/src/Tw.AspNetCore/Aop/Mvc backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/DependencyInjection/Diagnostics backend/dotnet/BuildingBlocks/tests/Tw.AspNetCore.Tests/Aop
git commit -m "feat: emit auto registration diagnostics"
```

### Task 3: Extend Localization and Comment Gates

**Files:**
- Modify: `backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/SourceLocalizationRulesTests.cs`
- Create: `backend/dotnet/BuildingBlocks/tests/Tw.AspNetCore.Tests/SourceLocalizationRulesTests.cs`

- [ ] **Step 1: Include new Tw.Core directories**

Update the existing `SourceRootPaths` in `Tw.Core.Tests` so it still includes:

```csharp
private static readonly string[] SourceRootPaths =
[
    "backend/dotnet/BuildingBlocks/src/Tw.Core",
    "backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests",
    "backend/dotnet/BuildingBlocks/tests/Tw.TestBase"
];
```

This keeps the new `DependencyInjection`, `Aop`, and `Cancellation` directories under the existing root.

- [ ] **Step 2: Add `Tw.AspNetCore` localization gate**

Create `Tw.AspNetCore.Tests/SourceLocalizationRulesTests.cs` by copying the existing source localization rule test and changing only the roots:

```csharp
private static readonly string[] SourceRootPaths =
[
    "backend/dotnet/BuildingBlocks/src/Tw.AspNetCore",
    "backend/dotnet/BuildingBlocks/tests/Tw.AspNetCore.Tests",
    "backend/dotnet/BuildingBlocks/tests/Tw.TestBase"
];
```

- [ ] **Step 3: Run localization gates**

Run:

```powershell
dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Tw.Core.Tests.csproj --filter FullyQualifiedName~SourceLocalizationRulesTests
dotnet test backend/dotnet/BuildingBlocks/tests/Tw.AspNetCore.Tests/Tw.AspNetCore.Tests.csproj --filter FullyQualifiedName~SourceLocalizationRulesTests
```

Expected: PASS. Public XML comments use simplified Chinese, comments do not end with Chinese or English periods, and explicit exception messages use simplified Chinese.

- [ ] **Step 4: Commit**

```bash
git add backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/SourceLocalizationRulesTests.cs backend/dotnet/BuildingBlocks/tests/Tw.AspNetCore.Tests/SourceLocalizationRulesTests.cs
git commit -m "test: enforce localization rules for aspnetcore"
```

### Task 4: Update Building Block Documentation

**Files:**
- Modify: `backend/dotnet/BuildingBlocks/src/Tw.Core/README.md`
- Modify: `backend/dotnet/BuildingBlocks/src/Tw.AspNetCore/README.md`
- Modify: `docs/superpowers/dependencies/2026-04-29-autofac-castle-grpc-admission.md`

- [ ] **Step 1: Document `Tw.Core` public surface**

Add these sections to `Tw.Core/README.md`:

```markdown
## DI 自动注册

`Tw.Core.DependencyInjection` 提供生命周期标记接口、注册控制特性、自动扫描、冲突裁决和 Autofac 注册模块。业务类型通过 `ITransientDependency`、`IScopedDependency` 或 `ISingletonDependency` 参与自动注册。

## Service AOP

`Tw.Core.Aop` 提供 `InterceptorBase`、`IInvocationContext`、`InterceptorScope.Service` 和 Castle 代理适配。Service AOP 作用于通过 DI 解析的服务方法调用，不作用于 MVC Controller 入口边界。

## 配置选项自动注册

选项类型通过 `IConfigurableOptions` 或 `[ConfigurationSection]` 参与自动注册。命名选项通过 `OptionsName` 访问，直接注入只对默认命名实例生效。
```

- [ ] **Step 2: Document `Tw.AspNetCore` public surface**

Add these sections to `Tw.AspNetCore/README.md`:

```markdown
## ASP.NET Core 基础设施入口

`AddTwAspNetCoreInfrastructure()` 注册 Tw 平台在 ASP.NET Core 中需要的基础设施，包括 Autofac 容器接管、HTTP 取消令牌提供者、DI 自动注册和 MVC/Web API Entry AOP Adapter。

## MVC/Web API Entry AOP

MVC/Web API Adapter 通过 `IConfigureOptions<MvcOptions>` 注册为全局 Action Filter，覆盖 Controller Action 调用边界。Controller 上的 `[Intercept]` 不启用 Castle 代理；需要 Entry 拦截时使用全局 Entry 拦截器或 `IAutoMatchInterceptor.MatchEntry()`。
```

- [ ] **Step 3: Record final dependency evidence**

Append the latest validation output to `docs/superpowers/dependencies/2026-04-29-autofac-castle-grpc-admission.md`:

```markdown
## P0/P1 Quality Evidence

| Command | Result |
| --- | --- |
| `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Tw.Core.Tests.csproj` | PASS |
| `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.AspNetCore.Tests/Tw.AspNetCore.Tests.csproj` | PASS |
| `dotnet list backend/dotnet/BuildingBlocks/src/Tw.Core/Tw.Core.csproj package --vulnerable --include-transitive` | PASS |
| `dotnet list backend/dotnet/BuildingBlocks/src/Tw.AspNetCore/Tw.AspNetCore.csproj package --vulnerable --include-transitive` | PASS |
```

- [ ] **Step 4: Commit**

```bash
git add backend/dotnet/BuildingBlocks/src/Tw.Core/README.md backend/dotnet/BuildingBlocks/src/Tw.AspNetCore/README.md docs/superpowers/dependencies/2026-04-29-autofac-castle-grpc-admission.md
git commit -m "docs: document auto registration and aop surfaces"
```

### Task 5: Final P0/P1 Verification

**Files:**
- Verify: `backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Tw.Core.Tests.csproj`
- Verify: `backend/dotnet/BuildingBlocks/tests/Tw.AspNetCore.Tests/Tw.AspNetCore.Tests.csproj`

- [ ] **Step 1: Run all BuildingBlocks tests created so far**

Run:

```powershell
dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Tw.Core.Tests.csproj
dotnet test backend/dotnet/BuildingBlocks/tests/Tw.AspNetCore.Tests/Tw.AspNetCore.Tests.csproj
```

Expected: PASS.

- [ ] **Step 2: Run package scans**

Run:

```powershell
dotnet list backend/dotnet/BuildingBlocks/src/Tw.Core/Tw.Core.csproj package --vulnerable --include-transitive
dotnet list backend/dotnet/BuildingBlocks/src/Tw.AspNetCore/Tw.AspNetCore.csproj package --vulnerable --include-transitive
```

Expected: no high or critical vulnerability.

## Self-Review Checklist

- [ ] Diagnostics include Scan, Sort, Register, AOP, and Total phases.
- [ ] Concrete-only proxy limitations and Controller `[Intercept]` misuse are visible as startup warnings.
- [ ] XML comments exist for public APIs and follow simplified Chinese comment rules.
- [ ] Explicit framework exceptions use simplified Chinese messages and do not expose secret values.
- [ ] Test evidence covers unit, integration, configuration, and adapter boundaries for P0/P1.
- [ ] Dependency admission record has final validation evidence before P2 begins.
