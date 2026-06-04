# Tw.Localization 核心包 Implementation Plan（多语言系列 Plan 2/3）

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 新增独立可选共享包 `Tw.Localization`，承载框架无关、ORM 无关的系统文案本地化与业务实体翻译核心能力。

**Architecture:** `Tw.Localization` 依赖 `Tw.Core`，复用 `Check`、`TwConfigurationException` 与 `ICancellationTokenProvider`。核心编排层把 `LocalizationContext` 与 `LocalizationOptions` 展开为候选 culture 集合，一次性传给静态 JSON 贡献源、动态文案 store 或实体翻译 store，避免逐级往返查询。包内不引用 ASP.NET Core、EF Core 或具体 ORM。

**Tech Stack:** .NET 10、C# file-scoped namespace、nullable enable、record 值对象、`System.Text.Json`、`Microsoft.Extensions.DependencyInjection.Abstractions`、xUnit、FluentAssertions。

**前置依赖：** Plan 1（DI 命名整改）已完成，`Tw.Context.CancellationTokenServiceCollectionExtensions.AddCancellationTokenProvider(...)` 可用。

**适用规范（实现前必读）：**
- `docs/superpowers/specs/2026-06-04-localization-abstractions-design.md`
- `docs/engineering-standards/03-project-and-code/language-specific/dotnet-core.md`
- `docs/engineering-standards/03-project-and-code/shared-package-charter.md`
- `docs/engineering-standards/04-quality/testing-standards.md`
- `docs/engineering-standards/04-quality/dependency-and-build.md`

**通用命令：**
- 构建解决方案：`dotnet build backend/dotnet/Tw.SmartPlatform.slnx`
- 测试核心包：`dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Localization.Tests/Tw.Localization.Tests.csproj`
- 过滤单类：`dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Localization.Tests/Tw.Localization.Tests.csproj --filter <ClassName>`

---

## File Structure

新增源码项目：
- `backend/dotnet/BuildingBlocks/src/Tw.Localization/Tw.Localization.csproj`
- `backend/dotnet/BuildingBlocks/src/Tw.Localization/package-charter.yaml`
- `backend/dotnet/BuildingBlocks/src/Tw.Localization/LanguageInfo.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.Localization/LocalizationContext.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.Localization/LocalizationOptions.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.Localization/MissingTextBehavior.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.Localization/LocalizedText.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.Localization/LocalizedTextSource.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.Localization/EntityTranslation.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.Localization/Requests/TextLookupRequest.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.Localization/Requests/TextFillRequest.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.Localization/Requests/EntityTranslationKey.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.Localization/Requests/EntityTranslationLookup.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.Localization/Requests/EntityTranslationQuery.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.Localization/Requests/EntityTranslationBatchQuery.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.Localization/CultureFallback.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.Localization/ITextLocalizer.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.Localization/ITextResourceContributor.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.Localization/IDynamicTextStore.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.Localization/IEntityTranslationStore.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.Localization/IEntityTranslationService.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.Localization/IStaticTextSnapshot.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.Localization/StaticTextSnapshot.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.Localization/DynamicTextContributor.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.Localization/TextLocalizer.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.Localization/EntityTranslationService.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.Localization/LocalizationServiceCollectionExtensions.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.Localization/Caching/ILocalizationChangeToken.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.Localization/Caching/ILocalizationCacheInvalidator.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.Localization/Json/JsonTextResource.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.Localization/Json/JsonTextResourceParser.cs`
- `backend/dotnet/BuildingBlocks/src/Tw.Localization/Json/JsonTextResourceContributor.cs`

新增测试项目：
- `backend/dotnet/BuildingBlocks/tests/Tw.Localization.Tests/Tw.Localization.Tests.csproj`
- `backend/dotnet/BuildingBlocks/tests/Tw.Localization.Tests/LocalizationModelsTests.cs`
- `backend/dotnet/BuildingBlocks/tests/Tw.Localization.Tests/LocalizationOptionsTests.cs`
- `backend/dotnet/BuildingBlocks/tests/Tw.Localization.Tests/CultureFallbackTests.cs`
- `backend/dotnet/BuildingBlocks/tests/Tw.Localization.Tests/JsonTextResourceParserTests.cs`
- `backend/dotnet/BuildingBlocks/tests/Tw.Localization.Tests/JsonTextResourceContributorTests.cs`
- `backend/dotnet/BuildingBlocks/tests/Tw.Localization.Tests/TextLocalizerTests.cs`
- `backend/dotnet/BuildingBlocks/tests/Tw.Localization.Tests/EntityTranslationServiceTests.cs`
- `backend/dotnet/BuildingBlocks/tests/Tw.Localization.Tests/LocalizationServiceCollectionExtensionsTests.cs`
- `backend/dotnet/BuildingBlocks/tests/Tw.Localization.Tests/Fakes/InMemoryDynamicTextStore.cs`
- `backend/dotnet/BuildingBlocks/tests/Tw.Localization.Tests/Fakes/InMemoryEntityTranslationStore.cs`

