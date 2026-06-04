# Tw.AspNetCore Web 本地化集成 Implementation Plan（多语言系列 Plan 3/3）

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 在 `Tw.AspNetCore` 提供 Web 边界本地化集成：请求语言解析中间件、当前语言上下文作用域、`IStringLocalizer` 同步适配、Web 侧 `AddLocalization`/`UseLocalization` 与聚合入口接入、运行时导出 DTO 契约。

**Architecture:** 中间件按 route→query→cookie→`Accept-Language`→默认 顺序解析 culture，写入 scoped `ICurrentLocalizationContextAccessor`（命名空间 `Tw.AspNetCore.Localization`）并同步设置 `CultureInfo.CurrentCulture/CurrentUICulture`；业务代码注入访问器获取当前 `LocalizationContext`，不直接依赖 `HttpContext`。`IStringLocalizer` 适配器**同步**读取 Plan 2 的 `IStaticTextSnapshot`（静态 JSON），缺失返回 key，不阻塞调用异步 `ITextLocalizer`；动态覆盖与异步查询走 `ITextLocalizer`。

**Tech Stack:** .NET 10、ASP.NET Core（`Microsoft.AspNetCore.App` 框架引用，含 `Microsoft.Extensions.Localization`）、xUnit、FluentAssertions、`DefaultHttpContext` 测试。

**前置依赖：** Plan 1（DI 整改）、Plan 2（Tw.Core 多语言核心，含 `IStaticTextSnapshot`、`AddLocalization`、`ITextLocalizer`）已完成。

**适用规范（实现前必读）：**
- 设计稿 `docs/superpowers/specs/2026-06-04-localization-abstractions-design.md`（Web 集成、运行时导出 API、错误处理、测试策略各节）
- `docs/engineering-standards/03-project-and-code/language-specific/dotnet-core.md`（禁止同步阻塞异步、共享包服务注册）
- `docs/engineering-standards/03-project-and-code/shared-package-charter.md`

**通用命令：**
- 构建：`dotnet build backend/dotnet/Tw.SmartPlatform.slnx`
- 测试 Tw.AspNetCore：`dotnet test backend/dotnet/BuildingBlocks/tests/Tw.AspNetCore.Tests/Tw.AspNetCore.Tests.csproj`

**约定：** 公共类型必须有中文 DocFX XML 注释；入参 `Check.NotNull`；不在 `Microsoft.Extensions.DependencyInjection` 命名空间放自有扩展类。

---

## File Structure

源码（`backend/dotnet/BuildingBlocks/src/Tw.AspNetCore/Localization/`，命名空间 `Tw.AspNetCore.Localization`，除注明外）：
- `ICurrentLocalizationContextAccessor.cs`、`CurrentLocalizationContextAccessor.cs` — scoped 当前上下文访问器
- `RequestCultureResolver.cs` — 纯解析逻辑（不依赖 HttpContext）
- `RequestCultureResolveResult.cs` — 解析结果（culture + 是否显式切换）
- `RequestLocalizationMiddleware.cs` — 中间件
- `LocalizationApplicationBuilderExtensions.cs` — `UseLocalization(...)`（命名空间 `Tw.AspNetCore.Localization`）
- `TwStringLocalizer.cs`、`TwStringLocalizerOfT.cs`、`TwStringLocalizerFactory.cs` — `IStringLocalizer` 适配
- `LocalizationResourceDto.cs`、`LocalizationTextDto.cs` — 运行时导出 DTO 契约
- `LocalizationServiceCollectionExtensions.cs`（命名空间 `Tw.AspNetCore.Localization`）— `AddLocalization(...)`

修改：
- `backend/dotnet/BuildingBlocks/src/Tw.AspNetCore/DependencyInjection/WebIntegrationServiceCollectionExtensions.cs` — `AddWebIntegration` 追加本地化注册
- `backend/dotnet/BuildingBlocks/src/Tw.AspNetCore/package-charter.yaml` — `public_capabilities` 增 `Tw.AspNetCore.Localization`
- 共享包文档 + 索引

测试（`backend/dotnet/BuildingBlocks/tests/Tw.AspNetCore.Tests/Localization/`）：每单元一份。

---

## Task 1: 当前语言上下文访问器

**Files:**
- Create: `backend/dotnet/BuildingBlocks/src/Tw.AspNetCore/Localization/ICurrentLocalizationContextAccessor.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.AspNetCore/Localization/CurrentLocalizationContextAccessor.cs`
- Test: `backend/dotnet/BuildingBlocks/tests/Tw.AspNetCore.Tests/Localization/CurrentLocalizationContextAccessorTests.cs`

- [ ] **Step 1: Write the failing test**

```csharp
using FluentAssertions;
using Tw.AspNetCore.Localization;
using Tw.Localization;
using Xunit;

namespace Tw.AspNetCore.Tests.Localization;

public class CurrentLocalizationContextAccessorTests
{
    [Fact]
    public void Current_DefaultsToNull()
    {
        new CurrentLocalizationContextAccessor().Current.Should().BeNull();
    }

    [Fact]
    public void Current_RoundTripsAssignedValue()
    {
        var accessor = new CurrentLocalizationContextAccessor
        {
            Current = new LocalizationContext("zh-Hans") { TenantId = "t1" },
        };

        accessor.Current!.CultureName.Should().Be("zh-Hans");
        accessor.Current!.TenantId.Should().Be("t1");
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.AspNetCore.Tests/Tw.AspNetCore.Tests.csproj --filter CurrentLocalizationContextAccessorTests`
Expected: 编译失败。

- [ ] **Step 3: Write minimal implementation**

`ICurrentLocalizationContextAccessor.cs`:

