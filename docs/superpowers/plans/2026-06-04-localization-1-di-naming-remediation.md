# DI 命名整改 Implementation Plan（多语言系列 Plan 1/3）

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 把 `Tw.Core`、`Tw.AspNetCore` 现有宽泛 DI 注册入口（`AddTwCore`、`AddTwAspNetCore`）整改为按功能能力组织的功能级注册与聚合入口，为后续多语言能力提供干净命名空间。

**Architecture:** 先以纯增量方式新增功能级注册扩展与聚合入口（保持构建绿），再删除旧的宽泛扩展类与其测试、更新文档（再次保持构建绿）。功能级方法放入功能命名空间，不放入 `Microsoft.Extensions.DependencyInjection`。两个包当前未被任何微服务引用、未发布 NuGet，处于采纳前阶段，按 `docs/engineering-standards/03-project-and-code/shared-package-charter.md` 允许直接破坏性删除旧入口，不留废弃壳。

**Tech Stack:** .NET 10、C#（file-scoped namespace、nullable enable、implicit usings）、xUnit、FluentAssertions、`Microsoft.Extensions.DependencyInjection(.Abstractions)`。

**依赖关系：** 本计划是多语言系列的基础计划，无前置依赖。Plan 2（Tw.Core 多语言核心）、Plan 3（Tw.AspNetCore Web 集成）在此之上构建。

**适用规范（实现前必读）：**
- `docs/engineering-standards/03-project-and-code/language-specific/dotnet-core.md`（共享包服务注册一节）
- `docs/engineering-standards/03-project-and-code/shared-package-charter.md`（采纳前破坏性变更一节）
- 设计稿 `docs/superpowers/specs/2026-06-04-localization-abstractions-design.md`（服务注册与命名规则、兼容性两节）

**通用命令：**
- 构建解决方案：`dotnet build backend/dotnet/Tw.SmartPlatform.slnx`
- 跑 Tw.Core 测试：`dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Tw.Core.Tests.csproj`
- 跑 Tw.AspNetCore 测试：`dotnet test backend/dotnet/BuildingBlocks/tests/Tw.AspNetCore.Tests/Tw.AspNetCore.Tests.csproj`

---

## File Structure

新增：
- `backend/dotnet/BuildingBlocks/src/Tw.Core/Context/CancellationTokenServiceCollectionExtensions.cs` — 命名空间 `Tw.Context`，`AddCancellationTokenProvider()`
- `backend/dotnet/BuildingBlocks/src/Tw.AspNetCore/Context/CancellationTokenServiceCollectionExtensions.cs` — 命名空间 `Tw.AspNetCore.Context`，`AddHttpContextCancellationTokenProvider()`
- `backend/dotnet/BuildingBlocks/src/Tw.AspNetCore/DependencyInjection/WebIntegrationServiceCollectionExtensions.cs` — 命名空间 `Tw.AspNetCore`，`AddWebIntegration()`
- 对应测试三份（见各任务）

删除：
- `backend/dotnet/BuildingBlocks/src/Tw.Core/DependencyInjection/TwCoreServiceCollectionExtensions.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.AspNetCore/DependencyInjection/TwAspNetCoreServiceCollectionExtensions.cs`
- `backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/DependencyInjection/TwCoreServiceCollectionExtensionsTests.cs`
- `backend/dotnet/BuildingBlocks/tests/Tw.AspNetCore.Tests/DependencyInjection/TwAspNetCoreServiceCollectionExtensionsTests.cs`

修改：
- `backend/dotnet/BuildingBlocks/src/Tw.AspNetCore/package-charter.yaml`（如需，见 Task 4）
- `docs/shared-packages/dotnet/Tw.Core/context/cancellation-token-provider.md`
- `docs/shared-packages/dotnet/Tw.AspNetCore/context/http-context-cancellation-token-provider.md`

---

## Task 1: Tw.Core 功能级注册 `AddCancellationTokenProvider`

**Files:**
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/Context/CancellationTokenServiceCollectionExtensions.cs`
- Test: `backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Context/CancellationTokenServiceCollectionExtensionsTests.cs`

- [ ] **Step 1: Write the failing test**

创建测试文件，内容：

```csharp
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Tw.Context;
using Xunit;

namespace Tw.Core.Tests.Context;

public class CancellationTokenServiceCollectionExtensionsTests
{
    [Fact]
    public void AddCancellationTokenProvider_RegistersNullProvider_AsDefault()
    {
        var services = new ServiceCollection();

        services.AddCancellationTokenProvider();

        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<ICancellationTokenProvider>()
            .Should().BeOfType<NullCancellationTokenProvider>();
    }