修改：
- `backend/dotnet/Tw.SmartPlatform.slnx`
- `docs/shared-packages/dotnet/README.md`
- `docs/shared-packages/dotnet/Tw.Localization/README.md`
- `docs/shared-packages/dotnet/Tw.Localization/text-localization.md`
- `docs/shared-packages/dotnet/Tw.Localization/entity-translation.md`

---

## Task 1: 项目脚手架、charter 与解决方案注册

**Files:**
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Localization/Tw.Localization.csproj`
- Create: `backend/dotnet/BuildingBlocks/tests/Tw.Localization.Tests/Tw.Localization.Tests.csproj`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Localization/package-charter.yaml`
- Modify: `backend/dotnet/Tw.SmartPlatform.slnx`

- [ ] **Step 1: Create source project**

创建 `Tw.Localization.csproj`：

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>true</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\Tw.Core\Tw.Core.csproj" />
  </ItemGroup>

</Project>
```

- [ ] **Step 2: Create test project**

创建 `Tw.Localization.Tests.csproj`：

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
    <PackageReference Include="xunit.runner.visualstudio" />
    <PackageReference Include="FluentAssertions" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\Tw.Localization\Tw.Localization.csproj" />
  </ItemGroup>

</Project>
```

- [ ] **Step 3: Create package charter**

创建 `package-charter.yaml`：

```yaml
schema_version: "1.0.0"
package: Tw.Localization
owner: platform-team
stability: experimental
compatibility: "experimental 阶段不承诺兼容"
responsibility: >
  独立可选的多语言核心构建块：语言上下文、系统文案资源、JSON 静态资源、
  动态文案覆盖抽象、业务实体字段翻译抽象、回退链与本地化缓存失效契约。
in_scope:
  - 系统文案本地化核心抽象与默认编排
  - JSON 静态多语言资源解析与贡献源
  - 动态系统文案 store 接口
  - 业务实体翻译 store 接口与服务
  - 多租户与 culture 回退策略
out_of_scope:
  - ASP.NET Core 请求语言解析和 IStringLocalizer 适配
  - EF Core 表模型、DbContext、迁移或默认数据库实现
  - 管理端页面和管理 API
  - 具体业务领域模型
public_capabilities:
  - Tw.Localization
dependency_rules:
  forbid:
    - "Microsoft.AspNetCore.*"
    - "Microsoft.EntityFrameworkCore*"
  allow: []
```

- [ ] **Step 4: Add projects to solution**

Run:

```powershell
dotnet sln backend/dotnet/Tw.SmartPlatform.slnx add backend/dotnet/BuildingBlocks/src/Tw.Localization/Tw.Localization.csproj
dotnet sln backend/dotnet/Tw.SmartPlatform.slnx add backend/dotnet/BuildingBlocks/tests/Tw.Localization.Tests/Tw.Localization.Tests.csproj
```

Expected: 两个项目被加入解决方案。

- [ ] **Step 5: Verify scaffold**

Run: `dotnet build backend/dotnet/BuildingBlocks/src/Tw.Localization/Tw.Localization.csproj`

Expected: build succeeds。

- [ ] **Step 6: Commit**

```powershell
git add backend/dotnet/BuildingBlocks/src/Tw.Localization backend/dotnet/BuildingBlocks/tests/Tw.Localization.Tests backend/dotnet/Tw.SmartPlatform.slnx
git commit -m "feat(localization): add Tw.Localization project scaffold"
```

---

## Task 2: 核心模型与请求 DTO

**Files:**
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Localization/LanguageInfo.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Localization/LocalizationContext.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Localization/LocalizedTextSource.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Localization/LocalizedText.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Localization/EntityTranslation.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Localization/Requests/TextLookupRequest.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Localization/Requests/TextFillRequest.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Localization/Requests/EntityTranslationKey.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Localization/Requests/EntityTranslationLookup.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Localization/Requests/EntityTranslationQuery.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Localization/Requests/EntityTranslationBatchQuery.cs`
- Test: `backend/dotnet/BuildingBlocks/tests/Tw.Localization.Tests/LocalizationModelsTests.cs`

- [ ] **Step 1: Write failing model tests**

```csharp
using FluentAssertions;
using Tw.Localization;
using Tw.Localization.Requests;
using Xunit;

namespace Tw.Localization.Tests;