```csharp
using Tw.Localization;

namespace Tw.AspNetCore.Localization;

/// <summary>提供当前请求作用域内已解析的本地化上下文，业务代码据此构建查询而不依赖 <c>HttpContext</c>。</summary>
public interface ICurrentLocalizationContextAccessor
{
    /// <summary>当前本地化上下文；请求未解析时为 <see langword="null"/>。</summary>
    LocalizationContext? Current { get; set; }
}
```

`CurrentLocalizationContextAccessor.cs`:

```csharp
using Tw.Localization;

namespace Tw.AspNetCore.Localization;

/// <summary>scoped 当前本地化上下文访问器的默认实现。</summary>
public sealed class CurrentLocalizationContextAccessor : ICurrentLocalizationContextAccessor
{
    /// <inheritdoc />
    public LocalizationContext? Current { get; set; }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.AspNetCore.Tests/Tw.AspNetCore.Tests.csproj --filter CurrentLocalizationContextAccessorTests`
Expected: PASS。

- [ ] **Step 5: Commit**

```bash
git add backend/dotnet/BuildingBlocks/src/Tw.AspNetCore/Localization/ICurrentLocalizationContextAccessor.cs backend/dotnet/BuildingBlocks/src/Tw.AspNetCore/Localization/CurrentLocalizationContextAccessor.cs backend/dotnet/BuildingBlocks/tests/Tw.AspNetCore.Tests/Localization/CurrentLocalizationContextAccessorTests.cs
git commit -m "feat(aspnetcore): add scoped current localization context accessor"
```

---

## Task 2: 请求语言解析逻辑

**Files:**
- Create: `backend/dotnet/BuildingBlocks/src/Tw.AspNetCore/Localization/RequestCultureResolveResult.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.AspNetCore/Localization/RequestCultureResolver.cs`
- Test: `backend/dotnet/BuildingBlocks/tests/Tw.AspNetCore.Tests/Localization/RequestCultureResolverTests.cs`

解析顺序：route culture → query `culture` → cookie → `Accept-Language` → 默认。命中的 culture 必须在支持列表内，否则跳过该来源。`IsExplicitSwitch` 为 true 当且仅当 route 或 query 命中（用于 cookie 写入策略）。解析逻辑接收原始字符串输入，不依赖 `HttpContext`，便于单测。

- [ ] **Step 1: Write the failing test**

```csharp
using FluentAssertions;
using Tw.AspNetCore.Localization;
using Xunit;

namespace Tw.AspNetCore.Tests.Localization;

public class RequestCultureResolverTests
{
    private static readonly string[] Supported = { "en-US", "zh-Hans" };
    private const string Default = "en-US";

    [Fact]
    public void Resolve_PrefersRoute()
    {
        var result = RequestCultureResolver.Resolve(
            routeCulture: "zh-Hans", queryCulture: "en-US", cookieCulture: null,
            acceptLanguage: null, Supported, Default);

        result.Culture.Should().Be("zh-Hans");
        result.IsExplicitSwitch.Should().BeTrue();
    }

    [Fact]
    public void Resolve_FallsToQuery_WhenNoRoute()
    {
        var result = RequestCultureResolver.Resolve(null, "zh-Hans", null, null, Supported, Default);
        result.Culture.Should().Be("zh-Hans");
        result.IsExplicitSwitch.Should().BeTrue();
    }

    [Fact]
    public void Resolve_FallsToCookie_NotExplicit()
    {
        var result = RequestCultureResolver.Resolve(null, null, "zh-Hans", null, Supported, Default);
        result.Culture.Should().Be("zh-Hans");
        result.IsExplicitSwitch.Should().BeFalse();
    }

    [Fact]
    public void Resolve_FallsToAcceptLanguage()
    {
        var result = RequestCultureResolver.Resolve(null, null, null, "zh-Hans,en;q=0.8", Supported, Default);
        result.Culture.Should().Be("zh-Hans");
        result.IsExplicitSwitch.Should().BeFalse();
    }

    [Fact]
    public void Resolve_FallsToDefault_WhenNothingMatches()
    {
        var result = RequestCultureResolver.Resolve(null, null, null, "fr-FR", Supported, Default);
        result.Culture.Should().Be("en-US");
        result.IsExplicitSwitch.Should().BeFalse();
    }

    [Fact]
    public void Resolve_SkipsUnsupportedSource()
    {
        // route 不支持 → 跳到 query
        var result = RequestCultureResolver.Resolve("fr-FR", "zh-Hans", null, null, Supported, Default);
        result.Culture.Should().Be("zh-Hans");
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.AspNetCore.Tests/Tw.AspNetCore.Tests.csproj --filter RequestCultureResolverTests`
Expected: 编译失败。

- [ ] **Step 3: Write minimal implementation**

`RequestCultureResolveResult.cs`:

```csharp
namespace Tw.AspNetCore.Localization;

/// <summary>请求语言解析结果。</summary>
/// <param name="Culture">解析得到的 culture 名称。</param>
/// <param name="IsExplicitSwitch">是否由 route 或 query 明确切换语言。</param>
public readonly record struct RequestCultureResolveResult(string Culture, bool IsExplicitSwitch);
```

`RequestCultureResolver.cs`:

