# Tw.Localization.AspNetCore Web 适配包 Implementation Plan（多语言系列 Plan 3/3）

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 新增独立可选共享包 `Tw.Localization.AspNetCore`，为 `Tw.Localization` 提供 ASP.NET Core 请求语言解析、当前本地化上下文、`IStringLocalizer` 适配和运行时导出 DTO 契约。

**Architecture:** `Tw.Localization.AspNetCore` 依赖 `Tw.Localization` 与 `Tw.AspNetCore`，Web 入口 `AddLocalization(...)` 调用核心包注册并接入 `Tw.AspNetCore.AddWebIntegration()`。请求中间件按 route、query、cookie、`Accept-Language`、默认 culture 顺序解析语言，并把结果写入 scoped 当前上下文。`IStringLocalizer` 是同步接口，只读取 `IStaticTextSnapshot` 的静态 JSON 快照；动态覆盖继续通过异步 `ITextLocalizer` 查询。

**Tech Stack:** .NET 10、ASP.NET Core `Microsoft.AspNetCore.App`、`Microsoft.Extensions.Localization`、xUnit、FluentAssertions、`DefaultHttpContext`。

**前置依赖：** Plan 1（DI 命名整改）和 Plan 2（`Tw.Localization` 核心包）已完成。

**适用规范（实现前必读）：**
- `docs/superpowers/specs/2026-06-04-localization-abstractions-design.md`
- `docs/engineering-standards/03-project-and-code/language-specific/dotnet-core.md`
- `docs/engineering-standards/03-project-and-code/shared-package-charter.md`
- `docs/engineering-standards/04-quality/testing-standards.md`
- `docs/engineering-standards/04-quality/dependency-and-build.md`

**通用命令：**
- 构建解决方案：`dotnet build backend/dotnet/Tw.SmartPlatform.slnx`
- 测试 Web 适配包：`dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Localization.AspNetCore.Tests/Tw.Localization.AspNetCore.Tests.csproj`
- 过滤单类：`dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Localization.AspNetCore.Tests/Tw.Localization.AspNetCore.Tests.csproj --filter <ClassName>`

---

## File Structure

新增源码项目：
- `backend/dotnet/BuildingBlocks/src/Tw.Localization.AspNetCore/Tw.Localization.AspNetCore.csproj`
- `backend/dotnet/BuildingBlocks/src/Tw.Localization.AspNetCore/package-charter.yaml`
- `backend/dotnet/BuildingBlocks/src/Tw.Localization.AspNetCore/ICurrentLocalizationContextAccessor.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.Localization.AspNetCore/CurrentLocalizationContextAccessor.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.Localization.AspNetCore/RequestCultureResolveResult.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.Localization.AspNetCore/RequestCultureResolver.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.Localization.AspNetCore/RequestLocalizationMiddleware.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.Localization.AspNetCore/LocalizationApplicationBuilderExtensions.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.Localization.AspNetCore/TwStringLocalizer.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.Localization.AspNetCore/TwStringLocalizerOfT.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.Localization.AspNetCore/TwStringLocalizerFactory.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.Localization.AspNetCore/LocalizationServiceCollectionExtensions.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.Localization.AspNetCore/LocalizationTextDto.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.Localization.AspNetCore/LocalizationResourceDto.cs`

新增测试项目：
- `backend/dotnet/BuildingBlocks/tests/Tw.Localization.AspNetCore.Tests/Tw.Localization.AspNetCore.Tests.csproj`
- `backend/dotnet/BuildingBlocks/tests/Tw.Localization.AspNetCore.Tests/CurrentLocalizationContextAccessorTests.cs`
- `backend/dotnet/BuildingBlocks/tests/Tw.Localization.AspNetCore.Tests/RequestCultureResolverTests.cs`
- `backend/dotnet/BuildingBlocks/tests/Tw.Localization.AspNetCore.Tests/RequestLocalizationMiddlewareTests.cs`
- `backend/dotnet/BuildingBlocks/tests/Tw.Localization.AspNetCore.Tests/TwStringLocalizerTests.cs`
- `backend/dotnet/BuildingBlocks/tests/Tw.Localization.AspNetCore.Tests/LocalizationServiceCollectionExtensionsTests.cs`
- `backend/dotnet/BuildingBlocks/tests/Tw.Localization.AspNetCore.Tests/LocalizationResourceDtoTests.cs`