    [Fact]
    public void AddCancellationTokenProvider_RegistersScopeProvider_AsSingleton()
    {
        var services = new ServiceCollection();

        services.AddCancellationTokenProvider();

        using var provider = services.BuildServiceProvider();
        var first = provider.GetRequiredService<AsyncLocalCancellationTokenScopeProvider>();
        var second = provider.GetRequiredService<AsyncLocalCancellationTokenScopeProvider>();
        first.Should().BeSameAs(second);
    }

    [Fact]
    public void AddCancellationTokenProvider_DoesNotOverride_ExistingProvider()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ICancellationTokenProvider>(
            new NullCancellationTokenProvider(new AsyncLocalCancellationTokenScopeProvider()));
        var sentinel = services.Single(d => d.ServiceType == typeof(ICancellationTokenProvider));

        services.AddCancellationTokenProvider();

        services.Single(d => d.ServiceType == typeof(ICancellationTokenProvider))
            .Should().BeSameAs(sentinel);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Tw.Core.Tests.csproj --filter CancellationTokenServiceCollectionExtensionsTests`
Expected: 编译失败，提示 `AddCancellationTokenProvider` 不存在。

- [ ] **Step 3: Write minimal implementation**

创建扩展文件，内容：

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Tw.Context;

/// <summary>
/// 为 <see cref="IServiceCollection"/> 提供取消令牌上下文能力注册扩展
/// </summary>
public static class CancellationTokenServiceCollectionExtensions
{
    /// <summary>
    /// 注册取消令牌上下文能力
    /// </summary>
    /// <param name="services">服务容器</param>
    /// <returns>同一 <see cref="IServiceCollection"/> 实例，便于链式调用</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="services"/> 为 <see langword="null"/> 时抛出</exception>
    /// <remarks>
    /// 注册 <see cref="AsyncLocalCancellationTokenScopeProvider"/> 为 singleton，
    /// 并将 <see cref="ICancellationTokenProvider"/> 默认注册为 <see cref="NullCancellationTokenProvider"/>。
    /// 已存在的同类型注册不会被覆盖。
    /// </remarks>
    public static IServiceCollection AddCancellationTokenProvider(this IServiceCollection services)
    {
        Check.NotNull(services);

        services.TryAddSingleton<AsyncLocalCancellationTokenScopeProvider>();
        services.TryAddSingleton<ICancellationTokenProvider, NullCancellationTokenProvider>();

        return services;
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Tw.Core.Tests.csproj --filter CancellationTokenServiceCollectionExtensionsTests`
Expected: PASS（3 个测试通过）。

- [ ] **Step 5: Commit**

```bash
git add backend/dotnet/BuildingBlocks/src/Tw.Core/Context/CancellationTokenServiceCollectionExtensions.cs backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Context/CancellationTokenServiceCollectionExtensionsTests.cs
git commit -m "feat(core): add AddCancellationTokenProvider feature registration"
```

---

## Task 2: Tw.AspNetCore 功能级注册 `AddHttpContextCancellationTokenProvider`

**Files:**
- Create: `backend/dotnet/BuildingBlocks/src/Tw.AspNetCore/Context/CancellationTokenServiceCollectionExtensions.cs`
- Test: `backend/dotnet/BuildingBlocks/tests/Tw.AspNetCore.Tests/Context/CancellationTokenServiceCollectionExtensionsTests.cs`

- [ ] **Step 1: Write the failing test**

创建测试文件，内容：

```csharp
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Tw.AspNetCore.Context;
using Tw.Context;
using Xunit;

namespace Tw.AspNetCore.Tests.Context;

public class CancellationTokenServiceCollectionExtensionsTests
{
    [Fact]
    public void AddHttpContextCancellationTokenProvider_ReplacesProvider_WithHttpContextProvider()
    {
        var services = new ServiceCollection();

        services.AddHttpContextCancellationTokenProvider();

        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<ICancellationTokenProvider>()
            .Should().BeOfType<HttpContextCancellationTokenProvider>();
    }

    [Fact]
    public void AddHttpContextCancellationTokenProvider_RegistersHttpContextAccessor()
    {
        var services = new ServiceCollection();

        services.AddHttpContextCancellationTokenProvider();

        using var provider = services.BuildServiceProvider();
        provider.GetService<IHttpContextAccessor>().Should().NotBeNull();
    }

    [Fact]
    public void AddHttpContextCancellationTokenProvider_RegistersScopeProvider_AsSingleton()
    {
        var services = new ServiceCollection();

        services.AddHttpContextCancellationTokenProvider();

        using var provider = services.BuildServiceProvider();
        var first = provider.GetRequiredService<AsyncLocalCancellationTokenScopeProvider>();
        var second = provider.GetRequiredService<AsyncLocalCancellationTokenScopeProvider>();
        first.Should().BeSameAs(second);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.AspNetCore.Tests/Tw.AspNetCore.Tests.csproj --filter CancellationTokenServiceCollectionExtensionsTests`
Expected: 编译失败，提示 `AddHttpContextCancellationTokenProvider` 不存在。

- [ ] **Step 3: Write minimal implementation**

创建扩展文件，内容：

```csharp
using Microsoft.Extensions.DependencyInjection;
using Tw.Context;

namespace Tw.AspNetCore.Context;

/// <summary>
/// 为 <see cref="IServiceCollection"/> 提供 ASP.NET Core 宿主取消令牌能力注册扩展
/// </summary>
public static class CancellationTokenServiceCollectionExtensions
{
    /// <summary>
    /// 注册 ASP.NET Core 宿主取消令牌能力
    /// </summary>
    /// <param name="services">服务容器</param>
    /// <returns>同一 <see cref="IServiceCollection"/> 实例，便于链式调用</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="services"/> 为 <see langword="null"/> 时抛出</exception>
    /// <remarks>
    /// 先调用 <see cref="CancellationTokenServiceCollectionExtensions.AddCancellationTokenProvider"/> 注册核心能力，
    /// 注册 <c>IHttpContextAccessor</c>，并将 <see cref="ICancellationTokenProvider"/>
    /// 替换为 <see cref="HttpContextCancellationTokenProvider"/>。
    /// </remarks>
    public static IServiceCollection AddHttpContextCancellationTokenProvider(this IServiceCollection services)
    {
        Check.NotNull(services);

        services.AddCancellationTokenProvider();
        services.AddHttpContextAccessor();
        services.Replace(
            ServiceDescriptor.Singleton<ICancellationTokenProvider, HttpContextCancellationTokenProvider>());

        return services;
    }
}
```

> 注意：`AddCancellationTokenProvider`（Tw.Core，命名空间 `Tw.Context`）与 `Replace`、`ServiceDescriptor` 通过 `using Microsoft.Extensions.DependencyInjection;` 与 `Tw.Context` 引入；`Replace` 来自 `Microsoft.Extensions.DependencyInjection.Extensions`，由 `Microsoft.Extensions.DependencyInjection` 命名空间间接可用。若编译提示 `Replace` 不存在，补 `using Microsoft.Extensions.DependencyInjection.Extensions;`。

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.AspNetCore.Tests/Tw.AspNetCore.Tests.csproj --filter CancellationTokenServiceCollectionExtensionsTests`
Expected: PASS（3 个测试通过）。

- [ ] **Step 5: Commit**

```bash
git add backend/dotnet/BuildingBlocks/src/Tw.AspNetCore/Context/CancellationTokenServiceCollectionExtensions.cs backend/dotnet/BuildingBlocks/tests/Tw.AspNetCore.Tests/Context/CancellationTokenServiceCollectionExtensionsTests.cs
git commit -m "feat(aspnetcore): add AddHttpContextCancellationTokenProvider feature registration"
```

---

## Task 3: Tw.AspNetCore 聚合入口 `AddWebIntegration`

**Files:**
- Create: `backend/dotnet/BuildingBlocks/src/Tw.AspNetCore/DependencyInjection/WebIntegrationServiceCollectionExtensions.cs`
- Test: `backend/dotnet/BuildingBlocks/tests/Tw.AspNetCore.Tests/DependencyInjection/WebIntegrationServiceCollectionExtensionsTests.cs`

- [ ] **Step 1: Write the failing test**

创建测试文件，内容：

```csharp
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Tw.AspNetCore.Context;
using Tw.Context;
using Xunit;

namespace Tw.AspNetCore.Tests.DependencyInjection;

public class WebIntegrationServiceCollectionExtensionsTests
{
    [Fact]
    public void AddWebIntegration_RegistersHttpContextProvider()
    {
        var services = new ServiceCollection();

        services.AddWebIntegration();

        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<ICancellationTokenProvider>()
            .Should().BeOfType<HttpContextCancellationTokenProvider>();
    }