```csharp
namespace Tw.AspNetCore.Localization;

/// <summary>按固定来源顺序解析请求语言，不依赖 <c>HttpContext</c>。</summary>
public static class RequestCultureResolver
{
    /// <summary>解析请求语言。</summary>
    /// <param name="routeCulture">route 中的 culture，可空。</param>
    /// <param name="queryCulture">query <c>culture</c> 值，可空。</param>
    /// <param name="cookieCulture">cookie 中的 culture，可空。</param>
    /// <param name="acceptLanguage"><c>Accept-Language</c> 头原始值，可空。</param>
    /// <param name="supportedCultures">支持语言列表。</param>
    /// <param name="defaultCulture">默认 culture。</param>
    public static RequestCultureResolveResult Resolve(
        string? routeCulture, string? queryCulture, string? cookieCulture,
        string? acceptLanguage, IReadOnlyCollection<string> supportedCultures, string defaultCulture)
    {
        Check.NotNull(supportedCultures);
        Check.NotNull(defaultCulture);

        if (TryMatch(routeCulture, supportedCultures, out var fromRoute))
            return new RequestCultureResolveResult(fromRoute, IsExplicitSwitch: true);
        if (TryMatch(queryCulture, supportedCultures, out var fromQuery))
            return new RequestCultureResolveResult(fromQuery, IsExplicitSwitch: true);
        if (TryMatch(cookieCulture, supportedCultures, out var fromCookie))
            return new RequestCultureResolveResult(fromCookie, IsExplicitSwitch: false);

        foreach (var candidate in ParseAcceptLanguage(acceptLanguage))
        {
            if (TryMatch(candidate, supportedCultures, out var fromHeader))
                return new RequestCultureResolveResult(fromHeader, IsExplicitSwitch: false);
        }

        return new RequestCultureResolveResult(defaultCulture, IsExplicitSwitch: false);
    }

    private static bool TryMatch(string? value, IReadOnlyCollection<string> supported, out string matched)
    {
        matched = string.Empty;
        if (string.IsNullOrWhiteSpace(value))
            return false;
        foreach (var s in supported)
        {
            if (string.Equals(s, value, StringComparison.OrdinalIgnoreCase))
            {
                matched = s;
                return true;
            }
        }
        return false;
    }

    private static IEnumerable<string> ParseAcceptLanguage(string? header)
    {
        if (string.IsNullOrWhiteSpace(header))
            yield break;
        // 按 q 值降序；简单实现：保留出现顺序（浏览器通常已按优先级排列）。
        foreach (var part in header.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var lang = part.Split(';', 2)[0].Trim();
            if (!string.IsNullOrEmpty(lang))
                yield return lang;
        }
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.AspNetCore.Tests/Tw.AspNetCore.Tests.csproj --filter RequestCultureResolverTests`
Expected: PASS。

- [ ] **Step 5: Commit**

```bash
git add backend/dotnet/BuildingBlocks/src/Tw.AspNetCore/Localization/RequestCultureResolveResult.cs backend/dotnet/BuildingBlocks/src/Tw.AspNetCore/Localization/RequestCultureResolver.cs backend/dotnet/BuildingBlocks/tests/Tw.AspNetCore.Tests/Localization/RequestCultureResolverTests.cs
git commit -m "feat(aspnetcore): add request culture resolver"
```

---

## Task 3: 请求语言中间件与 `UseLocalization`

**Files:**
- Create: `backend/dotnet/BuildingBlocks/src/Tw.AspNetCore/Localization/RequestLocalizationMiddleware.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.AspNetCore/Localization/LocalizationApplicationBuilderExtensions.cs`
- Test: `backend/dotnet/BuildingBlocks/tests/Tw.AspNetCore.Tests/Localization/RequestLocalizationMiddlewareTests.cs`

中间件职责：从 `HttpContext` 取 route(`culture`)、query(`culture`)、cookie(`.Tw.Culture`)、`Accept-Language` → 调 `RequestCultureResolver` → 写 `ICurrentLocalizationContextAccessor.Current`（TenantId 首轮为 `null`，租户解析由业务应用扩展）→ 设置 `CultureInfo.CurrentCulture/CurrentUICulture` → 当 `IsExplicitSwitch` 为 true 时写 cookie → 调 next。

- [ ] **Step 1: Write the failing test**

```csharp
using System.Globalization;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Tw.AspNetCore.Localization;
using Tw.Localization;
using Xunit;

namespace Tw.AspNetCore.Tests.Localization;

public class RequestLocalizationMiddlewareTests
{
    private static (RequestLocalizationMiddleware Mw, CurrentLocalizationContextAccessor Accessor) Build()
    {
        var options = new LocalizationOptions { DefaultCulture = "en-US", SupportedCultures = { "en-US", "zh-Hans" } };
        var accessor = new CurrentLocalizationContextAccessor();
        var mw = new RequestLocalizationMiddleware(_ => Task.CompletedTask, options);
        return (mw, accessor);
    }

    [Fact]
    public async Task Invoke_SetsAccessorFromQuery()
    {
        var (mw, accessor) = Build();
        var ctx = new DefaultHttpContext();
        ctx.Request.QueryString = new QueryString("?culture=zh-Hans");

        await mw.InvokeAsync(ctx, accessor);

        accessor.Current!.CultureName.Should().Be("zh-Hans");
        CultureInfo.CurrentUICulture.Name.Should().Be("zh-Hans");
    }

    [Fact]
    public async Task Invoke_WritesCookie_OnExplicitSwitch()
    {
        var (mw, accessor) = Build();
        var ctx = new DefaultHttpContext();
        ctx.Request.QueryString = new QueryString("?culture=zh-Hans");

        await mw.InvokeAsync(ctx, accessor);

        ctx.Response.Headers.SetCookie.ToString().Should().Contain(".Tw.Culture");
    }

    [Fact]
    public async Task Invoke_DoesNotWriteCookie_WhenNoExplicitSwitch()
    {
        var (mw, accessor) = Build();
        var ctx = new DefaultHttpContext();
        ctx.Request.Headers.AcceptLanguage = "zh-Hans";

        await mw.InvokeAsync(ctx, accessor);

        ctx.Response.Headers.SetCookie.ToString().Should().NotContain(".Tw.Culture");
        accessor.Current!.CultureName.Should().Be("zh-Hans");
    }

    [Fact]
    public async Task Invoke_FallsBackToDefault()
    {
        var (mw, accessor) = Build();
        var ctx = new DefaultHttpContext();

        await mw.InvokeAsync(ctx, accessor);

        accessor.Current!.CultureName.Should().Be("en-US");
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.AspNetCore.Tests/Tw.AspNetCore.Tests.csproj --filter RequestLocalizationMiddlewareTests`
Expected: 编译失败。