修改：
- `backend/dotnet/Tw.SmartPlatform.slnx`
- `docs/shared-packages/dotnet/README.md`
- `docs/shared-packages/dotnet/Tw.Localization.AspNetCore/README.md`
- `docs/shared-packages/dotnet/Tw.Localization.AspNetCore/request-localization.md`
- `docs/shared-packages/dotnet/Tw.AspNetCore/README.md`

---

## Task 1: 项目脚手架、charter 与解决方案注册

**Files:**
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Localization.AspNetCore/Tw.Localization.AspNetCore.csproj`
- Create: `backend/dotnet/BuildingBlocks/tests/Tw.Localization.AspNetCore.Tests/Tw.Localization.AspNetCore.Tests.csproj`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Localization.AspNetCore/package-charter.yaml`
- Modify: `backend/dotnet/Tw.SmartPlatform.slnx`

- [ ] **Step 1: Create source project**

创建 `Tw.Localization.AspNetCore.csproj`：

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <IsPackable>true</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <FrameworkReference Include="Microsoft.AspNetCore.App" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\Tw.AspNetCore\Tw.AspNetCore.csproj" />
    <ProjectReference Include="..\Tw.Localization\Tw.Localization.csproj" />
  </ItemGroup>

</Project>
```

- [ ] **Step 2: Create test project**

创建 `Tw.Localization.AspNetCore.Tests.csproj`：

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <FrameworkReference Include="Microsoft.AspNetCore.App" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="xunit" />
    <PackageReference Include="xunit.runner.visualstudio" />
    <PackageReference Include="FluentAssertions" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\Tw.Localization.AspNetCore\Tw.Localization.AspNetCore.csproj" />
    <ProjectReference Include="..\..\src\Tw.Localization\Tw.Localization.csproj" />
  </ItemGroup>

</Project>
```

- [ ] **Step 3: Create package charter**

创建 `package-charter.yaml`：

```yaml
schema_version: "1.0.0"
package: Tw.Localization.AspNetCore
owner: platform-team
stability: experimental
compatibility: "experimental 阶段不承诺兼容"
responsibility: >
  独立可选的 ASP.NET Core 多语言适配构建块：请求语言解析、当前本地化上下文、
  IStringLocalizer 适配、Web 侧服务注册和运行时本地化资源导出 DTO 契约。
in_scope:
  - ASP.NET Core 请求语言解析中间件
  - Web 请求本地化上下文访问器
  - IStringLocalizer 与 IStringLocalizer<T> 适配
  - Web 多语言依赖注入入口
  - 运行时本地化资源导出 DTO 契约
out_of_scope:
  - 多语言核心模型和回退编排
  - EF Core 表模型、DbContext、迁移或默认数据库实现
  - 管理端页面和管理 API
  - 具体业务领域模型
public_capabilities:
  - Tw.Localization.AspNetCore
dependency_rules:
  forbid:
    - "Microsoft.EntityFrameworkCore*"
  allow: []
```

- [ ] **Step 4: Add projects to solution**

Run:

```powershell
dotnet sln backend/dotnet/Tw.SmartPlatform.slnx add backend/dotnet/BuildingBlocks/src/Tw.Localization.AspNetCore/Tw.Localization.AspNetCore.csproj
dotnet sln backend/dotnet/Tw.SmartPlatform.slnx add backend/dotnet/BuildingBlocks/tests/Tw.Localization.AspNetCore.Tests/Tw.Localization.AspNetCore.Tests.csproj
```

Expected: 两个项目被加入解决方案。

- [ ] **Step 5: Verify scaffold**

Run: `dotnet build backend/dotnet/BuildingBlocks/src/Tw.Localization.AspNetCore/Tw.Localization.AspNetCore.csproj`

Expected: build succeeds。

- [ ] **Step 6: Commit**

```powershell
git add backend/dotnet/BuildingBlocks/src/Tw.Localization.AspNetCore backend/dotnet/BuildingBlocks/tests/Tw.Localization.AspNetCore.Tests backend/dotnet/Tw.SmartPlatform.slnx
git commit -m "feat(localization): add Tw.Localization.AspNetCore project scaffold"
```

---