    [Fact]
    public void AddWebIntegration_RegistersHttpContextAccessor()
    {
        var services = new ServiceCollection();

        services.AddWebIntegration();

        using var provider = services.BuildServiceProvider();
        provider.GetService<IHttpContextAccessor>().Should().NotBeNull();
    }

    [Fact]
    public void AddWebIntegration_ReturnsSameServices_ForChaining()
    {
        var services = new ServiceCollection();

        var result = services.AddWebIntegration();

        result.Should().BeSameAs(services);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.AspNetCore.Tests/Tw.AspNetCore.Tests.csproj --filter WebIntegrationServiceCollectionExtensionsTests`
Expected: 编译失败，提示 `AddWebIntegration` 不存在。

- [ ] **Step 3: Write minimal implementation**

创建扩展文件，内容：

```csharp
using Microsoft.Extensions.DependencyInjection;

namespace Tw.AspNetCore;

/// <summary>
/// 为 <see cref="IServiceCollection"/> 提供 <c>Tw.AspNetCore</c> Web 集成聚合注册入口
/// </summary>
/// <remarks>
/// 聚合入口按固定顺序调用本程序集内的功能级注册方法，使业务应用无需了解功能注册顺序。
/// 聚合入口不替代功能级注册方法；功能级注册方法仍可单独调用和组合。
/// </remarks>
public static class WebIntegrationServiceCollectionExtensions
{
    /// <summary>
    /// 注册 Web 集成所需的全部功能能力
    /// </summary>
    /// <param name="services">服务容器</param>
    /// <returns>同一 <see cref="IServiceCollection"/> 实例，便于链式调用</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="services"/> 为 <see langword="null"/> 时抛出</exception>
    /// <remarks>当前聚合 HTTP 请求取消令牌能力；后续 Web 功能在此追加。</remarks>
    public static IServiceCollection AddWebIntegration(this IServiceCollection services)
    {
        Check.NotNull(services);

        services.AddHttpContextCancellationTokenProvider();

        return services;
    }
}
```

> `AddHttpContextCancellationTokenProvider` 在命名空间 `Tw.AspNetCore.Context`。本文件命名空间为 `Tw.AspNetCore`，不自动可见该扩展，需补 `using Tw.AspNetCore.Context;`。在 Step 3 文件顶部加上该 using。

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.AspNetCore.Tests/Tw.AspNetCore.Tests.csproj --filter WebIntegrationServiceCollectionExtensionsTests`
Expected: PASS（3 个测试通过）。

- [ ] **Step 5: Commit**

```bash
git add backend/dotnet/BuildingBlocks/src/Tw.AspNetCore/DependencyInjection/WebIntegrationServiceCollectionExtensions.cs backend/dotnet/BuildingBlocks/tests/Tw.AspNetCore.Tests/DependencyInjection/WebIntegrationServiceCollectionExtensionsTests.cs
git commit -m "feat(aspnetcore): add AddWebIntegration aggregate registration"
```

---

## Task 4: 删除旧宽泛扩展类、旧测试，更新文档

**Files:**
- Delete: `backend/dotnet/BuildingBlocks/src/Tw.Core/DependencyInjection/TwCoreServiceCollectionExtensions.cs`
- Delete: `backend/dotnet/BuildingBlocks/src/Tw.AspNetCore/DependencyInjection/TwAspNetCoreServiceCollectionExtensions.cs`
- Delete: `backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/DependencyInjection/TwCoreServiceCollectionExtensionsTests.cs`
- Delete: `backend/dotnet/BuildingBlocks/tests/Tw.AspNetCore.Tests/DependencyInjection/TwAspNetCoreServiceCollectionExtensionsTests.cs`
- Modify: `docs/shared-packages/dotnet/Tw.Core/context/cancellation-token-provider.md`
- Modify: `docs/shared-packages/dotnet/Tw.AspNetCore/context/http-context-cancellation-token-provider.md`

- [ ] **Step 1: 删除旧扩展类与旧测试**

```bash
git rm backend/dotnet/BuildingBlocks/src/Tw.Core/DependencyInjection/TwCoreServiceCollectionExtensions.cs
git rm backend/dotnet/BuildingBlocks/src/Tw.AspNetCore/DependencyInjection/TwAspNetCoreServiceCollectionExtensions.cs
git rm backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/DependencyInjection/TwCoreServiceCollectionExtensionsTests.cs
git rm backend/dotnet/BuildingBlocks/tests/Tw.AspNetCore.Tests/DependencyInjection/TwAspNetCoreServiceCollectionExtensionsTests.cs
```

- [ ] **Step 2: 更新共享包文档里的注册方法引用**

打开 `docs/shared-packages/dotnet/Tw.Core/context/cancellation-token-provider.md`，把出现的 `AddTwCore()` 注册示例替换为 `AddCancellationTokenProvider()`，并把方法所在命名空间说明改为 `Tw.Context`（不再是 `Microsoft.Extensions.DependencyInjection`）。

打开 `docs/shared-packages/dotnet/Tw.AspNetCore/context/http-context-cancellation-token-provider.md`，把出现的 `AddTwAspNetCore()` 注册示例替换为 `AddHttpContextCancellationTokenProvider()`（命名空间 `Tw.AspNetCore.Context`），并补一句：业务应用可改用聚合入口 `AddWebIntegration()`（命名空间 `Tw.AspNetCore`）。

> 用 grep 定位需替换处：`AddTwCore`、`AddTwAspNetCore`。替换后再 grep 确认两个文档内不再出现旧方法名。

- [ ] **Step 3: 全量构建验证无残留引用**

Run: `dotnet build backend/dotnet/Tw.SmartPlatform.slnx`
Expected: 构建成功，无 `AddTwCore` / `AddTwAspNetCore` 未定义错误。若有其他文件仍引用旧方法，改为对应功能级方法（核心用 `AddCancellationTokenProvider`，Web 用 `AddHttpContextCancellationTokenProvider` 或 `AddWebIntegration`）后重新构建。

- [ ] **Step 4: 全量测试**

Run: `dotnet test backend/dotnet/Tw.SmartPlatform.slnx`
Expected: 全部通过，无编译错误。

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "refactor!: remove AddTwCore/AddTwAspNetCore broad registration entries"
```

---

## Task 5: charter 与最终验证

**Files:**
- Modify（按需）: `backend/dotnet/BuildingBlocks/src/Tw.AspNetCore/package-charter.yaml`

- [ ] **Step 1: 核对 charter `public_capabilities`**

打开 `backend/dotnet/BuildingBlocks/src/Tw.Core/package-charter.yaml` 与 `backend/dotnet/BuildingBlocks/src/Tw.AspNetCore/package-charter.yaml`。

本计划新增的扩展类命名空间为 `Tw.Context`、`Tw.AspNetCore.Context`、`Tw.AspNetCore`，三者均已在各自 charter 的 `public_capabilities` 列表中（`Tw.Core` 含 `Tw.Context`；`Tw.AspNetCore` 含 `Tw.AspNetCore`、`Tw.AspNetCore.Context`）。

若已存在则不改动；若发现缺失则补齐。本计划不新增 `Tw.Localization` 等命名空间（属于 Plan 2/3）。

- [ ] **Step 2: 全量构建 + 测试最终确认**

Run: `dotnet build backend/dotnet/Tw.SmartPlatform.slnx`
Expected: 构建成功。

Run: `dotnet test backend/dotnet/Tw.SmartPlatform.slnx`
Expected: 全部通过。

- [ ] **Step 3: 确认目标 API 不含旧名（人工核查）**

用 grep 在 `backend/dotnet/BuildingBlocks/src` 与 `tests` 下检索 `AddTwCore`、`AddTwAspNetCore`、`TwCoreServiceCollectionExtensions`、`TwAspNetCoreServiceCollectionExtensions`。
Expected: 无命中（设计稿与历史 plan 文档中的提及不计）。

- [ ] **Step 4: Commit（若 Step 1 有改动）**

```bash
git add backend/dotnet/BuildingBlocks/src/Tw.AspNetCore/package-charter.yaml backend/dotnet/BuildingBlocks/src/Tw.Core/package-charter.yaml
git commit -m "docs(charter): confirm DI capability namespaces after remediation"
```

若 Step 1 无改动，跳过本步。

---

## 完成标准

- `AddCancellationTokenProvider`（`Tw.Context`）、`AddHttpContextCancellationTokenProvider`（`Tw.AspNetCore.Context`）、`AddWebIntegration`（`Tw.AspNetCore`）三个入口存在且有测试覆盖。
- 旧 `AddTwCore`、`AddTwAspNetCore` 及其宽泛扩展类、`Microsoft.Extensions.DependencyInjection` 命名空间占位、旧测试全部移除。
- 共享包文档不再引用旧方法名。
- `dotnet build` 与 `dotnet test` 全量通过。