public class LocalizationModelsTests
{
    [Fact]
    public void LanguageInfo_DefaultsUiCultureToCulture()
    {
        var language = new LanguageInfo("zh-Hans") { DisplayName = "简体中文" };

        language.UiCultureName.Should().Be("zh-Hans");
        language.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public void LocalizedText_NotFound_ReturnsKeyAsValue()
    {
        var text = LocalizedText.NotFound("App", "Menu.Home", "zh-Hans");

        text.Value.Should().Be("Menu.Home");
        text.ResourceNotFound.Should().BeTrue();
        text.Source.Should().Be(LocalizedTextSource.NotFound);
    }

    [Fact]
    public void EntityTranslationKey_UsesValueEquality()
    {
        var left = new EntityTranslationKey("Product", "42", "Name");
        var right = new EntityTranslationKey("Product", "42", "Name");

        left.Should().Be(right);
    }

    [Fact]
    public void BatchQuery_ReusesContext()
    {
        var context = new LocalizationContext("zh-Hans") { TenantId = "tenant-a" };
        var query = new EntityTranslationBatchQuery(
            [new EntityTranslationKey("Product", "42", "Name")],
            context);

        query.Context.TenantId.Should().Be("tenant-a");
        query.Keys.Should().ContainSingle();
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Localization.Tests/Tw.Localization.Tests.csproj --filter LocalizationModelsTests`

Expected: compile fails because models do not exist。

- [ ] **Step 3: Implement model contracts**

Create the following types with Chinese XML comments and `Check.NotNullOrWhiteSpace(...)` validation for required strings.

```csharp
namespace Tw.Localization;

public sealed class LanguageInfo
{
    public LanguageInfo(string cultureName)
    {
        CultureName = Check.NotNullOrWhiteSpace(cultureName);
        UiCultureName = CultureName;
    }

    public string CultureName { get; }
    public string UiCultureName { get; init; }
    public string? DisplayName { get; init; }
    public bool IsEnabled { get; init; } = true;
    public int SortOrder { get; init; }
}

public sealed class LocalizationContext
{
    public LocalizationContext(string cultureName)
    {
        CultureName = Check.NotNullOrWhiteSpace(cultureName);
    }

    public string CultureName { get; }
    public string? TenantId { get; init; }
    public bool FallbackToParentCultures { get; init; } = true;
    public bool FallbackToDefaultCulture { get; init; } = true;
}

public enum LocalizedTextSource
{
    StaticJson = 0,
    Dynamic = 1,
    ParentCulture = 2,
    DefaultCulture = 3,
    BaseResource = 4,
    NotFound = 5,
}

public sealed record LocalizedText(
    string ResourceName,
    string Name,
    string Value,
    string CultureName,
    bool ResourceNotFound,
    LocalizedTextSource Source)
{
    public static LocalizedText NotFound(string resourceName, string name, string cultureName) =>
        new(resourceName, name, name, cultureName, true, LocalizedTextSource.NotFound);
}

public sealed record EntityTranslation(
    string EntityType,
    string EntityId,
    string FieldName,
    string CultureName,
    string Value,
    string? TenantId);
```

```csharp
namespace Tw.Localization.Requests;

public sealed record TextLookupRequest(
    string ResourceName,
    string Name,
    LocalizationContext Context,
    IReadOnlyList<string> CandidateCultureNames);

public sealed record TextFillRequest(
    string ResourceName,
    LocalizationContext Context,
    IReadOnlyList<string> CandidateCultureNames);

public sealed record EntityTranslationKey(string EntityType, string EntityId, string FieldName);

public sealed record EntityTranslationLookup(
    EntityTranslationKey Key,
    LocalizationContext Context);

public sealed record EntityTranslationQuery(
    IReadOnlyList<EntityTranslationKey> Keys,
    LocalizationContext Context,
    IReadOnlyList<string> CandidateCultureNames);

public sealed record EntityTranslationBatchQuery(
    IReadOnlyList<EntityTranslationKey> Keys,
    LocalizationContext Context);
```

- [ ] **Step 4: Run model tests**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Localization.Tests/Tw.Localization.Tests.csproj --filter LocalizationModelsTests`

Expected: tests pass。

- [ ] **Step 5: Commit**

```powershell
git add backend/dotnet/BuildingBlocks/src/Tw.Localization backend/dotnet/BuildingBlocks/tests/Tw.Localization.Tests/LocalizationModelsTests.cs
git commit -m "feat(localization): add core localization models"
```

---

## Task 3: 选项、culture 校验与回退展开

**Files:**
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Localization/LocalizationOptions.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Localization/MissingTextBehavior.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Localization/CultureFallback.cs`
- Test: `backend/dotnet/BuildingBlocks/tests/Tw.Localization.Tests/LocalizationOptionsTests.cs`
- Test: `backend/dotnet/BuildingBlocks/tests/Tw.Localization.Tests/CultureFallbackTests.cs`

- [ ] **Step 1: Write failing tests**

```csharp
using FluentAssertions;
using Tw.Exceptions;
using Xunit;

namespace Tw.Localization.Tests;

public class LocalizationOptionsTests
{
    [Fact]
    public void Validate_RejectsInvalidDefaultCulture()
    {
        var options = new LocalizationOptions { DefaultCulture = "not a culture" };

        var act = () => options.Validate();

        act.Should().Throw<TwConfigurationException>();
    }

    [Fact]
    public void Validate_RequiresDefaultCultureInSupportedList()
    {
        var options = new LocalizationOptions
        {
            DefaultCulture = "en-US",
            SupportedCultures = { "zh-Hans" },
        };

        var act = () => options.Validate();

        act.Should().Throw<TwConfigurationException>();
    }
}

public class CultureFallbackTests
{
    [Fact]
    public void Expand_ReturnsCurrentParentAndDefault()
    {
        var options = new LocalizationOptions
        {
            DefaultCulture = "en-US",
            SupportedCultures = { "en-US", "zh", "zh-Hans" },
        };
        var context = new LocalizationContext("zh-Hans");

        CultureFallback.Expand(context, options).Should().Equal("zh-Hans", "zh", "en-US");
    }

    [Fact]
    public void Expand_DoesNotDuplicateDefault()
    {
        var options = new LocalizationOptions
        {
            DefaultCulture = "en-US",
            SupportedCultures = { "en-US" },
        };
        var context = new LocalizationContext("en-US");

        CultureFallback.Expand(context, options).Should().Equal("en-US");
    }
}
```

- [ ] **Step 2: Run tests to verify failure**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Localization.Tests/Tw.Localization.Tests.csproj --filter "LocalizationOptionsTests|CultureFallbackTests"`

Expected: compile fails because options and fallback do not exist。

- [ ] **Step 3: Implement options and fallback**

Contracts:

```csharp
namespace Tw.Localization;

public enum MissingTextBehavior
{
    ReturnKey = 0,
    ReturnEmptyString = 1,
    ReturnKeyAndRecordDiagnostic = 2,
}

public sealed class LocalizationOptions
{
    public string DefaultCulture { get; set; } = "en-US";
    public List<string> SupportedCultures { get; } = [];
    public List<string> JsonResourcePaths { get; } = [];
    public bool WatchJsonFiles { get; set; }
    public MissingTextBehavior MissingTextBehavior { get; set; } = MissingTextBehavior.ReturnKey;
    public bool FallbackToParentCultures { get; set; } = true;
    public bool FallbackToDefaultCulture { get; set; } = true;
    public bool AllowDuplicateResourceKeys { get; set; } = true;

    public void Validate()
    {
        if (!CultureFallback.IsValidCulture(DefaultCulture))
        {
            throw new TwConfigurationException($"默认 culture 无效：{DefaultCulture}");
        }

        if (SupportedCultures.Count == 0)
        {
            throw new TwConfigurationException("支持语言列表不能为空");
        }

        foreach (var culture in SupportedCultures)
        {
            if (!CultureFallback.IsValidCulture(culture))
            {
                throw new TwConfigurationException($"支持语言 culture 无效：{culture}");
            }
        }

        if (!SupportedCultures.Contains(DefaultCulture, StringComparer.OrdinalIgnoreCase))
        {
            throw new TwConfigurationException($"默认 culture 必须包含在支持语言列表中：{DefaultCulture}");
        }
    }
}

public static class CultureFallback
{
    public static bool IsValidCulture(string cultureName)
    {
        if (string.IsNullOrWhiteSpace(cultureName))
        {
            return false;
        }

        try
        {
            CultureInfo.GetCultureInfo(cultureName);
            return true;
        }
        catch (CultureNotFoundException)
        {
            return false;
        }
    }

    public static IReadOnlyList<string> Expand(LocalizationContext context, LocalizationOptions options)
    {
        Check.NotNull(context);
        Check.NotNull(options);

        var result = new List<string>();
        AddIfMissing(result, context.CultureName);

        if (context.FallbackToParentCultures && options.FallbackToParentCultures)
        {
            var culture = CultureInfo.GetCultureInfo(context.CultureName);
            while (!string.IsNullOrWhiteSpace(culture.Parent.Name))
            {
                culture = culture.Parent;
                AddIfMissing(result, culture.Name);
            }
        }

        if (context.FallbackToDefaultCulture && options.FallbackToDefaultCulture)
        {
            AddIfMissing(result, options.DefaultCulture);
        }

        return result;
    }

    private static void AddIfMissing(List<string> cultures, string cultureName)
    {
        if (!cultures.Contains(cultureName, StringComparer.OrdinalIgnoreCase))
        {
            cultures.Add(cultureName);
        }
    }
}
```

Rules:
- `Validate()` throws `TwConfigurationException` for invalid default culture, invalid supported culture, empty supported list after normalization, and default culture not present in supported cultures.
- `Expand()` includes current culture first.
- `Expand()` includes parent culture chain only when both `context.FallbackToParentCultures` and `options.FallbackToParentCultures` are true.
- `Expand()` includes default culture only when both `context.FallbackToDefaultCulture` and `options.FallbackToDefaultCulture` are true.
- `Expand()` removes duplicates while preserving order.

- [ ] **Step 4: Run tests**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Localization.Tests/Tw.Localization.Tests.csproj --filter "LocalizationOptionsTests|CultureFallbackTests"`

Expected: tests pass。

- [ ] **Step 5: Commit**

```powershell
git add backend/dotnet/BuildingBlocks/src/Tw.Localization backend/dotnet/BuildingBlocks/tests/Tw.Localization.Tests/LocalizationOptionsTests.cs backend/dotnet/BuildingBlocks/tests/Tw.Localization.Tests/CultureFallbackTests.cs
git commit -m "feat(localization): add options and culture fallback"
```

---

## Task 4: 接口、缓存契约与静态快照契约

**Files:**
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Localization/ITextLocalizer.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Localization/ITextResourceContributor.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Localization/IDynamicTextStore.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Localization/IEntityTranslationStore.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Localization/IEntityTranslationService.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Localization/IStaticTextSnapshot.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Localization/Caching/ILocalizationChangeToken.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Localization/Caching/ILocalizationCacheInvalidator.cs`
- Test: `backend/dotnet/BuildingBlocks/tests/Tw.Localization.Tests/InterfaceShapeTests.cs`

- [ ] **Step 1: Write failing shape tests**

```csharp
using FluentAssertions;
using Xunit;

namespace Tw.Localization.Tests;

public class InterfaceShapeTests
{
    [Fact]
    public void PublicInterfaces_LiveInTwLocalizationNamespace()
    {
        typeof(ITextLocalizer).Namespace.Should().Be("Tw.Localization");
        typeof(ITextResourceContributor).Namespace.Should().Be("Tw.Localization");
        typeof(IDynamicTextStore).Namespace.Should().Be("Tw.Localization");
        typeof(IEntityTranslationStore).Namespace.Should().Be("Tw.Localization");
        typeof(IEntityTranslationService).Namespace.Should().Be("Tw.Localization");
        typeof(IStaticTextSnapshot).Namespace.Should().Be("Tw.Localization");
    }
}
```

- [ ] **Step 2: Run test to verify failure**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Localization.Tests/Tw.Localization.Tests.csproj --filter InterfaceShapeTests`

Expected: compile fails because interfaces do not exist。

- [ ] **Step 3: Implement interfaces**

Use the signatures from the spec exactly, with optional `CancellationToken cancellationToken = default` parameters. Add `IStaticTextSnapshot` for the Web adapter's synchronous `IStringLocalizer` path:

```csharp
public interface IStaticTextSnapshot
{
    LocalizedText? Find(
        string resourceName,
        string name,
        IReadOnlyList<string> candidateCultureNames);

    IReadOnlyDictionary<string, LocalizedText> GetAll(
        string resourceName,
        IReadOnlyList<string> candidateCultureNames);
}
```

Caching contracts:

```csharp
namespace Tw.Localization.Caching;

public interface ILocalizationChangeToken
{
    IDisposable RegisterChangeCallback(Action<object?> callback, object? state);
}

public interface ILocalizationCacheInvalidator
{
    void InvalidateResource(string resourceName, string? cultureName = null, string? tenantId = null);
    void InvalidateEntity(string entityType, string? entityId = null, string? cultureName = null, string? tenantId = null);
}
```

- [ ] **Step 4: Run test**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Localization.Tests/Tw.Localization.Tests.csproj --filter InterfaceShapeTests`

Expected: tests pass。

- [ ] **Step 5: Commit**

```powershell
git add backend/dotnet/BuildingBlocks/src/Tw.Localization backend/dotnet/BuildingBlocks/tests/Tw.Localization.Tests/InterfaceShapeTests.cs
git commit -m "feat(localization): define localization contracts"
```

---

## Task 5: JSON 静态资源解析、贡献源与静态快照

**Files:**
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Localization/Json/JsonTextResource.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Localization/Json/JsonTextResourceParser.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Localization/Json/JsonTextResourceContributor.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Localization/StaticTextSnapshot.cs`
- Test: `backend/dotnet/BuildingBlocks/tests/Tw.Localization.Tests/JsonTextResourceParserTests.cs`
- Test: `backend/dotnet/BuildingBlocks/tests/Tw.Localization.Tests/JsonTextResourceContributorTests.cs`

- [ ] **Step 1: Write failing parser tests**

```csharp
using FluentAssertions;
using Tw.Exceptions;
using Tw.Localization.Json;
using Xunit;

namespace Tw.Localization.Tests;

public class JsonTextResourceParserTests
{
    [Fact]
    public void Parse_FlattensNestedObjects()
    {
        const string json = """
        {
          "culture": "zh-Hans",
          "texts": {
            "Menu": { "Dashboard": "控制台" },
            "Validation__Required": "必填"
          }
        }
        """;

        var resource = JsonTextResourceParser.Parse("App", "app.zh-Hans.json", json);

        resource.CultureName.Should().Be("zh-Hans");
        resource.Texts["Menu__Dashboard"].Should().Be("控制台");
        resource.Texts["Validation__Required"].Should().Be("必填");
    }

    [Fact]
    public void Parse_RejectsNonStringLeaf()
    {
        const string json = """{ "culture": "zh-Hans", "texts": { "Count": 1 } }""";

        var act = () => JsonTextResourceParser.Parse("App", "bad.json", json);

        act.Should().Throw<TwConfigurationException>();
    }
}
```

- [ ] **Step 2: Implement parser**

Contracts:

```csharp
namespace Tw.Localization.Json;

public sealed record JsonTextResource(
    string ResourceName,
    string CultureName,
    IReadOnlyDictionary<string, string> Texts);

public static class JsonTextResourceParser
{
    public static JsonTextResource Parse(string resourceName, string sourcePath, string json)
    {
        var validatedResourceName = Check.NotNullOrWhiteSpace(resourceName);
        var validatedSourcePath = Check.NotNullOrWhiteSpace(sourcePath);
        var validatedJson = Check.NotNullOrWhiteSpace(json);

        try
        {
            using var document = JsonDocument.Parse(validatedJson);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                throw new TwConfigurationException($"JSON 多语言资源根节点必须是对象：{validatedSourcePath}");
            }

            if (!root.TryGetProperty("culture", out var cultureElement) ||
                cultureElement.ValueKind != JsonValueKind.String)
            {
                throw new TwConfigurationException($"JSON 多语言资源缺少 culture 字符串：{validatedSourcePath}");
            }

            var cultureName = Check.NotNullOrWhiteSpace(cultureElement.GetString());
            if (!CultureFallback.IsValidCulture(cultureName))
            {
                throw new TwConfigurationException($"JSON 多语言资源 culture 无效：{validatedSourcePath}");
            }

            if (!root.TryGetProperty("texts", out var textsElement) ||
                textsElement.ValueKind != JsonValueKind.Object)
            {
                throw new TwConfigurationException($"JSON 多语言资源缺少 texts 对象：{validatedSourcePath}");
            }

            var texts = new Dictionary<string, string>(StringComparer.Ordinal);
            FlattenTexts(textsElement, prefix: string.Empty, texts, validatedSourcePath);
            return new JsonTextResource(validatedResourceName, cultureName, texts);
        }
        catch (JsonException exception)
        {
            throw new TwConfigurationException($"JSON 多语言资源格式错误：{validatedSourcePath}", exception);
        }
    }

    private static void FlattenTexts(
        JsonElement element,
        string prefix,
        IDictionary<string, string> texts,
        string sourcePath)
    {
        foreach (var property in element.EnumerateObject())
        {
            var key = string.IsNullOrWhiteSpace(prefix)
                ? property.Name
                : $"{prefix}__{property.Name}";

            if (property.Value.ValueKind == JsonValueKind.Object)
            {
                FlattenTexts(property.Value, key, texts, sourcePath);
                continue;
            }

            if (property.Value.ValueKind == JsonValueKind.String)
            {
                texts[key] = property.Value.GetString() ?? string.Empty;
                continue;
            }

            throw new TwConfigurationException($"JSON 多语言资源 texts 叶子值必须是字符串：{sourcePath}");
        }
    }
}
```

Parser rules:
- Root object must contain string `culture`.
- Root object must contain object `texts`.
- Leaf values under `texts` must be strings.
- Nested objects flatten with `__`.
- Arrays, numbers, booleans and null under `texts` throw `TwConfigurationException`.
- Error message includes `sourcePath` and does not include secret values.

- [ ] **Step 3: Write contributor tests**

```csharp
using FluentAssertions;
using Tw.Localization.Json;
using Tw.Localization.Requests;
using Xunit;

namespace Tw.Localization.Tests;

public class JsonTextResourceContributorTests
{
    [Fact]
    public async Task GetOrNullAsync_ReturnsCurrentCultureText()
    {
        var resource = new JsonTextResource("App", "zh-Hans", new Dictionary<string, string> { ["Menu"] = "菜单" });
        var contributor = new JsonTextResourceContributor([resource], priority: 0);
        var request = new TextLookupRequest("App", "Menu", new LocalizationContext("zh-Hans"), ["zh-Hans"]);

        var text = await contributor.GetOrNullAsync(request);

        text!.Value.Should().Be("菜单");
        text.Source.Should().Be(LocalizedTextSource.StaticJson);
    }

    [Fact]
    public void StaticSnapshot_ReturnsFallbackCultureText()
    {
        var resources = new[]
        {
            new JsonTextResource("App", "en-US", new Dictionary<string, string> { ["Menu"] = "Menu" }),
        };
        var snapshot = new StaticTextSnapshot(resources);

        var text = snapshot.Find("App", "Menu", ["zh-Hans", "en-US"]);

        text!.Value.Should().Be("Menu");
    }
}
```

- [ ] **Step 4: Implement contributor and snapshot**

Contributor rules:
- `Priority` is constructor value.
- `GetOrNullAsync` checks request resource name, candidate cultures in order, then key.
- `FillAsync` fills all texts for candidate cultures in low-to-high fallback order; later candidate cultures override earlier ones.

Snapshot rules:
- Constructor indexes by resource, culture and key.
- `Find` is synchronous and reads only static JSON texts.
- `GetAll` returns an immutable dictionary keyed by text key.

- [ ] **Step 5: Run JSON tests**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Localization.Tests/Tw.Localization.Tests.csproj --filter "JsonTextResourceParserTests|JsonTextResourceContributorTests"`

Expected: tests pass。

- [ ] **Step 6: Commit**

```powershell
git add backend/dotnet/BuildingBlocks/src/Tw.Localization backend/dotnet/BuildingBlocks/tests/Tw.Localization.Tests/JsonTextResourceParserTests.cs backend/dotnet/BuildingBlocks/tests/Tw.Localization.Tests/JsonTextResourceContributorTests.cs
git commit -m "feat(localization): add static json text resources"
```

---

## Task 6: 动态文案、系统文案编排与实体翻译服务

**Files:**
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Localization/DynamicTextContributor.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Localization/TextLocalizer.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Localization/EntityTranslationService.cs`
- Create: `backend/dotnet/BuildingBlocks/tests/Tw.Localization.Tests/Fakes/InMemoryDynamicTextStore.cs`
- Create: `backend/dotnet/BuildingBlocks/tests/Tw.Localization.Tests/Fakes/InMemoryEntityTranslationStore.cs`
- Test: `backend/dotnet/BuildingBlocks/tests/Tw.Localization.Tests/TextLocalizerTests.cs`
- Test: `backend/dotnet/BuildingBlocks/tests/Tw.Localization.Tests/EntityTranslationServiceTests.cs`

- [ ] **Step 1: Write failing text localizer tests**

```csharp
using FluentAssertions;
using Tw.Context;
using Tw.Localization.Json;
using Tw.Localization.Tests.Fakes;
using Xunit;

namespace Tw.Localization.Tests;

public class TextLocalizerTests
{
    [Fact]
    public async Task GetAsync_PrefersDynamicTenantText()
    {
        var options = new LocalizationOptions { DefaultCulture = "en-US", SupportedCultures = { "en-US", "zh-Hans" } };
        var staticContributor = new JsonTextResourceContributor(
            [new JsonTextResource("App", "zh-Hans", new Dictionary<string, string> { ["Menu"] = "静态菜单" })],
            priority: 0);
        var store = new InMemoryDynamicTextStore();
        store.Add(new LocalizedText("App", "Menu", "租户菜单", "zh-Hans", false, LocalizedTextSource.Dynamic), tenantId: "t1");
        var dynamicContributor = new DynamicTextContributor(store, priority: 100);
        var localizer = new TextLocalizer([staticContributor, dynamicContributor], options, new NullCancellationTokenProvider());

        var text = await localizer.GetAsync("App", "Menu", new LocalizationContext("zh-Hans") { TenantId = "t1" });

        text.Value.Should().Be("租户菜单");
    }

    [Fact]
    public async Task GetAsync_ReturnsNotFoundText_WhenMissing()
    {
        var options = new LocalizationOptions { DefaultCulture = "en-US", SupportedCultures = { "en-US" } };
        var localizer = new TextLocalizer([], options, new NullCancellationTokenProvider());

        var text = await localizer.GetAsync("App", "Missing", new LocalizationContext("en-US"));

        text.ResourceNotFound.Should().BeTrue();
        text.Value.Should().Be("Missing");
    }
}
```

- [ ] **Step 2: Write failing entity translation tests**

```csharp
using FluentAssertions;
using Tw.Context;
using Tw.Localization.Requests;
using Tw.Localization.Tests.Fakes;
using Xunit;

namespace Tw.Localization.Tests;

public class EntityTranslationServiceTests
{
    [Fact]
    public async Task GetFieldsAsync_UsesBatchStoreAndFallback()
    {
        var options = new LocalizationOptions { DefaultCulture = "en-US", SupportedCultures = { "en-US", "zh", "zh-Hans" } };
        var store = new InMemoryEntityTranslationStore();
        store.Add(new EntityTranslation("Product", "42", "Name", "zh", "父级名称", "t1"));
        var service = new EntityTranslationService(store, options, new NullCancellationTokenProvider());
        var query = new EntityTranslationBatchQuery(
            [new EntityTranslationKey("Product", "42", "Name")],
            new LocalizationContext("zh-Hans") { TenantId = "t1" });

        var result = await service.GetFieldsAsync(query);

        result[new EntityTranslationKey("Product", "42", "Name")].Value.Should().Be("父级名称");
        store.GetListCallCount.Should().Be(1);
    }
}
```

- [ ] **Step 3: Implement fakes**

Fake dynamic store behavior:
- Store records by tenant id, resource, culture and name.
- `FindAsync` and `GetListAsync` return all candidates contained in the request.
- Expose `GetListCallCount` for batch query assertions.

Fake entity store behavior:
- Store records by tenant id, entity type, entity id, field, culture.
- `GetListAsync` returns all records matching requested keys and candidate cultures.
- Expose `GetListCallCount`.

- [ ] **Step 4: Implement services**

Implementation rules:
- `TextLocalizer` sorts contributors by `Priority` descending for `GetAsync`.
- `TextLocalizer.GetAllAsync` sorts contributors by `Priority` ascending and lets later contributors override earlier values.
- Both services resolve token using `_cancellationTokenProvider.FallbackToProvider(cancellationToken)`.
- `EntityTranslationService` calls store once per `GetFieldsAsync`, then chooses translations by tenant priority and culture fallback order.
- Tenant priority is current tenant before global tenant.

- [ ] **Step 5: Run service tests**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Localization.Tests/Tw.Localization.Tests.csproj --filter "TextLocalizerTests|EntityTranslationServiceTests"`

Expected: tests pass。

- [ ] **Step 6: Commit**

```powershell
git add backend/dotnet/BuildingBlocks/src/Tw.Localization backend/dotnet/BuildingBlocks/tests/Tw.Localization.Tests
git commit -m "feat(localization): add text and entity translation services"
```

---

## Task 7: `AddLocalization` 服务注册

**Files:**
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Localization/LocalizationServiceCollectionExtensions.cs`
- Test: `backend/dotnet/BuildingBlocks/tests/Tw.Localization.Tests/LocalizationServiceCollectionExtensionsTests.cs`

- [ ] **Step 1: Write failing DI tests**

```csharp
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Tw.Localization.Tests;

public class LocalizationServiceCollectionExtensionsTests
{
    [Fact]
    public void AddLocalization_RegistersCoreServices()
    {
        var services = new ServiceCollection();

        services.AddLocalization(o =>
        {
            o.DefaultCulture = "en-US";
            o.SupportedCultures.Add("en-US");
        });

        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<ITextLocalizer>().Should().BeOfType<TextLocalizer>();
        provider.GetRequiredService<IEntityTranslationService>().Should().BeOfType<EntityTranslationService>();
        provider.GetRequiredService<IStaticTextSnapshot>().Should().NotBeNull();
    }
}
```

- [ ] **Step 2: Run test to verify failure**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Localization.Tests/Tw.Localization.Tests.csproj --filter LocalizationServiceCollectionExtensionsTests`

Expected: compile fails because `AddLocalization` does not exist。

- [ ] **Step 3: Implement registration**

Rules:
- Namespace is `Tw.Localization`.
- Method name is `AddLocalization`.
- The method accepts `Action<LocalizationOptions> configure`.
- It validates options at registration time.
- It calls `services.AddCancellationTokenProvider()` from `Tw.Context`.
- It registers `LocalizationOptions` singleton.
- It registers `ITextLocalizer`, `IEntityTranslationService`, `IStaticTextSnapshot`.
- It registers static JSON contributors loaded from `LocalizationOptions.JsonResourcePaths`.
- It does not register `IDynamicTextStore` or `IEntityTranslationStore`; business applications implement those.
- It does not use `Microsoft.Extensions.DependencyInjection` as the extension class namespace.

- [ ] **Step 4: Run DI tests**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Localization.Tests/Tw.Localization.Tests.csproj --filter LocalizationServiceCollectionExtensionsTests`

Expected: tests pass。

- [ ] **Step 5: Commit**

```powershell
git add backend/dotnet/BuildingBlocks/src/Tw.Localization/LocalizationServiceCollectionExtensions.cs backend/dotnet/BuildingBlocks/tests/Tw.Localization.Tests/LocalizationServiceCollectionExtensionsTests.cs
git commit -m "feat(localization): add core localization registration"
```

---

## Task 8: 共享包文档、索引和最终验证

**Files:**
- Create: `docs/shared-packages/dotnet/Tw.Localization/README.md`
- Create: `docs/shared-packages/dotnet/Tw.Localization/text-localization.md`
- Create: `docs/shared-packages/dotnet/Tw.Localization/entity-translation.md`
- Modify: `docs/shared-packages/dotnet/README.md`

- [ ] **Step 1: Write package README**

`README.md` 内容覆盖：
- 包定位：独立可选多语言核心包。
- 依赖边界：依赖 `Tw.Core`，不依赖 ASP.NET Core、EF Core、ORM。
- 能力入口：`Tw.Localization.LocalizationServiceCollectionExtensions.AddLocalization(...)`。
- 文档链接：`text-localization.md`、`entity-translation.md`。

- [ ] **Step 2: Write text localization how-to**

`text-localization.md` 内容覆盖：
- 注册示例。
- JSON 格式示例。
- `ITextLocalizer.GetAsync` 与 `GetAllAsync` 用法。
- `IDynamicTextStore` 由业务应用实现。
- 动态覆盖优先于静态 JSON。
- 缺失文案返回 key 并标记 `ResourceNotFound = true`。

- [ ] **Step 3: Write entity translation how-to**

`entity-translation.md` 内容覆盖：
- `IEntityTranslationStore` 由业务应用实现。
- `IEntityTranslationService.GetFieldAsync` 与 `GetFieldsAsync` 用法。
- 批量查询避免 N+1。
- 缺失翻译返回 `null`。
- 框架不自动覆盖实体原字段。

- [ ] **Step 4: Update dotnet shared package index**

在 `docs/shared-packages/dotnet/README.md` 增加 `Tw.Localization` 条目，链接到 `Tw.Localization/README.md`。

- [ ] **Step 5: Run final verification**

Run:

```powershell
dotnet build backend/dotnet/Tw.SmartPlatform.slnx
dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Localization.Tests/Tw.Localization.Tests.csproj
rg -n "Microsoft.AspNetCore|Microsoft.EntityFrameworkCore" backend/dotnet/BuildingBlocks/src/Tw.Localization
```

Expected:
- Build succeeds.
- Tests pass.
- `rg` returns no source package references to forbidden dependencies.

- [ ] **Step 6: Commit**

```powershell
git add docs/shared-packages/dotnet/Tw.Localization docs/shared-packages/dotnet/README.md
git commit -m "docs(shared-packages): document Tw.Localization"
```

---

## 完成标准

- `Tw.Localization` 项目和测试项目已加入解决方案。
- `Tw.Localization/package-charter.yaml` 存在且 `public_capabilities` 仅登记 `Tw.Localization`。
- 模型、请求 DTO、接口、JSON 解析、静态快照、动态贡献源、系统文案编排、实体翻译服务、`AddLocalization` 全部有测试覆盖。
- `Tw.Localization` 不引用 ASP.NET Core、EF Core 或具体 ORM。
- 共享包文档和索引可从 `docs/shared-packages/dotnet/README.md` 跳转。