## Task 2: 当前本地化上下文与请求语言解析器

**Files:**
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Localization.AspNetCore/ICurrentLocalizationContextAccessor.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Localization.AspNetCore/CurrentLocalizationContextAccessor.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Localization.AspNetCore/RequestCultureResolveResult.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Localization.AspNetCore/RequestCultureResolver.cs`
- Test: `backend/dotnet/BuildingBlocks/tests/Tw.Localization.AspNetCore.Tests/CurrentLocalizationContextAccessorTests.cs`
- Test: `backend/dotnet/BuildingBlocks/tests/Tw.Localization.AspNetCore.Tests/RequestCultureResolverTests.cs`

- [ ] **Step 1: Write failing accessor tests**

```csharp
using FluentAssertions;
using Tw.Localization;
using Xunit;

namespace Tw.Localization.AspNetCore.Tests;

public class CurrentLocalizationContextAccessorTests
{
    [Fact]
    public void Current_DefaultsToNull()
    {
        new CurrentLocalizationContextAccessor().Current.Should().BeNull();
    }

    [Fact]
    public void Current_RoundTripsAssignedContext()
    {
        var accessor = new CurrentLocalizationContextAccessor
        {
            Current = new LocalizationContext("zh-Hans") { TenantId = "tenant-a" },
        };

        accessor.Current!.CultureName.Should().Be("zh-Hans");
        accessor.Current!.TenantId.Should().Be("tenant-a");
    }
}
```

- [ ] **Step 2: Write failing resolver tests**

```csharp
using FluentAssertions;
using Tw.Localization;
using Xunit;

namespace Tw.Localization.AspNetCore.Tests;

public class RequestCultureResolverTests
{
    private static LocalizationOptions Options()
    {
        return new LocalizationOptions
        {
            DefaultCulture = "en-US",
            SupportedCultures = { "en-US", "zh-Hans" },
        };
    }

    [Fact]
    public void Resolve_UsesRouteBeforeQuery()
    {
        var result = RequestCultureResolver.Resolve(
            routeCulture: "zh-Hans",
            queryCulture: "en-US",
            cookieCulture: null,
            acceptLanguageHeader: null,
            Options());

        result.CultureName.Should().Be("zh-Hans");
        result.IsExplicitSwitch.Should().BeTrue();
    }

    [Fact]
    public void Resolve_UsesDefaultForUnsupportedCulture()
    {
        var result = RequestCultureResolver.Resolve(
            routeCulture: null,
            queryCulture: "fr-FR",
            cookieCulture: null,
            acceptLanguageHeader: null,
            Options());

        result.CultureName.Should().Be("en-US");
    }
}
```

- [ ] **Step 3: Implement accessor and resolver**

Contracts:

```csharp
using Tw.Localization;

namespace Tw.Localization.AspNetCore;

public interface ICurrentLocalizationContextAccessor
{
    LocalizationContext? Current { get; set; }
}

public sealed class CurrentLocalizationContextAccessor : ICurrentLocalizationContextAccessor
{
    public LocalizationContext? Current { get; set; }
}

public sealed record RequestCultureResolveResult(string CultureName, bool IsExplicitSwitch);

public static class RequestCultureResolver
{
    public static RequestCultureResolveResult Resolve(
        string? routeCulture,
        string? queryCulture,
        string? cookieCulture,
        string? acceptLanguageHeader,
        LocalizationOptions options)
    {
        Check.NotNull(options);

        if (TrySelect(routeCulture, options, out var routeResult))
        {
            return new RequestCultureResolveResult(routeResult, true);
        }

        if (TrySelect(queryCulture, options, out var queryResult))
        {
            return new RequestCultureResolveResult(queryResult, true);
        }

        if (TrySelect(cookieCulture, options, out var cookieResult))
        {
            return new RequestCultureResolveResult(cookieResult, false);
        }

        foreach (var language in ParseAcceptLanguage(acceptLanguageHeader))
        {
            if (TrySelect(language, options, out var headerResult))
            {
                return new RequestCultureResolveResult(headerResult, false);
            }
        }

        return new RequestCultureResolveResult(options.DefaultCulture, false);
    }