- [ ] **Step 3: Write minimal implementation**

`RequestLocalizationMiddleware.cs`:

```csharp
using System.Globalization;
using Microsoft.AspNetCore.Http;
using Tw.Localization;

namespace Tw.AspNetCore.Localization;

/// <summary>解析请求语言并写入当前本地化上下文作用域的中间件。</summary>
public sealed class RequestLocalizationMiddleware
{
    /// <summary>承载解析结果的 cookie 名。</summary>
    public const string CultureCookieName = ".Tw.Culture";

    private readonly RequestDelegate _next;
    private readonly LocalizationOptions _options;

    /// <summary>初始化中间件。</summary>
    public RequestLocalizationMiddleware(RequestDelegate next, LocalizationOptions options)
    {
        _next = Check.NotNull(next);
        _options = Check.NotNull(options);
    }

    /// <summary>解析语言、写入访问器与线程 culture，并按策略写 cookie。</summary>
    public async Task InvokeAsync(HttpContext context, ICurrentLocalizationContextAccessor accessor)
    {
        Check.NotNull(context);
        Check.NotNull(accessor);

        var routeCulture = context.Request.RouteValues.TryGetValue("culture", out var rv) ? rv?.ToString() : null;
        var queryCulture = context.Request.Query.TryGetValue("culture", out var qv) ? qv.ToString() : null;
        var cookieCulture = context.Request.Cookies.TryGetValue(CultureCookieName, out var cv) ? cv : null;
        var acceptLanguage = context.Request.Headers.AcceptLanguage.ToString();

        var result = RequestCultureResolver.Resolve(
            routeCulture, queryCulture, cookieCulture, acceptLanguage,
            (IReadOnlyCollection<string>)_options.SupportedCultures, _options.DefaultCulture);

        accessor.Current = new LocalizationContext(result.Culture)
        {
            FallbackToParentCultures = _options.FallbackToParentCultures,
            FallbackToDefaultCulture = _options.FallbackToDefaultCulture,
        };

        var culture = CultureInfo.GetCultureInfo(result.Culture);
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;

        if (result.IsExplicitSwitch)
        {
            context.Response.Cookies.Append(CultureCookieName, result.Culture,
                new CookieOptions { HttpOnly = false, IsEssential = true });
        }

        await _next(context);
    }
}
```

> `_options.SupportedCultures` 是 `IList<string>`，可直接转 `IReadOnlyCollection<string>`。若编译告警可改为 `_options.SupportedCultures.ToList()`。

`LocalizationApplicationBuilderExtensions.cs`:

```csharp
using Microsoft.AspNetCore.Builder;

namespace Tw.AspNetCore.Localization;

/// <summary>为 <see cref="IApplicationBuilder"/> 提供请求本地化中间件接入。</summary>
public static class LocalizationApplicationBuilderExtensions
{
    /// <summary>启用请求语言解析中间件。</summary>
    /// <param name="app">应用构建器。</param>
    /// <exception cref="ArgumentNullException">当 <paramref name="app"/> 为 <see langword="null"/> 时抛出。</exception>
    public static IApplicationBuilder UseLocalization(this IApplicationBuilder app)
    {
        Check.NotNull(app);
        return app.UseMiddleware<RequestLocalizationMiddleware>();
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.AspNetCore.Tests/Tw.AspNetCore.Tests.csproj --filter RequestLocalizationMiddlewareTests`
Expected: PASS。

> 测试间会改全局 `CultureInfo.CurrentUICulture`。若出现跨测试干扰，给该测试类加 `[Collection]` 串行或在每个测试末尾不依赖残留状态（断言只针对本次设置值，已满足）。

- [ ] **Step 5: Commit**

```bash
git add backend/dotnet/BuildingBlocks/src/Tw.AspNetCore/Localization/RequestLocalizationMiddleware.cs backend/dotnet/BuildingBlocks/src/Tw.AspNetCore/Localization/LocalizationApplicationBuilderExtensions.cs backend/dotnet/BuildingBlocks/tests/Tw.AspNetCore.Tests/Localization/RequestLocalizationMiddlewareTests.cs
git commit -m "feat(aspnetcore): add request localization middleware"
```

---

## Task 4: `IStringLocalizer` 适配（同步读静态快照）

**Files:**
- Create: `backend/dotnet/BuildingBlocks/src/Tw.AspNetCore/Localization/TwStringLocalizer.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.AspNetCore/Localization/TwStringLocalizerOfT.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.AspNetCore/Localization/TwStringLocalizerFactory.cs`
- Test: `backend/dotnet/BuildingBlocks/tests/Tw.AspNetCore.Tests/Localization/TwStringLocalizerTests.cs`

适配器同步读取 `IStaticTextSnapshot`，按当前上下文（访问器，缺省用默认 culture）展开候选 culture。命中返回 `LocalizedString(name, value, resourceNotFound:false)`；缺失返回 `LocalizedString(name, name, resourceNotFound:true)`。`TwStringLocalizer<TResource>` 资源名取 `typeof(TResource).Name`。

- [ ] **Step 1: Write the failing test**