    private static bool TrySelect(string? value, LocalizationOptions options, out string cultureName)
    {
        cultureName = string.Empty;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var supported = options.SupportedCultures.FirstOrDefault(
            x => string.Equals(x, value, StringComparison.OrdinalIgnoreCase));
        if (supported is null)
        {
            return false;
        }

        cultureName = supported;
        return true;
    }

    private static IEnumerable<string> ParseAcceptLanguage(string? header)
    {
        if (string.IsNullOrWhiteSpace(header))
        {
            yield break;
        }

        foreach (var item in header.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var culture = item.Split(';', 2, StringSplitOptions.TrimEntries)[0];
            if (!string.IsNullOrWhiteSpace(culture))
            {
                yield return culture;
            }
        }
    }
}
```

Resolver rules:
- route wins over query.
- query wins over cookie.
- cookie wins over `Accept-Language`.
- `Accept-Language` selects the first supported culture.
- unsupported values are ignored.
- default culture is the final fallback.
- `IsExplicitSwitch` is true only when route or query supplies the selected supported culture.

- [ ] **Step 4: Run resolver tests**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Localization.AspNetCore.Tests/Tw.Localization.AspNetCore.Tests.csproj --filter "CurrentLocalizationContextAccessorTests|RequestCultureResolverTests"`

Expected: tests pass。

- [ ] **Step 5: Commit**

```powershell
git add backend/dotnet/BuildingBlocks/src/Tw.Localization.AspNetCore backend/dotnet/BuildingBlocks/tests/Tw.Localization.AspNetCore.Tests/CurrentLocalizationContextAccessorTests.cs backend/dotnet/BuildingBlocks/tests/Tw.Localization.AspNetCore.Tests/RequestCultureResolverTests.cs
git commit -m "feat(localization): add request culture resolver"
```

---

## Task 3: 请求语言中间件与 `UseLocalization`

**Files:**
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Localization.AspNetCore/RequestLocalizationMiddleware.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Localization.AspNetCore/LocalizationApplicationBuilderExtensions.cs`
- Test: `backend/dotnet/BuildingBlocks/tests/Tw.Localization.AspNetCore.Tests/RequestLocalizationMiddlewareTests.cs`

- [ ] **Step 1: Write failing middleware tests**

```csharp
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Tw.Localization;
using Xunit;

namespace Tw.Localization.AspNetCore.Tests;

public class RequestLocalizationMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_WritesCurrentContext()
    {
        var options = new LocalizationOptions { DefaultCulture = "en-US", SupportedCultures = { "en-US", "zh-Hans" } };
        var accessor = new CurrentLocalizationContextAccessor();
        var middleware = new RequestLocalizationMiddleware(_ => Task.CompletedTask, options);
        var context = new DefaultHttpContext();
        context.Request.QueryString = new QueryString("?culture=zh-Hans");

        await middleware.InvokeAsync(context, accessor);

        accessor.Current!.CultureName.Should().Be("zh-Hans");
    }

    [Fact]
    public async Task InvokeAsync_WritesCookieForExplicitSwitch()
    {
        var options = new LocalizationOptions { DefaultCulture = "en-US", SupportedCultures = { "en-US", "zh-Hans" } };
        var accessor = new CurrentLocalizationContextAccessor();
        var middleware = new RequestLocalizationMiddleware(_ => Task.CompletedTask, options);
        var context = new DefaultHttpContext();
        context.Request.QueryString = new QueryString("?culture=zh-Hans");

        await middleware.InvokeAsync(context, accessor);

        context.Response.Headers.SetCookie.ToString().Should().Contain(".Tw.Culture=zh-Hans");
    }
}
```

- [ ] **Step 2: Implement middleware and extension**

Contracts:

```csharp
namespace Tw.Localization.AspNetCore;

public sealed class RequestLocalizationMiddleware
{
    public const string CultureCookieName = ".Tw.Culture";

    private readonly RequestDelegate _next;
    private readonly LocalizationOptions _options;

    public RequestLocalizationMiddleware(RequestDelegate next, LocalizationOptions options)
    {
        _next = Check.NotNull(next);
        _options = Check.NotNull(options);
    }