```csharp
using FluentAssertions;
using Microsoft.Extensions.Localization;
using Tw.AspNetCore.Localization;
using Tw.Localization;
using Tw.Localization.Json;
using Xunit;

namespace Tw.AspNetCore.Tests.Localization;

public sealed class Menu { } // 资源名映射目标

public class TwStringLocalizerTests
{
    private static (TwStringLocalizerFactory Factory, CurrentLocalizationContextAccessor Accessor) Build()
    {
        var snapshot = new StaticTextSnapshot(new[]
        {
            ("Menu", new JsonTextResource("en-US", new Dictionary<string, string> { ["Dashboard"] = "Dashboard" })),
            ("Menu", new JsonTextResource("zh-Hans", new Dictionary<string, string> { ["Dashboard"] = "控制台" })),
        });
        var options = new LocalizationOptions { DefaultCulture = "en-US", SupportedCultures = { "en-US", "zh-Hans" } };
        var accessor = new CurrentLocalizationContextAccessor();
        return (new TwStringLocalizerFactory(snapshot, accessor, options), accessor);
    }

    [Fact]
    public void Indexer_ReturnsCurrentCultureValue()
    {
        var (factory, accessor) = Build();
        accessor.Current = new LocalizationContext("zh-Hans");
        var localizer = factory.Create(typeof(Menu));

        var value = localizer["Dashboard"];

        value.Value.Should().Be("控制台");
        value.ResourceNotFound.Should().BeFalse();
    }

    [Fact]
    public void Indexer_ReturnsKey_WhenMissing()
    {
        var (factory, accessor) = Build();
        accessor.Current = new LocalizationContext("zh-Hans");
        var localizer = factory.Create(typeof(Menu));

        var value = localizer["Nope"];

        value.Value.Should().Be("Nope");
        value.ResourceNotFound.Should().BeTrue();
    }

    [Fact]
    public void Indexer_UsesDefaultCulture_WhenNoCurrent()
    {
        var (factory, _) = Build();
        var localizer = factory.Create(typeof(Menu));

        localizer["Dashboard"].Value.Should().Be("Dashboard");
    }

    [Fact]
    public void GetAllStrings_ReturnsMergedSet()
    {
        var (factory, accessor) = Build();
        accessor.Current = new LocalizationContext("zh-Hans");
        var localizer = factory.Create(typeof(Menu));

        var all = localizer.GetAllStrings(includeParentCultures: true).ToList();

        all.Should().ContainSingle(s => s.Name == "Dashboard" && s.Value == "控制台");
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.AspNetCore.Tests/Tw.AspNetCore.Tests.csproj --filter TwStringLocalizerTests`
Expected: 编译失败。

- [ ] **Step 3: Write minimal implementation**

`TwStringLocalizer.cs`:

```csharp
using Microsoft.Extensions.Localization;
using Tw.Localization;

namespace Tw.AspNetCore.Localization;

/// <summary>基于 <see cref="IStaticTextSnapshot"/> 的同步 <see cref="IStringLocalizer"/> 适配器。</summary>
public sealed class TwStringLocalizer : IStringLocalizer
{
    private readonly IStaticTextSnapshot _snapshot;
    private readonly ICurrentLocalizationContextAccessor _accessor;
    private readonly LocalizationOptions _options;
    private readonly string _resourceName;

    /// <summary>初始化适配器。</summary>
    public TwStringLocalizer(
        IStaticTextSnapshot snapshot,
        ICurrentLocalizationContextAccessor accessor,
        LocalizationOptions options,
        string resourceName)
    {
        _snapshot = Check.NotNull(snapshot);
        _accessor = Check.NotNull(accessor);
        _options = Check.NotNull(options);
        _resourceName = Check.NotNull(resourceName);
    }

    private IReadOnlyList<string> Candidates()
    {
        var context = _accessor.Current ?? new LocalizationContext(_options.DefaultCulture);
        return CultureFallback.ExpandCandidates(context, _options.DefaultCulture);
    }

    /// <inheritdoc />
    public LocalizedString this[string name]
    {
        get
        {
            Check.NotNull(name);
            var value = _snapshot.Find(_resourceName, name, Candidates());
            return value is null
                ? new LocalizedString(name, name, resourceNotFound: true, _resourceName)
                : new LocalizedString(name, value, resourceNotFound: false, _resourceName);
        }
    }

    /// <inheritdoc />
    public LocalizedString this[string name, params object[] arguments]
    {
        get
        {
            var template = this[name];
            var formatted = string.Format(template.Value, arguments);
            return new LocalizedString(name, formatted, template.ResourceNotFound, _resourceName);
        }
    }

    /// <inheritdoc />
    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
    {
        foreach (var (key, value) in _snapshot.GetAll(_resourceName, Candidates()))
            yield return new LocalizedString(key, value, resourceNotFound: false, _resourceName);
    }
}
```

`TwStringLocalizerOfT.cs`:

```csharp
using Microsoft.Extensions.Localization;

namespace Tw.AspNetCore.Localization;

/// <summary>泛型资源的 <see cref="IStringLocalizer{T}"/> 适配器，资源名取 <typeparamref name="TResource"/> 类型名。</summary>
public sealed class TwStringLocalizer<TResource> : IStringLocalizer<TResource>
{
    private readonly IStringLocalizer _inner;

    /// <summary>初始化泛型适配器。</summary>
    public TwStringLocalizer(IStringLocalizerFactory factory)
    {
        Check.NotNull(factory);
        _inner = factory.Create(typeof(TResource));
    }

    /// <inheritdoc />
    public LocalizedString this[string name] => _inner[name];

    /// <inheritdoc />
    public LocalizedString this[string name, params object[] arguments] => _inner[name, arguments];

    /// <inheritdoc />
    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) =>
        _inner.GetAllStrings(includeParentCultures);
}
```

`TwStringLocalizerFactory.cs`:

```csharp
using Microsoft.Extensions.Localization;
using Tw.Localization;

namespace Tw.AspNetCore.Localization;

/// <summary>创建 <see cref="TwStringLocalizer"/> 的工厂，资源名由类型名或 baseName 映射。</summary>
public sealed class TwStringLocalizerFactory : IStringLocalizerFactory
{
    private readonly IStaticTextSnapshot _snapshot;
    private readonly ICurrentLocalizationContextAccessor _accessor;
    private readonly LocalizationOptions _options;

    /// <summary>初始化工厂。</summary>
    public TwStringLocalizerFactory(
        IStaticTextSnapshot snapshot,
        ICurrentLocalizationContextAccessor accessor,
        LocalizationOptions options)
    {
        _snapshot = Check.NotNull(snapshot);
        _accessor = Check.NotNull(accessor);
        _options = Check.NotNull(options);
    }

    /// <inheritdoc />
    public IStringLocalizer Create(Type resourceSource)
    {
        Check.NotNull(resourceSource);
        return new TwStringLocalizer(_snapshot, _accessor, _options, resourceSource.Name);
    }

    /// <inheritdoc />
    public IStringLocalizer Create(string baseName, string location)
    {
        Check.NotNull(baseName);
        return new TwStringLocalizer(_snapshot, _accessor, _options, baseName);
    }
}
```