    public async Task InvokeAsync(HttpContext context, ICurrentLocalizationContextAccessor accessor)
    {
        Check.NotNull(context);
        Check.NotNull(accessor);

        var routeCulture = context.Request.RouteValues.TryGetValue("culture", out var routeValue)
            ? Convert.ToString(routeValue, CultureInfo.InvariantCulture)
            : null;
        var queryCulture = context.Request.Query["culture"].FirstOrDefault();
        var cookieCulture = context.Request.Cookies[CultureCookieName];
        var headerCulture = context.Request.Headers.AcceptLanguage.ToString();

        var result = RequestCultureResolver.Resolve(
            routeCulture,
            queryCulture,
            cookieCulture,
            headerCulture,
            _options);

        accessor.Current = new LocalizationContext(result.CultureName);
        var cultureInfo = CultureInfo.GetCultureInfo(result.CultureName);
        CultureInfo.CurrentCulture = cultureInfo;
        CultureInfo.CurrentUICulture = cultureInfo;

        if (result.IsExplicitSwitch)
        {
            context.Response.Cookies.Append(CultureCookieName, result.CultureName);
        }

        await _next(context);
    }
}

public static class LocalizationApplicationBuilderExtensions
{
    public static IApplicationBuilder UseLocalization(this IApplicationBuilder app)
    {
        return app.UseMiddleware<RequestLocalizationMiddleware>();
    }
}
```

Middleware rules:
- route value key is `culture`.
- query key is `culture`.
- cookie name is `.Tw.Culture`.
- write cookie only when route or query selected a supported culture.
- set `CultureInfo.CurrentCulture` and `CultureInfo.CurrentUICulture`.
- do not expose internal exception details in responses.

- [ ] **Step 3: Run middleware tests**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Localization.AspNetCore.Tests/Tw.Localization.AspNetCore.Tests.csproj --filter RequestLocalizationMiddlewareTests`

Expected: tests pass。

- [ ] **Step 4: Commit**

```powershell
git add backend/dotnet/BuildingBlocks/src/Tw.Localization.AspNetCore backend/dotnet/BuildingBlocks/tests/Tw.Localization.AspNetCore.Tests/RequestLocalizationMiddlewareTests.cs
git commit -m "feat(localization): add request localization middleware"
```

---

## Task 4: `IStringLocalizer` 同步适配

**Files:**
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Localization.AspNetCore/TwStringLocalizer.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Localization.AspNetCore/TwStringLocalizerOfT.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Localization.AspNetCore/TwStringLocalizerFactory.cs`
- Test: `backend/dotnet/BuildingBlocks/tests/Tw.Localization.AspNetCore.Tests/TwStringLocalizerTests.cs`

- [ ] **Step 1: Write failing localizer tests**

```csharp
using FluentAssertions;
using Microsoft.Extensions.Localization;
using Tw.Localization.Json;
using Xunit;

namespace Tw.Localization.AspNetCore.Tests;

public class TwStringLocalizerTests
{
    [Fact]
    public void Indexer_ReturnsStaticSnapshotText()
    {
        var snapshot = new StaticTextSnapshot(
            [new JsonTextResource("App", "zh-Hans", new Dictionary<string, string> { ["Menu"] = "菜单" })]);
        var accessor = new CurrentLocalizationContextAccessor { Current = new LocalizationContext("zh-Hans") };
        var options = new LocalizationOptions { DefaultCulture = "en-US", SupportedCultures = { "en-US", "zh-Hans" } };
        IStringLocalizer localizer = new TwStringLocalizer(snapshot, accessor, options, "App");

        var value = localizer["Menu"];

        value.Value.Should().Be("菜单");
        value.ResourceNotFound.Should().BeFalse();
    }

    [Fact]
    public void Indexer_ReturnsKeyForMissingText()
    {
        var snapshot = new StaticTextSnapshot([]);
        var accessor = new CurrentLocalizationContextAccessor { Current = new LocalizationContext("zh-Hans") };
        var options = new LocalizationOptions { DefaultCulture = "en-US", SupportedCultures = { "en-US", "zh-Hans" } };
        IStringLocalizer localizer = new TwStringLocalizer(snapshot, accessor, options, "App");

        var value = localizer["Missing"];

        value.Value.Should().Be("Missing");
        value.ResourceNotFound.Should().BeTrue();
    }
}
```

- [ ] **Step 2: Implement localizer adapters**

Rules:
- `TwStringLocalizer` implements `IStringLocalizer`.
- `TwStringLocalizer<TResource>` implements `IStringLocalizer<TResource>`.
- `TwStringLocalizerFactory` implements `IStringLocalizerFactory`.
- Resource name defaults to type name for generic resources.
- Synchronous indexer uses `IStaticTextSnapshot`.
- Missing text returns `new LocalizedString(name, name, resourceNotFound: true)`.
- Do not call `.Result`, `.Wait()` or async localizer APIs from the synchronous indexer.

- [ ] **Step 3: Run localizer tests**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Localization.AspNetCore.Tests/Tw.Localization.AspNetCore.Tests.csproj --filter TwStringLocalizerTests`

Expected: tests pass。

- [ ] **Step 4: Commit**

```powershell
git add backend/dotnet/BuildingBlocks/src/Tw.Localization.AspNetCore backend/dotnet/BuildingBlocks/tests/Tw.Localization.AspNetCore.Tests/TwStringLocalizerTests.cs
git commit -m "feat(localization): add string localizer adapters"
```

---

## Task 5: Web `AddLocalization` 注册入口

**Files:**
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Localization.AspNetCore/LocalizationServiceCollectionExtensions.cs`
- Test: `backend/dotnet/BuildingBlocks/tests/Tw.Localization.AspNetCore.Tests/LocalizationServiceCollectionExtensionsTests.cs`

- [ ] **Step 1: Write failing registration tests**

```csharp
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Tw.AspNetCore.Context;
using Xunit;

namespace Tw.Localization.AspNetCore.Tests;

public class LocalizationServiceCollectionExtensionsTests
{
    [Fact]
    public void AddLocalization_RegistersWebAndCoreServices()
    {
        var services = new ServiceCollection();

        services.AddLocalization(o =>
        {
            o.DefaultCulture = "en-US";
            o.SupportedCultures.Add("en-US");
        });

        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<ITextLocalizer>().Should().NotBeNull();
        provider.GetRequiredService<ICurrentLocalizationContextAccessor>().Should().BeOfType<CurrentLocalizationContextAccessor>();
        provider.GetRequiredService<IStringLocalizerFactory>().Should().BeOfType<TwStringLocalizerFactory>();
        provider.GetRequiredService<IStringLocalizer<LocalizationServiceCollectionExtensionsTests>>().Should().NotBeNull();
        provider.GetRequiredService<HttpContextCancellationTokenProvider>().Should().NotBeNull();
    }
}
```

- [ ] **Step 2: Implement registration**

Rules:
- Namespace is `Tw.Localization.AspNetCore`.
- Method name is `AddLocalization`.
- Class name is `LocalizationServiceCollectionExtensions`.
- It calls `services.AddWebIntegration()` from namespace `Tw.AspNetCore`.
- It calls `global::Tw.Localization.LocalizationServiceCollectionExtensions.AddLocalization(services, configure)`.
- It registers `ICurrentLocalizationContextAccessor` as scoped.
- It registers `IStringLocalizerFactory` as singleton.
- It registers open generic `IStringLocalizer<>` to `TwStringLocalizer<>`.
- It does not use `Microsoft.Extensions.DependencyInjection` as the extension class namespace.

- [ ] **Step 3: Run registration tests**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Localization.AspNetCore.Tests/Tw.Localization.AspNetCore.Tests.csproj --filter LocalizationServiceCollectionExtensionsTests`

Expected: tests pass。

- [ ] **Step 4: Commit**

```powershell
git add backend/dotnet/BuildingBlocks/src/Tw.Localization.AspNetCore/LocalizationServiceCollectionExtensions.cs backend/dotnet/BuildingBlocks/tests/Tw.Localization.AspNetCore.Tests/LocalizationServiceCollectionExtensionsTests.cs
git commit -m "feat(localization): add aspnetcore localization registration"
```

---

## Task 6: 运行时导出 DTO、共享包文档与最终验证

**Files:**
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Localization.AspNetCore/LocalizationTextDto.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Localization.AspNetCore/LocalizationResourceDto.cs`
- Test: `backend/dotnet/BuildingBlocks/tests/Tw.Localization.AspNetCore.Tests/LocalizationResourceDtoTests.cs`
- Create: `docs/shared-packages/dotnet/Tw.Localization.AspNetCore/README.md`
- Create: `docs/shared-packages/dotnet/Tw.Localization.AspNetCore/request-localization.md`
- Modify: `docs/shared-packages/dotnet/README.md`
- Modify: `docs/shared-packages/dotnet/Tw.AspNetCore/README.md`

- [ ] **Step 1: Write failing DTO tests**

```csharp
using FluentAssertions;
using Xunit;