> `LocalizedString` 的 4 参构造为 `(string name, string value, bool resourceNotFound, string searchedLocation)`；若该重载在当前框架不可用，去掉最后一个参数用 3 参构造 `(name, value, resourceNotFound)`。

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.AspNetCore.Tests/Tw.AspNetCore.Tests.csproj --filter TwStringLocalizerTests`
Expected: PASS。

- [ ] **Step 5: Commit**

```bash
git add backend/dotnet/BuildingBlocks/src/Tw.AspNetCore/Localization/TwStringLocalizer.cs backend/dotnet/BuildingBlocks/src/Tw.AspNetCore/Localization/TwStringLocalizerOfT.cs backend/dotnet/BuildingBlocks/src/Tw.AspNetCore/Localization/TwStringLocalizerFactory.cs backend/dotnet/BuildingBlocks/tests/Tw.AspNetCore.Tests/Localization/TwStringLocalizerTests.cs
git commit -m "feat(aspnetcore): add synchronous IStringLocalizer adapters"
```

---

## Task 5: Web 侧 `AddLocalization` 与聚合入口接入

**Files:**
- Create: `backend/dotnet/BuildingBlocks/src/Tw.AspNetCore/Localization/LocalizationServiceCollectionExtensions.cs`
- Modify: `backend/dotnet/BuildingBlocks/src/Tw.AspNetCore/DependencyInjection/WebIntegrationServiceCollectionExtensions.cs`
- Test: `backend/dotnet/BuildingBlocks/tests/Tw.AspNetCore.Tests/Localization/LocalizationServiceCollectionExtensionsTests.cs`

Web `AddLocalization` 职责：调用核心 `Tw.Localization` 的 `AddLocalization(configure)`，注册 scoped `ICurrentLocalizationContextAccessor`→`CurrentLocalizationContextAccessor`、`IStringLocalizerFactory`→`TwStringLocalizerFactory`（singleton）、开放泛型 `IStringLocalizer<>`→`TwStringLocalizer<>`。`AddWebIntegration` 追加调用本 `AddLocalization`（需要 `LocalizationOptions` 配置回调参数）。

> 命名冲突说明：核心扩展方法 `AddLocalization`（命名空间 `Tw.Localization`）与 Web 扩展方法 `AddLocalization`（命名空间 `Tw.AspNetCore.Localization`）同名，通过命名空间隔离。Web 实现内部用完全限定 `global::Tw.Localization` 静态类调用核心方法，避免递归与歧义。

- [ ] **Step 1: Write the failing test**

```csharp
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Tw.AspNetCore.Localization;
using Tw.Localization;
using Xunit;

namespace Tw.AspNetCore.Tests.Localization;