namespace Tw.Localization.AspNetCore.Tests;

public class LocalizationResourceDtoTests
{
    [Fact]
    public void ResourceDto_HoldsTexts()
    {
        var dto = new LocalizationResourceDto(
            "App",
            "zh-Hans",
            [new LocalizationTextDto("Menu", "菜单", false)]);

        dto.ResourceName.Should().Be("App");
        dto.Texts.Should().ContainSingle(x => x.Name == "Menu" && x.Value == "菜单");
    }
}
```

- [ ] **Step 2: Implement DTOs**

Contracts:

```csharp
namespace Tw.Localization.AspNetCore;

public sealed record LocalizationTextDto(
    string Name,
    string Value,
    bool ResourceNotFound);

public sealed record LocalizationResourceDto(
    string ResourceName,
    string CultureName,
    IReadOnlyList<LocalizationTextDto> Texts);
```

- [ ] **Step 3: Run DTO tests**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Localization.AspNetCore.Tests/Tw.Localization.AspNetCore.Tests.csproj --filter LocalizationResourceDtoTests`

Expected: tests pass。

- [ ] **Step 4: Write shared package docs**

`docs/shared-packages/dotnet/Tw.Localization.AspNetCore/README.md` covers:
- package role as optional ASP.NET Core localization adapter.
- dependency on `Tw.Localization` and `Tw.AspNetCore`.
- links to `request-localization.md`.

`request-localization.md` covers:
- `services.AddLocalization(...)` registration.
- `app.UseLocalization()` middleware order.
- route, query, cookie, `Accept-Language`, default culture resolution order.
- cookie write rule for route and query explicit switches.
- `ICurrentLocalizationContextAccessor` usage.
- `IStringLocalizer` static snapshot boundary.
- runtime export DTOs and recommended endpoint shape.

`docs/shared-packages/dotnet/Tw.AspNetCore/README.md` gains a note that Web localization lives in `Tw.Localization.AspNetCore` and is not built into `Tw.AspNetCore`.

- [ ] **Step 5: Run final verification**

Run:

```powershell
dotnet build backend/dotnet/Tw.SmartPlatform.slnx
dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Localization.AspNetCore.Tests/Tw.Localization.AspNetCore.Tests.csproj
rg -n "Microsoft.EntityFrameworkCore|Tw.AspNetCore.Localization" backend/dotnet/BuildingBlocks/src/Tw.Localization.AspNetCore backend/dotnet/BuildingBlocks/src/Tw.AspNetCore
```

Expected:
- Build succeeds.
- Tests pass.
- `rg` returns no forbidden EF Core reference and no `Tw.AspNetCore.Localization` namespace.

- [ ] **Step 6: Commit**

```powershell
git add backend/dotnet/BuildingBlocks/src/Tw.Localization.AspNetCore backend/dotnet/BuildingBlocks/tests/Tw.Localization.AspNetCore.Tests docs/shared-packages/dotnet/Tw.Localization.AspNetCore docs/shared-packages/dotnet/Tw.AspNetCore/README.md docs/shared-packages/dotnet/README.md
git commit -m "docs(shared-packages): document Tw.Localization.AspNetCore"
```

---

## 完成标准

- `Tw.Localization.AspNetCore` 项目和测试项目已加入解决方案。
- `Tw.Localization.AspNetCore/package-charter.yaml` 存在且 `public_capabilities` 仅登记 `Tw.Localization.AspNetCore`。
- 请求语言解析、中间件、当前上下文访问器、`IStringLocalizer` 适配、Web `AddLocalization`、运行时导出 DTO 全部有测试覆盖。
- `Tw.AspNetCore` 未新增 `Tw.AspNetCore.Localization` 命名空间，未承载多语言实现。
- `Tw.Localization.AspNetCore` 不引用 EF Core 或具体 ORM。
- 共享包文档和索引可从 `docs/shared-packages/dotnet/README.md` 跳转。