public class LocalizationServiceCollectionExtensionsTests
{
    private static ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        services.AddLocalization(o =>
        {
            o.DefaultCulture = "en-US";
            o.SupportedCultures.Add("en-US");
        });
        return services.BuildServiceProvider();
    }

    [Fact]
    public void AddLocalization_RegistersStringLocalizerFactory()
    {
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        scope.ServiceProvider.GetService<IStringLocalizerFactory>().Should().BeOfType<TwStringLocalizerFactory>();
    }

    [Fact]
    public void AddLocalization_RegistersGenericStringLocalizer()
    {
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        scope.ServiceProvider.GetService<IStringLocalizer<LocalizationServiceCollectionExtensionsTests>>()
            .Should().NotBeNull();
    }

    [Fact]
    public void AddLocalization_RegistersCurrentContextAccessor_AsScoped()
    {
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        scope.ServiceProvider.GetService<ICurrentLocalizationContextAccessor>()
            .Should().BeOfType<CurrentLocalizationContextAccessor>();
    }

    [Fact]
    public void AddLocalization_RegistersCoreTextLocalizer()
    {
        using var provider = BuildProvider();
        provider.GetService<ITextLocalizer>().Should().NotBeNull();
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.AspNetCore.Tests/Tw.AspNetCore.Tests.csproj --filter "LocalizationServiceCollectionExtensionsTests"`
Expected: 编译失败。

- [ ] **Step 3: Write minimal implementation**

`LocalizationServiceCollectionExtensions.cs`:

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Localization;
using Tw.Localization;

namespace Tw.AspNetCore.Localization;

/// <summary>为 <see cref="IServiceCollection"/> 提供 Web 本地化能力注册扩展。</summary>
public static class LocalizationServiceCollectionExtensions
{
    /// <summary>注册 Web 本地化能力，内部调用核心本地化注册。</summary>
    /// <param name="services">服务容器。</param>
    /// <param name="configure">核心本地化配置回调。</param>
    /// <exception cref="ArgumentNullException">参数为 <see langword="null"/> 时抛出。</exception>
    public static IServiceCollection AddLocalization(
        this IServiceCollection services, Action<LocalizationOptions> configure)
    {
        Check.NotNull(services);
        Check.NotNull(configure);

        // 完全限定调用核心扩展，避免与本方法同名递归。
        global::Tw.Localization.LocalizationServiceCollectionExtensions.AddLocalization(services, configure);

        services.TryAddScoped<ICurrentLocalizationContextAccessor, CurrentLocalizationContextAccessor>();
        // 工厂依赖 scoped 访问器，必须注册为 scoped，避免 singleton 捕获 scoped 的 captive dependency。
        services.TryAddScoped<IStringLocalizerFactory, TwStringLocalizerFactory>();
        services.TryAddScoped(typeof(IStringLocalizer<>), typeof(TwStringLocalizer<>));

        return services;
    }
}
```

- [ ] **Step 4: 扩展 `AddWebIntegration` 接入本地化**

编辑 `backend/dotnet/BuildingBlocks/src/Tw.AspNetCore/DependencyInjection/WebIntegrationServiceCollectionExtensions.cs`，给 `AddWebIntegration` 增加一个带配置回调的重载，并让其调用 Web `AddLocalization`：

```csharp
using Microsoft.Extensions.DependencyInjection;
using Tw.AspNetCore.Context;
using Tw.AspNetCore.Localization;
using Tw.Localization;

namespace Tw.AspNetCore;

/// <summary>为 <see cref="IServiceCollection"/> 提供 <c>Tw.AspNetCore</c> Web 集成聚合注册入口。</summary>
public static class WebIntegrationServiceCollectionExtensions
{
    /// <summary>注册 Web 集成的取消令牌能力（不含本地化）。</summary>
    public static IServiceCollection AddWebIntegration(this IServiceCollection services)
    {
        Check.NotNull(services);
        services.AddHttpContextCancellationTokenProvider();
        return services;
    }

    /// <summary>注册 Web 集成的取消令牌与本地化能力。</summary>
    /// <param name="services">服务容器。</param>
    /// <param name="configureLocalization">本地化配置回调。</param>
    public static IServiceCollection AddWebIntegration(
        this IServiceCollection services, Action<LocalizationOptions> configureLocalization)
    {
        Check.NotNull(services);
        Check.NotNull(configureLocalization);

        services.AddHttpContextCancellationTokenProvider();
        services.AddLocalization(configureLocalization);
        return services;
    }
}
```

> 保留原无参 `AddWebIntegration()`（Plan 1 已建），新增带回调重载；`AddLocalization` 来自 `Tw.AspNetCore.Localization`，已 `using`。

- [ ] **Step 5: Run tests to verify they pass**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.AspNetCore.Tests/Tw.AspNetCore.Tests.csproj --filter "LocalizationServiceCollectionExtensionsTests|WebIntegrationServiceCollectionExtensionsTests"`
Expected: PASS（含 Plan 1 的聚合入口测试仍通过）。

- [ ] **Step 6: Commit**

```bash
git add backend/dotnet/BuildingBlocks/src/Tw.AspNetCore/Localization/LocalizationServiceCollectionExtensions.cs backend/dotnet/BuildingBlocks/src/Tw.AspNetCore/DependencyInjection/WebIntegrationServiceCollectionExtensions.cs backend/dotnet/BuildingBlocks/tests/Tw.AspNetCore.Tests/Localization/LocalizationServiceCollectionExtensionsTests.cs
git commit -m "feat(aspnetcore): wire web localization registration and aggregate entry"
```

---

## Task 6: 运行时导出 DTO 契约

**Files:**
- Create: `backend/dotnet/BuildingBlocks/src/Tw.AspNetCore/Localization/LocalizationTextDto.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.AspNetCore/Localization/LocalizationResourceDto.cs`
- Test: `backend/dotnet/BuildingBlocks/tests/Tw.AspNetCore.Tests/Localization/LocalizationResourceDtoTests.cs`

仅提供 DTO 契约与从 `IReadOnlyList<LocalizedText>` 组装的辅助；不强制注册控制器（业务应用按权限/审计实现端点）。

- [ ] **Step 1: Write the failing test**

```csharp
using FluentAssertions;
using Tw.AspNetCore.Localization;
using Tw.Localization;
using Xunit;

namespace Tw.AspNetCore.Tests.Localization;

public class LocalizationResourceDtoTests
{
    [Fact]
    public void From_BuildsResourceDtoFromTexts()
    {
        var texts = new[]
        {
            new LocalizedText("Dashboard", "控制台", "zh-Hans", "Menu", false, LocalizedTextSource.StaticJson),
            new LocalizedText("Home", "主页", "zh-Hans", "Menu", false, LocalizedTextSource.DynamicOverride),
        };

        var dto = LocalizationResourceDto.From("Menu", "zh-Hans", texts);

        dto.ResourceName.Should().Be("Menu");
        dto.Culture.Should().Be("zh-Hans");
        dto.Texts.Should().HaveCount(2);
        dto.Texts.Should().Contain(t => t.Name == "Dashboard" && t.Value == "控制台");
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.AspNetCore.Tests/Tw.AspNetCore.Tests.csproj --filter LocalizationResourceDtoTests`
Expected: 编译失败。

- [ ] **Step 3: Write minimal implementation**

`LocalizationTextDto.cs`:

```csharp
namespace Tw.AspNetCore.Localization;

/// <summary>导出单条文案 DTO。</summary>
/// <param name="Name">文案 key。</param>
/// <param name="Value">文案值。</param>
public sealed record LocalizationTextDto(string Name, string Value);
```

`LocalizationResourceDto.cs`:

```csharp
using Tw.Localization;

namespace Tw.AspNetCore.Localization;

/// <summary>导出资源 DTO，供前端加载本地化包。</summary>
public sealed record LocalizationResourceDto(
    string ResourceName, string Culture, IReadOnlyList<LocalizationTextDto> Texts)
{
    /// <summary>从编排结果组装导出 DTO。</summary>
    public static LocalizationResourceDto From(
        string resourceName, string culture, IReadOnlyList<LocalizedText> texts)
    {
        Check.NotNull(resourceName);
        Check.NotNull(culture);
        Check.NotNull(texts);
        var items = texts.Select(t => new LocalizationTextDto(t.Name, t.Value)).ToList();
        return new LocalizationResourceDto(resourceName, culture, items);
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.AspNetCore.Tests/Tw.AspNetCore.Tests.csproj --filter LocalizationResourceDtoTests`
Expected: PASS。

- [ ] **Step 5: Commit**

```bash
git add backend/dotnet/BuildingBlocks/src/Tw.AspNetCore/Localization/LocalizationTextDto.cs backend/dotnet/BuildingBlocks/src/Tw.AspNetCore/Localization/LocalizationResourceDto.cs backend/dotnet/BuildingBlocks/tests/Tw.AspNetCore.Tests/Localization/LocalizationResourceDtoTests.cs
git commit -m "feat(aspnetcore): add localization export DTO contracts"
```

---

## Task 7: charter 与共享包文档

**Files:**
- Modify: `backend/dotnet/BuildingBlocks/src/Tw.AspNetCore/package-charter.yaml`
- Create: `docs/shared-packages/dotnet/Tw.AspNetCore/localization/request-localization.md`
- Modify: `docs/shared-packages/dotnet/Tw.AspNetCore/README.md`
- Modify: `docs/shared-packages/dotnet/README.md`

- [ ] **Step 1: 更新 charter**

编辑 `backend/dotnet/BuildingBlocks/src/Tw.AspNetCore/package-charter.yaml`：`public_capabilities` 新增 `Tw.AspNetCore.Localization`；`in_scope` 增加一行 `- 请求语言解析与 IStringLocalizer 适配`。核对与 `Tw.Core` 的 `Tw.Localization` 命名空间不重叠（Web 用 `Tw.AspNetCore.Localization`，互斥成立）。

- [ ] **Step 2: 写能力使用文档（How-to Guide）**

`docs/shared-packages/dotnet/Tw.AspNetCore/localization/request-localization.md` 覆盖：能力定位；`AddLocalization(configure)` 与 `AddWebIntegration(configure)` 两种注册方式；`UseLocalization()` 中间件接入位置；语言来源顺序（route→query→cookie→`Accept-Language`→默认）与 cookie 写入策略；`ICurrentLocalizationContextAccessor` 如何在业务代码取当前上下文（不依赖 HttpContext）；`IStringLocalizer`/`IStringLocalizer<T>` 用法与「同步只覆盖静态 JSON、动态覆盖走 `ITextLocalizer`」的边界；运行时导出 DTO 与推荐端点（`GET /api/localization/resources/{resourceName}?culture=...&onlyDynamic=...`，由业务应用实现）。

- [ ] **Step 3: 更新索引（Reference）**

`docs/shared-packages/dotnet/Tw.AspNetCore/README.md` 增链接；`docs/shared-packages/dotnet/README.md` 增 Tw.AspNetCore localization 条目。

- [ ] **Step 4: Commit**

```bash
git add backend/dotnet/BuildingBlocks/src/Tw.AspNetCore/package-charter.yaml docs/shared-packages/dotnet/Tw.AspNetCore/localization docs/shared-packages/dotnet/Tw.AspNetCore/README.md docs/shared-packages/dotnet/README.md
git commit -m "docs(shared-packages): document Tw.AspNetCore request localization"
```

---

## Task 8: 全量验证

- [ ] **Step 1: 构建**

Run: `dotnet build backend/dotnet/Tw.SmartPlatform.slnx`
Expected: 成功。

- [ ] **Step 2: 全量测试**

Run: `dotnet test backend/dotnet/Tw.SmartPlatform.slnx`
Expected: 全部通过。

- [ ] **Step 3: 依赖边界核查**

确认 `Tw.AspNetCore` 未引入 `Microsoft.EntityFrameworkCore*`（charter forbid）。`IStringLocalizer` 等来自 `Microsoft.AspNetCore.App` 框架引用，无需新增包。
Run: `dotnet build backend/dotnet/BuildingBlocks/src/Tw.AspNetCore/Tw.AspNetCore.csproj`
Expected: 成功。

---

## 完成标准

- 请求语言中间件、`UseLocalization`、`ICurrentLocalizationContextAccessor`、`RequestCultureResolver`、`IStringLocalizer` 适配三件套、Web `AddLocalization`、`AddWebIntegration(configure)`、导出 DTO 全部实现并测试覆盖。
- 语言来源顺序、cookie 写入策略、`IStringLocalizer` 资源名映射、缺失返回 key 均有测试。
- `IStringLocalizer` 适配同步读静态快照，不阻塞调用异步 `ITextLocalizer`，符合规范。
- charter 与共享包文档同步更新，命名空间互斥成立。
- `dotnet build` 与 `dotnet test` 全量通过。

## Self-Review 备注

- 租户解析：中间件首轮把 `TenantId` 设为 `null`（单租户）。多租户应用可在中间件之后用自有逻辑改写 `ICurrentLocalizationContextAccessor.Current` 的 `TenantId`，或包装中间件。多租户解析实现属业务应用，不在本 Plan。
- MVC / DataAnnotations / 视图本地化深度接入（设计稿提及）：本 Plan 通过标准 `IStringLocalizerFactory`/`IStringLocalizer<>` 注册即可被 ASP.NET Core DataAnnotations、视图本地化复用。若需 `AddDataAnnotationsLocalization` 显式接线，业务应用在 `AddControllersWithViews()` 链上自行调用；首轮不强绑。
- 运行时导出端点：只提供 DTO 契约与组装辅助，不注册控制器，符合设计稿「不强制注册控制器」。
- 非法 culture 的 Web 边界处理：本 Plan 中间件对不支持/非法 culture 采取「跳过该来源、回退默认」，不抛 4xx，保证请求始终有可用 culture。设计稿「非法 culture 在 Web 边界返回稳定 4xx」适用于业务应用显式校验 route/query culture 的端点（业务应用决定响应结构），属业务边界，不在共享中间件强制；如需共享强校验模式，作为后续增强。
