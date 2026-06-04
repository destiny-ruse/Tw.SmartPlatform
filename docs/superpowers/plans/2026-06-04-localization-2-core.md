# Tw.Core 多语言核心 Implementation Plan（多语言系列 Plan 2/3）

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 在 `Tw.Core` 新增框架无关、ORM 无关的多语言核心：语言/上下文/选项模型、请求与查询 DTO、culture 校验与回退展开、系统文案编排（含 JSON 静态贡献源与动态覆盖契约）、业务实体翻译服务，以及缓存失效契约与内存测试替身。

**Architecture:** 命名空间 `Tw.Localization`。编排层（`ITextLocalizer`、`IEntityTranslationService`）负责把 `LocalizationContext` + `LocalizationOptions` 展开为候选 culture 集合后一次性传给 store/contributor，store 不重复计算回退。系统文案来源用 `ITextResourceContributor`（JSON 静态、动态覆盖）按 `Priority` 组合：单 key 查询高→低首中即返回，批量填充低→高覆盖。业务实体翻译独立建模，批量查询避免 N+1。`Tw.Core` 不引用 `Microsoft.AspNetCore.*` 与 `Microsoft.EntityFrameworkCore*`。

**Tech Stack:** .NET 10、C#（file-scoped namespace、nullable enable、implicit usings、record 值对象）、`System.Text.Json`、xUnit、FluentAssertions。

**前置依赖：** Plan 1（DI 命名整改）已完成。

**适用规范（实现前必读）：**
- 设计稿 `docs/superpowers/specs/2026-06-04-localization-abstractions-design.md`（核心模型、请求与查询模型、核心接口、系统文案流、业务实体翻译流、缓存与失效、错误处理、测试策略各节）
- `docs/engineering-standards/03-project-and-code/language-specific/dotnet-core.md`
- `docs/engineering-standards/03-project-and-code/shared-package-charter.md`

**通用命令：**
- 构建：`dotnet build backend/dotnet/Tw.SmartPlatform.slnx`
- 测试 Tw.Core：`dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Tw.Core.Tests.csproj`
- 过滤单类：在上面命令后加 `--filter <ClassName>`

**约定：**
- 所有公共类型必须有 DocFX XML 文档注释（中文），方法注明异常语义。本计划代码块为节省篇幅省略部分 `<summary>`，实现时必须补全。
- 入参校验统一用 `Check.NotNull(...)`（已存在于 `Tw.Core`，命名空间 `Tw`，全局 implicit using 不含它——文件需 `using Tw;` 或依赖 RootNamespace `Tw`；`Check` 在根命名空间 `Tw` 下，同程序集内直接可见）。
- 取消令牌统一用 `provider.FallbackToProvider(cancellationToken)`（`Tw.Context`）。

---

## File Structure

源码（全部位于 `backend/dotnet/BuildingBlocks/src/Tw.Core/Localization/`，命名空间 `Tw.Localization`）：
- `LanguageInfo.cs` — 可用语言
- `LocalizationContext.cs` — 查询上下文
- `LocalizationOptions.cs` — 配置 + 校验
- `LocalizedText.cs` + `LocalizedTextSource.cs`(enum) — 系统文案结果
- `EntityTranslation.cs` — 实体字段翻译
- `Requests/TextLookupRequest.cs`、`Requests/TextFillRequest.cs`
- `Requests/EntityTranslationKey.cs`、`Requests/EntityTranslationLookup.cs`、`Requests/EntityTranslationQuery.cs`、`Requests/EntityTranslationBatchQuery.cs`
- `CultureFallback.cs` — culture 校验与候选展开
- `ITextResourceContributor.cs`、`IDynamicTextStore.cs`、`ITextLocalizer.cs`
- `IEntityTranslationStore.cs`、`IEntityTranslationService.cs`
- `Caching/ILocalizationChangeToken.cs`、`Caching/ILocalizationCacheInvalidator.cs`
- `Json/JsonTextResource.cs`、`Json/JsonTextResourceParser.cs`、`Json/JsonTextResourceContributor.cs`
- `IStaticTextSnapshot.cs`、`StaticTextSnapshot.cs` — 静态文案同步快照（供 Web `IStringLocalizer` 同步读取，见 Task 7B）
- `DynamicTextContributor.cs` — 包装 `IDynamicTextStore` 的贡献源
- `TextLocalizer.cs` — `ITextLocalizer` 编排实现
- `EntityTranslationService.cs` — `IEntityTranslationService` 实现
- `LocalizationServiceCollectionExtensions.cs` — `AddLocalization(...)`

测试（`backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Localization/`）：每个行为单元对应测试文件 + 内存测试替身 `Fakes/InMemoryDynamicTextStore.cs`、`Fakes/InMemoryEntityTranslationStore.cs`。

文档（Plan 收尾）：`docs/shared-packages/dotnet/Tw.Core/localization/text-localization.md`、`.../entity-translation.md` + 索引与 charter。

---

## Task 1: 核心值模型

**Files:**
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/Localization/LanguageInfo.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/Localization/LocalizationContext.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/Localization/LocalizedTextSource.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/Localization/LocalizedText.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/Localization/EntityTranslation.cs`
- Test: `backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Localization/CoreModelsTests.cs`

- [ ] **Step 1: Write the failing test**

```csharp
using FluentAssertions;
using Tw.Localization;
using Xunit;

namespace Tw.Core.Tests.Localization;

public class CoreModelsTests
{
    [Fact]
    public void LanguageInfo_UiCultureName_DefaultsToCultureName_WhenNull()
    {
        var info = new LanguageInfo("zh-Hans") { DisplayName = "简体中文" };

        info.UiCultureName.Should().Be("zh-Hans");
        info.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public void LanguageInfo_UiCultureName_UsesExplicitValue_WhenProvided()
    {
        var info = new LanguageInfo("zh-Hans") { UiCultureName = "zh-CN" };

        info.UiCultureName.Should().Be("zh-CN");
    }

    [Fact]
    public void LocalizationContext_Defaults_EnableBothFallbacks()
    {
        var context = new LocalizationContext("zh-Hans");

        context.TenantId.Should().BeNull();
        context.FallbackToParentCultures.Should().BeTrue();
        context.FallbackToDefaultCulture.Should().BeTrue();
    }

    [Fact]
    public void LocalizedText_NotFound_FactoryReturnsKeyAndFlag()
    {
        var text = LocalizedText.NotFound("Menu", "Missing", "zh-Hans");

        text.Value.Should().Be("Missing");
        text.ResourceNotFound.Should().BeTrue();
        text.Source.Should().Be(LocalizedTextSource.NotFound);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Tw.Core.Tests.csproj --filter CoreModelsTests`
Expected: 编译失败（类型不存在）。

- [ ] **Step 3: Write minimal implementation**

`LanguageInfo.cs`:

```csharp
namespace Tw.Localization;

/// <summary>表示一种可用语言。</summary>
public sealed record LanguageInfo
{
    /// <summary>初始化语言信息。</summary>
    /// <param name="cultureName">标准 culture 名称，例如 <c>zh-Hans</c>、<c>en-US</c>。</param>
    /// <exception cref="ArgumentException">当 <paramref name="cultureName"/> 为空或空白时抛出。</exception>
    public LanguageInfo(string cultureName)
    {
        Check.NotNullOrWhiteSpace(cultureName);
        CultureName = cultureName;
    }

    /// <summary>标准 culture 名称。</summary>
    public string CultureName { get; }

    private readonly string? _uiCultureName;

    /// <summary>界面 culture 名称，缺省等于 <see cref="CultureName"/>。</summary>
    public string UiCultureName
    {
        get => _uiCultureName ?? CultureName;
        init => _uiCultureName = value;
    }

    /// <summary>展示名称。</summary>
    public string? DisplayName { get; init; }

    /// <summary>是否启用，默认启用。</summary>
    public bool IsEnabled { get; init; } = true;

    /// <summary>排序值。</summary>
    public int SortOrder { get; init; }
}
```

> 若 `Check.NotNullOrWhiteSpace` 不存在，改用 `Check.NotNull` + `ArgumentException.ThrowIfNullOrWhiteSpace(cultureName)`。先用 grep 确认 `Check` 已有的方法集。

`LocalizationContext.cs`:

```csharp
namespace Tw.Localization;

/// <summary>表示一次本地化查询上下文。</summary>
public sealed record LocalizationContext
{
    /// <summary>初始化本地化上下文。</summary>
    /// <param name="cultureName">当前查询 culture 名称。</param>
    /// <exception cref="ArgumentException">当 <paramref name="cultureName"/> 为空或空白时抛出。</exception>
    public LocalizationContext(string cultureName)
    {
        Check.NotNullOrWhiteSpace(cultureName);
        CultureName = cultureName;
    }

    /// <summary>当前查询 culture 名称。</summary>
    public string CultureName { get; }

    /// <summary>租户标识；单租户应用传 <see langword="null"/>。</summary>
    public string? TenantId { get; init; }

    /// <summary>是否回退到父级 culture，默认开启。</summary>
    public bool FallbackToParentCultures { get; init; } = true;

    /// <summary>是否回退到默认 culture，默认开启。</summary>
    public bool FallbackToDefaultCulture { get; init; } = true;
}
```

`LocalizedTextSource.cs`:

```csharp
namespace Tw.Localization;

/// <summary>表达系统文案命中来源。</summary>
public enum LocalizedTextSource
{
    /// <summary>未命中任何来源。</summary>
    NotFound = 0,
    /// <summary>基础资源。</summary>
    BaseResource,
    /// <summary>静态 JSON 资源。</summary>
    StaticJson,
    /// <summary>动态覆盖。</summary>
    DynamicOverride,
}
```

`LocalizedText.cs`:

```csharp
namespace Tw.Localization;

/// <summary>表示系统文案查询结果。</summary>
public sealed record LocalizedText
{
    /// <summary>初始化系统文案结果。</summary>
    public LocalizedText(
        string name,
        string value,
        string cultureName,
        string resourceName,
        bool resourceNotFound,
        LocalizedTextSource source)
    {
        Name = name;
        Value = value;
        CultureName = cultureName;
        ResourceName = resourceName;
        ResourceNotFound = resourceNotFound;
        Source = source;
    }

    /// <summary>文案 key。</summary>
    public string Name { get; }
    /// <summary>文案值。</summary>
    public string Value { get; }
    /// <summary>命中文案所属 culture 名称。</summary>
    public string CultureName { get; }
    /// <summary>资源名。</summary>
    public string ResourceName { get; }
    /// <summary>是否未命中。</summary>
    public bool ResourceNotFound { get; }
    /// <summary>命中来源。</summary>
    public LocalizedTextSource Source { get; }

    /// <summary>构造一个未命中结果，值回退为 key。</summary>
    public static LocalizedText NotFound(string resourceName, string name, string cultureName) =>
        new(name, name, cultureName, resourceName, resourceNotFound: true, LocalizedTextSource.NotFound);
}
```

`EntityTranslation.cs`:

```csharp
namespace Tw.Localization;

/// <summary>表示业务实体字段翻译。</summary>
public sealed record EntityTranslation
{
    /// <summary>初始化实体字段翻译。</summary>
    public EntityTranslation(
        string entityType,
        string entityId,
        string fieldName,
        string cultureName,
        string value,
        string? tenantId = null)
    {
        EntityType = entityType;
        EntityId = entityId;
        FieldName = fieldName;
        CultureName = cultureName;
        Value = value;
        TenantId = tenantId;
    }

    /// <summary>稳定实体类型名，例如 <c>Product</c>。</summary>
    public string EntityType { get; }
    /// <summary>不透明实体标识字符串。</summary>
    public string EntityId { get; }
    /// <summary>可翻译字段名，例如 <c>Name</c>。</summary>
    public string FieldName { get; }
    /// <summary>翻译所属 culture 名称。</summary>
    public string CultureName { get; }
    /// <summary>翻译值。</summary>
    public string Value { get; }
    /// <summary>租户标识；全局翻译传 <see langword="null"/>。</summary>
    public string? TenantId { get; }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Tw.Core.Tests.csproj --filter CoreModelsTests`
Expected: PASS。

- [ ] **Step 5: Commit**

```bash
git add backend/dotnet/BuildingBlocks/src/Tw.Core/Localization backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Localization/CoreModelsTests.cs
git commit -m "feat(core): add localization core value models"
```

---

## Task 2: 请求与查询 DTO

**Files:**
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/Localization/Requests/TextLookupRequest.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/Localization/Requests/TextFillRequest.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/Localization/Requests/EntityTranslationKey.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/Localization/Requests/EntityTranslationLookup.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/Localization/Requests/EntityTranslationQuery.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/Localization/Requests/EntityTranslationBatchQuery.cs`
- Test: `backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Localization/RequestModelsTests.cs`

- [ ] **Step 1: Write the failing test**

```csharp
using FluentAssertions;
using Tw.Localization;
using Tw.Localization.Requests;
using Xunit;

namespace Tw.Core.Tests.Localization;

public class RequestModelsTests
{
    [Fact]
    public void EntityTranslationKey_Equality_IsValueBased()
    {
        var a = new EntityTranslationKey("Product", "1", "Name");
        var b = new EntityTranslationKey("Product", "1", "Name");
        var c = new EntityTranslationKey("Product", "1", "Title");

        a.Should().Be(b);
        a.GetHashCode().Should().Be(b.GetHashCode());
        a.Should().NotBe(c);
    }

    [Fact]
    public void TextLookupRequest_CarriesCandidateCultures()
    {
        var request = new TextLookupRequest(
            "Menu", "Dashboard",
            new LocalizationContext("zh-Hans"),
            candidateCultures: new[] { "zh-Hans", "zh", "en-US" });

        request.CandidateCultures.Should().ContainInOrder("zh-Hans", "zh", "en-US");
        request.Name.Should().Be("Dashboard");
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Tw.Core.Tests.csproj --filter RequestModelsTests`
Expected: 编译失败。

- [ ] **Step 3: Write minimal implementation**

`EntityTranslationKey.cs`（值对象，默认 record 即值相等）：

```csharp
namespace Tw.Localization.Requests;

/// <summary>业务实体翻译批量结果字典键。</summary>
/// <param name="EntityType">稳定实体类型名。</param>
/// <param name="EntityId">不透明实体标识。</param>
/// <param name="FieldName">可翻译字段名。</param>
public readonly record struct EntityTranslationKey(string EntityType, string EntityId, string FieldName);
```

`TextLookupRequest.cs`:

```csharp
namespace Tw.Localization.Requests;

/// <summary>单 key 系统文案查询请求，携带回退展开后的候选 culture 集合。</summary>
public sealed record TextLookupRequest
{
    /// <summary>初始化单 key 查询请求。</summary>
    public TextLookupRequest(
        string resourceName,
        string name,
        LocalizationContext context,
        IReadOnlyList<string> candidateCultures)
    {
        ResourceName = resourceName;
        Name = name;
        Context = context;
        CandidateCultures = candidateCultures;
    }

    /// <summary>资源名。</summary>
    public string ResourceName { get; }
    /// <summary>文案 key。</summary>
    public string Name { get; }
    /// <summary>原始查询上下文。</summary>
    public LocalizationContext Context { get; }
    /// <summary>从当前 culture 到默认 culture 的候选集合，按优先级从高到低。</summary>
    public IReadOnlyList<string> CandidateCultures { get; }
}
```

`TextFillRequest.cs`:

```csharp
namespace Tw.Localization.Requests;

/// <summary>批量系统文案填充请求，携带回退展开后的候选 culture 集合。</summary>
public sealed record TextFillRequest
{
    /// <summary>初始化批量填充请求。</summary>
    public TextFillRequest(
        string resourceName,
        LocalizationContext context,
        IReadOnlyList<string> candidateCultures)
    {
        ResourceName = resourceName;
        Context = context;
        CandidateCultures = candidateCultures;
    }

    /// <summary>资源名。</summary>
    public string ResourceName { get; }
    /// <summary>原始查询上下文。</summary>
    public LocalizationContext Context { get; }
    /// <summary>候选 culture 集合，按优先级从高到低。</summary>
    public IReadOnlyList<string> CandidateCultures { get; }
}
```

`EntityTranslationLookup.cs`:

```csharp
namespace Tw.Localization.Requests;

/// <summary>单实体单字段翻译查询。</summary>
public sealed record EntityTranslationLookup(EntityTranslationKey Key, LocalizationContext Context);
```

`EntityTranslationQuery.cs`（供 store 批量读取，不做回退）：

```csharp
namespace Tw.Localization.Requests;

/// <summary>供 <see cref="IEntityTranslationStore"/> 批量读取的查询，store 一次返回全部命中翻译。</summary>
public sealed record EntityTranslationQuery
{
    /// <summary>初始化批量读取查询。</summary>
    public EntityTranslationQuery(
        string entityType,
        IReadOnlyList<string> entityIds,
        IReadOnlyList<string> fieldNames,
        IReadOnlyList<string> candidateCultures,
        string? tenantId)
    {
        EntityType = entityType;
        EntityIds = entityIds;
        FieldNames = fieldNames;
        CandidateCultures = candidateCultures;
        TenantId = tenantId;
    }

    /// <summary>实体类型名。</summary>
    public string EntityType { get; }
    /// <summary>实体标识集合。</summary>
    public IReadOnlyList<string> EntityIds { get; }
    /// <summary>字段名集合。</summary>
    public IReadOnlyList<string> FieldNames { get; }
    /// <summary>候选 culture 集合。</summary>
    public IReadOnlyList<string> CandidateCultures { get; }
    /// <summary>当前租户；查询会同时覆盖全局（<see langword="null"/>）翻译。</summary>
    public string? TenantId { get; }
}
```

`EntityTranslationBatchQuery.cs`（供 service 批量查询 + 回退）：

```csharp
namespace Tw.Localization.Requests;

/// <summary>供 <see cref="IEntityTranslationService"/> 批量查询并执行回退的请求。</summary>
public sealed record EntityTranslationBatchQuery(
    IReadOnlyList<EntityTranslationKey> Keys,
    LocalizationContext Context);
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Tw.Core.Tests.csproj --filter RequestModelsTests`
Expected: PASS。

- [ ] **Step 5: Commit**

```bash
git add backend/dotnet/BuildingBlocks/src/Tw.Core/Localization/Requests backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Localization/RequestModelsTests.cs
git commit -m "feat(core): add localization request and query models"
```

---

## Task 3: `LocalizationOptions` 与启动校验

**Files:**
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/Localization/LocalizationOptions.cs`
- Test: `backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Localization/LocalizationOptionsTests.cs`

- [ ] **Step 1: Write the failing test**

```csharp
using FluentAssertions;
using Tw.Localization;
using Xunit;

namespace Tw.Core.Tests.Localization;

public class LocalizationOptionsTests
{
    private static LocalizationOptions Valid() => new()
    {
        DefaultCulture = "en-US",
        SupportedCultures = { "en-US", "zh-Hans" },
        ResourcePaths = { "Resources/Localization" },
    };

    [Fact]
    public void Validate_Passes_ForValidOptions()
    {
        var act = () => Valid().Validate();
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_Throws_WhenDefaultCultureInvalid()
    {
        var options = Valid();
        options.DefaultCulture = "not a culture";

        var act = () => options.Validate();
        act.Should().Throw<TwConfigurationException>();
    }

    [Fact]
    public void Validate_Throws_WhenSupportedCulturesEmpty()
    {
        var options = Valid();
        options.SupportedCultures.Clear();

        var act = () => options.Validate();
        act.Should().Throw<TwConfigurationException>();
    }

    [Fact]
    public void Validate_Throws_WhenResourcePathsRequiredButEmpty()
    {
        var options = Valid();
        options.ResourcePaths.Clear();
        options.RequireResourcePaths = true;

        var act = () => options.Validate();
        act.Should().Throw<TwConfigurationException>();
    }

    [Fact]
    public void MissingTextBehavior_DefaultsToReturnKey()
    {
        new LocalizationOptions().MissingTextBehavior
            .Should().Be(MissingTextBehavior.ReturnKey);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Tw.Core.Tests.csproj --filter LocalizationOptionsTests`
Expected: 编译失败。

- [ ] **Step 3: Write minimal implementation**

`LocalizationOptions.cs`（含 `MissingTextBehavior`、`DuplicateKeyBehavior` 枚举与 `Validate()`；culture 合法性用 `CultureFallback.IsValidCulture`，该方法在 Task 4 实现——本任务先内联一个静态校验，Task 4 收口为复用。为避免顺序耦合，本任务把 culture 校验写成私有方法 `IsValidCulture`，Task 4 完成后改为调用 `CultureFallback.IsValidCulture`）：

```csharp
using System.Globalization;

namespace Tw.Localization;

/// <summary>缺失系统文案时的行为。</summary>
public enum MissingTextBehavior
{
    /// <summary>返回 key 本身。</summary>
    ReturnKey = 0,
    /// <summary>返回空字符串。</summary>
    ReturnEmpty,
    /// <summary>返回 key 并记录诊断日志。</summary>
    ReturnKeyAndLog,
}

/// <summary>资源重复 key 时的行为。</summary>
public enum DuplicateKeyBehavior
{
    /// <summary>后加载值覆盖先加载值。</summary>
    Overwrite = 0,
    /// <summary>重复即视为配置错误。</summary>
    Throw,
}

/// <summary>本地化配置。</summary>
public sealed class LocalizationOptions
{
    /// <summary>默认 culture 名称。</summary>
    public string DefaultCulture { get; set; } = "en-US";

    /// <summary>支持语言列表。</summary>
    public IList<string> SupportedCultures { get; } = new List<string>();

    /// <summary>JSON 静态资源路径列表。</summary>
    public IList<string> ResourcePaths { get; } = new List<string>();

    /// <summary>资源路径是否必需，生产环境缺失即启动失败。</summary>
    public bool RequireResourcePaths { get; set; }

    /// <summary>是否监听 JSON 文件变更。</summary>
    public bool WatchFileChanges { get; set; }

    /// <summary>缺失文案行为，默认返回 key。</summary>
    public MissingTextBehavior MissingTextBehavior { get; set; } = MissingTextBehavior.ReturnKey;

    /// <summary>是否回退到父级 culture，作为上下文默认值。</summary>
    public bool FallbackToParentCultures { get; set; } = true;

    /// <summary>是否回退到默认 culture，作为上下文默认值。</summary>
    public bool FallbackToDefaultCulture { get; set; } = true;

    /// <summary>资源重复 key 行为，默认后值覆盖。</summary>
    public DuplicateKeyBehavior DuplicateKeyBehavior { get; set; } = DuplicateKeyBehavior.Overwrite;

    /// <summary>校验配置，非法时抛出。</summary>
    /// <exception cref="TwConfigurationException">默认 culture 非法、支持语言为空或含非法 culture、必需资源路径缺失时抛出。</exception>
    public void Validate()
    {
        if (!IsValidCulture(DefaultCulture))
            throw new TwConfigurationException($"默认 culture 非法：{DefaultCulture}");

        if (SupportedCultures.Count == 0)
            throw new TwConfigurationException("支持语言列表不能为空");

        foreach (var culture in SupportedCultures)
        {
            if (!IsValidCulture(culture))
                throw new TwConfigurationException($"支持语言包含非法 culture：{culture}");
        }

        if (RequireResourcePaths && ResourcePaths.Count == 0)
            throw new TwConfigurationException("资源路径为必需但未配置");
    }

    private static bool IsValidCulture(string cultureName)
    {
        if (string.IsNullOrWhiteSpace(cultureName))
            return false;
        try
        {
            _ = CultureInfo.GetCultureInfo(cultureName);
            return true;
        }
        catch (CultureNotFoundException)
        {
            return false;
        }
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Tw.Core.Tests.csproj --filter LocalizationOptionsTests`
Expected: PASS。

- [ ] **Step 5: Commit**

```bash
git add backend/dotnet/BuildingBlocks/src/Tw.Core/Localization/LocalizationOptions.cs backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Localization/LocalizationOptionsTests.cs
git commit -m "feat(core): add LocalizationOptions with startup validation"
```

---

## Task 4: culture 校验与回退展开 `CultureFallback`

**Files:**
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/Localization/CultureFallback.cs`
- Modify: `backend/dotnet/BuildingBlocks/src/Tw.Core/Localization/LocalizationOptions.cs`（把私有 `IsValidCulture` 改为调用 `CultureFallback.IsValidCulture`，删除私有方法）
- Test: `backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Localization/CultureFallbackTests.cs`

- [ ] **Step 1: Write the failing test**

```csharp
using FluentAssertions;
using Tw.Localization;
using Xunit;

namespace Tw.Core.Tests.Localization;

public class CultureFallbackTests
{
    [Fact]
    public void IsValidCulture_True_ForKnownCulture()
    {
        CultureFallback.IsValidCulture("zh-Hans").Should().BeTrue();
    }

    [Fact]
    public void IsValidCulture_False_ForGarbage()
    {
        CultureFallback.IsValidCulture("not a culture").Should().BeFalse();
        CultureFallback.IsValidCulture("").Should().BeFalse();
    }

    [Fact]
    public void ExpandCandidates_CurrentThenParentsThenDefault()
    {
        var context = new LocalizationContext("zh-Hans-CN");

        var candidates = CultureFallback.ExpandCandidates(context, defaultCulture: "en-US");

        candidates.Should().ContainInOrder("zh-Hans-CN", "zh-Hans", "zh", "en-US");
    }

    [Fact]
    public void ExpandCandidates_NoParents_WhenDisabled()
    {
        var context = new LocalizationContext("zh-Hans-CN") { FallbackToParentCultures = false };

        var candidates = CultureFallback.ExpandCandidates(context, defaultCulture: "en-US");

        candidates.Should().ContainInOrder("zh-Hans-CN", "en-US");
        candidates.Should().NotContain("zh-Hans");
    }

    [Fact]
    public void ExpandCandidates_NoDefault_WhenDisabled()
    {
        var context = new LocalizationContext("zh-Hans") { FallbackToDefaultCulture = false };

        var candidates = CultureFallback.ExpandCandidates(context, defaultCulture: "en-US");

        candidates.Should().NotContain("en-US");
    }

    [Fact]
    public void ExpandCandidates_Deduplicates()
    {
        var context = new LocalizationContext("en-US");

        var candidates = CultureFallback.ExpandCandidates(context, defaultCulture: "en-US");

        candidates.Should().ContainSingle(c => c == "en-US");
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Tw.Core.Tests.csproj --filter CultureFallbackTests`
Expected: 编译失败。

- [ ] **Step 3: Write minimal implementation**

`CultureFallback.cs`:

```csharp
using System.Globalization;

namespace Tw.Localization;

/// <summary>提供 culture 合法性校验与回退候选展开。</summary>
public static class CultureFallback
{
    /// <summary>判断 culture 名称是否合法。</summary>
    public static bool IsValidCulture(string cultureName)
    {
        if (string.IsNullOrWhiteSpace(cultureName))
            return false;
        try
        {
            _ = CultureInfo.GetCultureInfo(cultureName);
            return true;
        }
        catch (CultureNotFoundException)
        {
            return false;
        }
    }

    /// <summary>把上下文展开为从高到低的候选 culture 集合：当前 → 父级链 → 默认。</summary>
    /// <param name="context">查询上下文。</param>
    /// <param name="defaultCulture">默认 culture。</param>
    /// <returns>去重后的候选 culture 列表，保持优先级顺序。</returns>
    public static IReadOnlyList<string> ExpandCandidates(LocalizationContext context, string defaultCulture)
    {
        Check.NotNull(context);

        var result = new List<string>();
        void Add(string name)
        {
            if (!string.IsNullOrWhiteSpace(name) && !result.Contains(name, StringComparer.OrdinalIgnoreCase))
                result.Add(name);
        }

        Add(context.CultureName);

        if (context.FallbackToParentCultures)
        {
            CultureInfo current;
            try { current = CultureInfo.GetCultureInfo(context.CultureName); }
            catch (CultureNotFoundException) { current = CultureInfo.InvariantCulture; }

            var parent = current.Parent;
            while (!string.IsNullOrEmpty(parent.Name))
            {
                Add(parent.Name);
                parent = parent.Parent;
            }
        }

        if (context.FallbackToDefaultCulture)
            Add(defaultCulture);

        return result;
    }
}
```

- [ ] **Step 4: 把 `LocalizationOptions.Validate` 改为复用 `CultureFallback.IsValidCulture`**

编辑 `LocalizationOptions.cs`：把 `Validate()` 内的 `IsValidCulture(...)` 调用改为 `CultureFallback.IsValidCulture(...)`，删除私有静态 `IsValidCulture` 方法与不再需要的 `using System.Globalization;`。

- [ ] **Step 5: Run tests to verify they pass**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Tw.Core.Tests.csproj --filter "CultureFallbackTests|LocalizationOptionsTests"`
Expected: PASS（两个测试类全部通过）。

- [ ] **Step 6: Commit**

```bash
git add backend/dotnet/BuildingBlocks/src/Tw.Core/Localization/CultureFallback.cs backend/dotnet/BuildingBlocks/src/Tw.Core/Localization/LocalizationOptions.cs backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Localization/CultureFallbackTests.cs
git commit -m "feat(core): add CultureFallback validation and candidate expansion"
```

---

## Task 5: 核心接口与缓存契约

**Files:**
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/Localization/ITextResourceContributor.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/Localization/IDynamicTextStore.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/Localization/ITextLocalizer.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/Localization/IEntityTranslationStore.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/Localization/IEntityTranslationService.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/Localization/Caching/ILocalizationChangeToken.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/Localization/Caching/ILocalizationCacheInvalidator.cs`
- Test: `backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Localization/InterfaceShapeTests.cs`

> 接口无行为，测试只验证签名可用（用最小 Fake 实现 + 反射断言方法存在），保证后续任务编译契约稳定。

- [ ] **Step 1: Write the failing test**

```csharp
using FluentAssertions;
using Tw.Localization;
using Xunit;

namespace Tw.Core.Tests.Localization;

public class InterfaceShapeTests
{
    [Fact]
    public void Interfaces_Exist_AndAreInLocalizationNamespace()
    {
        typeof(ITextLocalizer).Namespace.Should().Be("Tw.Localization");
        typeof(ITextResourceContributor).Namespace.Should().Be("Tw.Localization");
        typeof(IDynamicTextStore).Namespace.Should().Be("Tw.Localization");
        typeof(IEntityTranslationStore).Namespace.Should().Be("Tw.Localization");
        typeof(IEntityTranslationService).Namespace.Should().Be("Tw.Localization");
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Tw.Core.Tests.csproj --filter InterfaceShapeTests`
Expected: 编译失败。

- [ ] **Step 3: Write minimal implementation**

`ITextResourceContributor.cs`:

```csharp
using Tw.Localization.Requests;

namespace Tw.Localization;

/// <summary>表示一个系统文案来源（静态 JSON、动态覆盖等）。</summary>
public interface ITextResourceContributor
{
    /// <summary>优先级，数值大者优先。</summary>
    int Priority { get; }

    /// <summary>按单 key 查询返回命中文案，未命中返回 <see langword="null"/>。</summary>
    ValueTask<LocalizedText?> GetOrNullAsync(
        TextLookupRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>把本来源命中的文案填入累积字典，高优先级覆盖低优先级。</summary>
    ValueTask FillAsync(
        TextFillRequest request,
        IDictionary<string, LocalizedText> texts,
        CancellationToken cancellationToken = default);
}
```

`IDynamicTextStore.cs`:

```csharp
using Tw.Localization.Requests;

namespace Tw.Localization;

/// <summary>动态系统文案覆盖仓储，由业务应用实现。</summary>
public interface IDynamicTextStore
{
    /// <summary>按候选维度一次查询命中覆盖文案，未命中返回 <see langword="null"/>。</summary>
    ValueTask<LocalizedText?> FindAsync(
        TextLookupRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>按候选维度一次返回资源下全部命中覆盖文案。</summary>
    ValueTask<IReadOnlyList<LocalizedText>> GetListAsync(
        TextFillRequest request,
        CancellationToken cancellationToken = default);
}
```

`ITextLocalizer.cs`:

```csharp
using Tw.Localization.Requests;

namespace Tw.Localization;

/// <summary>系统文案编排入口，组合贡献源并执行回退链。</summary>
public interface ITextLocalizer
{
    /// <summary>查询单条文案，缺失返回 key 且 <see cref="LocalizedText.ResourceNotFound"/> 为 true。</summary>
    ValueTask<LocalizedText> GetAsync(
        string resourceName,
        string name,
        LocalizationContext context,
        CancellationToken cancellationToken = default);

    /// <summary>查询资源下全部文案。</summary>
    ValueTask<IReadOnlyList<LocalizedText>> GetAllAsync(
        string resourceName,
        LocalizationContext context,
        CancellationToken cancellationToken = default);
}
```

`IEntityTranslationStore.cs`:

```csharp
using Tw.Localization.Requests;

namespace Tw.Localization;

/// <summary>业务实体翻译仓储，由业务应用实现，一次返回全部命中翻译，不做回退。</summary>
public interface IEntityTranslationStore
{
    /// <summary>按查询批量读取命中翻译。</summary>
    ValueTask<IReadOnlyList<EntityTranslation>> GetListAsync(
        EntityTranslationQuery query,
        CancellationToken cancellationToken = default);
}
```

`IEntityTranslationService.cs`:

```csharp
using Tw.Localization.Requests;

namespace Tw.Localization;

/// <summary>业务实体翻译查询服务，负责批量查询、回退与结果组装。</summary>
public interface IEntityTranslationService
{
    /// <summary>查询单实体单字段翻译，缺失返回 <see langword="null"/>。</summary>
    ValueTask<string?> GetFieldAsync(
        EntityTranslationLookup lookup,
        CancellationToken cancellationToken = default);

    /// <summary>批量查询并回退，返回按 <see cref="EntityTranslationKey"/> 索引的命中翻译。</summary>
    ValueTask<IReadOnlyDictionary<EntityTranslationKey, EntityTranslation>> GetFieldsAsync(
        EntityTranslationBatchQuery query,
        CancellationToken cancellationToken = default);
}
```

`Caching/ILocalizationChangeToken.cs`:

```csharp
namespace Tw.Localization.Caching;

/// <summary>表示一次本地化缓存变更信号。</summary>
public interface ILocalizationChangeToken
{
    /// <summary>变更是否已发生。</summary>
    bool HasChanged { get; }

    /// <summary>注册变更回调，返回取消注册的句柄。</summary>
    IDisposable RegisterChangeCallback(Action<object?> callback, object? state);
}
```

`Caching/ILocalizationCacheInvalidator.cs`:

```csharp
namespace Tw.Localization.Caching;

/// <summary>本地化缓存失效契约，按维度精准失效，避免整体重建。</summary>
public interface ILocalizationCacheInvalidator
{
    /// <summary>使指定资源在指定租户、culture 下的缓存失效；参数为 <see langword="null"/> 表示该维度全部。</summary>
    void Invalidate(string? tenantId, string? resourceName, string? cultureName);
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Tw.Core.Tests.csproj --filter InterfaceShapeTests`
Expected: PASS。

- [ ] **Step 5: Commit**

```bash
git add backend/dotnet/BuildingBlocks/src/Tw.Core/Localization backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Localization/InterfaceShapeTests.cs
git commit -m "feat(core): add localization core interfaces and cache contracts"
```

---

## Task 6: JSON 静态资源解析

**Files:**
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/Localization/Json/JsonTextResource.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/Localization/Json/JsonTextResourceParser.cs`
- Test: `backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Localization/JsonTextResourceParserTests.cs`

JSON 规则（设计稿「JSON 静态资源」）：`culture` 必须合法；`texts` 叶子值只能是字符串，对象仅用于分组；嵌套对象扁平化为 `Menu__Dashboard`；非字符串叶子值按格式错误处理；同 culture 可拆多文件、后值覆盖前值（受 `DuplicateKeyBehavior` 控制）。

- [ ] **Step 1: Write the failing test**

```csharp
using FluentAssertions;
using Tw.Localization;
using Tw.Localization.Json;
using Xunit;

namespace Tw.Core.Tests.Localization;

public class JsonTextResourceParserTests
{
    [Fact]
    public void Parse_FlattensNestedObjects_WithDoubleUnderscore()
    {
        const string json = """
        { "culture": "zh-Hans", "texts": { "Menu": { "Dashboard": "控制台" }, "Validation__Required": "必填" } }
        """;

        var resource = JsonTextResourceParser.Parse(json, "menu.json");

        resource.CultureName.Should().Be("zh-Hans");
        resource.Entries["Menu__Dashboard"].Should().Be("控制台");
        resource.Entries["Validation__Required"].Should().Be("必填");
    }

    [Fact]
    public void Parse_Throws_OnNonStringLeaf()
    {
        const string json = """ { "culture": "zh-Hans", "texts": { "Count": 5 } } """;

        var act = () => JsonTextResourceParser.Parse(json, "bad.json");

        act.Should().Throw<TwConfigurationException>()
            .Which.Message.Should().Contain("bad.json");
    }

    [Fact]
    public void Parse_Throws_OnInvalidCulture()
    {
        const string json = """ { "culture": "nope", "texts": {} } """;

        var act = () => JsonTextResourceParser.Parse(json, "x.json");
        act.Should().Throw<TwConfigurationException>();
    }

    [Fact]
    public void Parse_Throws_OnMalformedJson()
    {
        var act = () => JsonTextResourceParser.Parse("{ not json", "x.json");
        act.Should().Throw<TwConfigurationException>().Which.Message.Should().Contain("x.json");
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Tw.Core.Tests.csproj --filter JsonTextResourceParserTests`
Expected: 编译失败。

- [ ] **Step 3: Write minimal implementation**

`JsonTextResource.cs`:

```csharp
namespace Tw.Localization.Json;

/// <summary>单个已解析的 JSON 静态文案资源。</summary>
public sealed class JsonTextResource
{
    /// <summary>初始化已解析资源。</summary>
    public JsonTextResource(string cultureName, IReadOnlyDictionary<string, string> entries)
    {
        CultureName = cultureName;
        Entries = entries;
    }

    /// <summary>资源 culture 名称。</summary>
    public string CultureName { get; }

    /// <summary>扁平化后的 key → 文案值字典。</summary>
    public IReadOnlyDictionary<string, string> Entries { get; }
}
```

`JsonTextResourceParser.cs`:

```csharp
using System.Text.Json;

namespace Tw.Localization.Json;

/// <summary>把 JSON 静态文案文件解析为 <see cref="JsonTextResource"/>。</summary>
public static class JsonTextResourceParser
{
    private const string Separator = "__";

    /// <summary>解析 JSON 文本。</summary>
    /// <param name="json">文件内容。</param>
    /// <param name="sourcePath">来源路径，用于错误诊断。</param>
    /// <exception cref="TwConfigurationException">JSON 非法、culture 非法或存在非字符串叶子值时抛出。</exception>
    public static JsonTextResource Parse(string json, string sourcePath)
    {
        JsonDocument document;
        try { document = JsonDocument.Parse(json); }
        catch (JsonException ex)
        {
            throw new TwConfigurationException($"本地化资源 JSON 格式错误：{sourcePath}", ex);
        }

        using (document)
        {
            var root = document.RootElement;
            if (!root.TryGetProperty("culture", out var cultureElement) ||
                cultureElement.ValueKind != JsonValueKind.String)
            {
                throw new TwConfigurationException($"本地化资源缺少 culture 字段：{sourcePath}");
            }

            var culture = cultureElement.GetString()!;
            if (!CultureFallback.IsValidCulture(culture))
                throw new TwConfigurationException($"本地化资源 culture 非法：{culture}（{sourcePath}）");

            var entries = new Dictionary<string, string>(StringComparer.Ordinal);
            if (root.TryGetProperty("texts", out var texts) && texts.ValueKind == JsonValueKind.Object)
                Flatten(texts, prefix: null, entries, sourcePath);

            return new JsonTextResource(culture, entries);
        }
    }

    private static void Flatten(
        JsonElement element, string? prefix, IDictionary<string, string> entries, string sourcePath)
    {
        foreach (var property in element.EnumerateObject())
        {
            var key = prefix is null ? property.Name : prefix + Separator + property.Name;
            switch (property.Value.ValueKind)
            {
                case JsonValueKind.Object:
                    Flatten(property.Value, key, entries, sourcePath);
                    break;
                case JsonValueKind.String:
                    entries[key] = property.Value.GetString()!;
                    break;
                default:
                    throw new TwConfigurationException(
                        $"本地化资源叶子值必须是字符串：{key}（{sourcePath}）");
            }
        }
    }
}
```

> 确认 `TwConfigurationException` 有 `(string message, Exception inner)` 构造。先用 grep/Read 看 `backend/dotnet/BuildingBlocks/src/Tw.Core/Exceptions/TwConfigurationException.cs`；若无 inner 构造，改为 `new TwConfigurationException($"...: {sourcePath}")`，不传 inner。

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Tw.Core.Tests.csproj --filter JsonTextResourceParserTests`
Expected: PASS。

- [ ] **Step 5: Commit**

```bash
git add backend/dotnet/BuildingBlocks/src/Tw.Core/Localization/Json backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Localization/JsonTextResourceParserTests.cs
git commit -m "feat(core): add JSON static text resource parser"
```

---

## Task 7: JSON 静态贡献源

**Files:**
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/Localization/Json/JsonTextResourceContributor.cs`
- Test: `backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Localization/JsonTextResourceContributorTests.cs`

贡献源职责：持有「culture → 合并后的 key→value 字典」（拆分文件已合并、按 `DuplicateKeyBehavior` 处理重复）。`Priority` 取较低值（静态低于动态）。`GetOrNullAsync` 按 `request.CandidateCultures` 顺序找首个命中；`FillAsync` 按候选 culture 从低到高填充（默认/父级先、当前后），结果 `Source = StaticJson`。

为隔离文件 IO，贡献源接收已解析的 `IReadOnlyList<JsonTextResource>`（文件加载在注册层做，Task 12）。资源名维度：本设计 JSON 贡献源服务于「单一资源名」——构造时传入 `resourceName`，只对匹配的 `request.ResourceName` 响应。

- [ ] **Step 1: Write the failing test**

```csharp
using FluentAssertions;
using Tw.Localization;
using Tw.Localization.Json;
using Tw.Localization.Requests;
using Xunit;

namespace Tw.Core.Tests.Localization;

public class JsonTextResourceContributorTests
{
    private static JsonTextResourceContributor Build() => new(
        resourceName: "Menu",
        priority: 0,
        resources: new[]
        {
            new JsonTextResource("en-US", new Dictionary<string, string> { ["Dashboard"] = "Dashboard", ["Home"] = "Home" }),
            new JsonTextResource("zh-Hans", new Dictionary<string, string> { ["Dashboard"] = "控制台" }),
        });

    [Fact]
    public async Task GetOrNull_ReturnsCurrentCulture_WhenPresent()
    {
        var request = new TextLookupRequest("Menu", "Dashboard",
            new LocalizationContext("zh-Hans"), new[] { "zh-Hans", "en-US" });

        var result = await Build().GetOrNullAsync(request);

        result!.Value.Should().Be("控制台");
        result.CultureName.Should().Be("zh-Hans");
        result.Source.Should().Be(LocalizedTextSource.StaticJson);
    }

    [Fact]
    public async Task GetOrNull_FallsBackToDefaultCulture()
    {
        var request = new TextLookupRequest("Menu", "Home",
            new LocalizationContext("zh-Hans"), new[] { "zh-Hans", "en-US" });

        var result = await Build().GetOrNullAsync(request);

        result!.Value.Should().Be("Home");
        result.CultureName.Should().Be("en-US");
    }

    [Fact]
    public async Task GetOrNull_ReturnsNull_ForOtherResource()
    {
        var request = new TextLookupRequest("Other", "Dashboard",
            new LocalizationContext("zh-Hans"), new[] { "zh-Hans" });

        (await Build().GetOrNullAsync(request)).Should().BeNull();
    }

    [Fact]
    public async Task Fill_CurrentCultureOverridesFallback()
    {
        var request = new TextFillRequest("Menu",
            new LocalizationContext("zh-Hans"), new[] { "zh-Hans", "en-US" });
        var bag = new Dictionary<string, LocalizedText>();

        await Build().FillAsync(request, bag);

        bag["Dashboard"].Value.Should().Be("控制台"); // 当前 culture 覆盖默认
        bag["Home"].Value.Should().Be("Home");        // 仅默认 culture 存在
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Tw.Core.Tests.csproj --filter JsonTextResourceContributorTests`
Expected: 编译失败。

- [ ] **Step 3: Write minimal implementation**

```csharp
using Tw.Localization.Requests;

namespace Tw.Localization.Json;

/// <summary>基于已解析 JSON 资源的静态文案贡献源，服务单一资源名。</summary>
public sealed class JsonTextResourceContributor : ITextResourceContributor
{
    private readonly string _resourceName;
    private readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> _byCulture;

    /// <summary>初始化静态贡献源。</summary>
    /// <param name="resourceName">本贡献源服务的资源名。</param>
    /// <param name="priority">优先级，静态资源应低于动态覆盖。</param>
    /// <param name="resources">同资源名下各 culture 的已解析资源（同 culture 可多份，后份覆盖前份）。</param>
    public JsonTextResourceContributor(
        string resourceName, int priority, IEnumerable<JsonTextResource> resources)
    {
        _resourceName = Check.NotNull(resourceName);
        Priority = priority;

        var map = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var resource in resources)
        {
            if (!map.TryGetValue(resource.CultureName, out var dict))
            {
                dict = new Dictionary<string, string>(StringComparer.Ordinal);
                map[resource.CultureName] = dict;
            }
            foreach (var (key, value) in resource.Entries)
                dict[key] = value; // 后份覆盖
        }
        _byCulture = map.ToDictionary(
            p => p.Key,
            p => (IReadOnlyDictionary<string, string>)p.Value,
            StringComparer.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public int Priority { get; }

    /// <inheritdoc />
    public ValueTask<LocalizedText?> GetOrNullAsync(
        TextLookupRequest request, CancellationToken cancellationToken = default)
    {
        Check.NotNull(request);
        if (!string.Equals(request.ResourceName, _resourceName, StringComparison.OrdinalIgnoreCase))
            return ValueTask.FromResult<LocalizedText?>(null);

        foreach (var culture in request.CandidateCultures)
        {
            if (_byCulture.TryGetValue(culture, out var dict) && dict.TryGetValue(request.Name, out var value))
            {
                return ValueTask.FromResult<LocalizedText?>(new LocalizedText(
                    request.Name, value, culture, _resourceName,
                    resourceNotFound: false, LocalizedTextSource.StaticJson));
            }
        }
        return ValueTask.FromResult<LocalizedText?>(null);
    }

    /// <inheritdoc />
    public ValueTask FillAsync(
        TextFillRequest request, IDictionary<string, LocalizedText> texts,
        CancellationToken cancellationToken = default)
    {
        Check.NotNull(request);
        Check.NotNull(texts);
        if (!string.Equals(request.ResourceName, _resourceName, StringComparison.OrdinalIgnoreCase))
            return ValueTask.CompletedTask;

        // 候选从高到低；为实现「高覆盖低」，逆序填充（低优先 culture 先写，高优先 culture 后覆盖）。
        for (var i = request.CandidateCultures.Count - 1; i >= 0; i--)
        {
            var culture = request.CandidateCultures[i];
            if (!_byCulture.TryGetValue(culture, out var dict))
                continue;
            foreach (var (key, value) in dict)
            {
                texts[key] = new LocalizedText(
                    key, value, culture, _resourceName,
                    resourceNotFound: false, LocalizedTextSource.StaticJson);
            }
        }
        return ValueTask.CompletedTask;
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Tw.Core.Tests.csproj --filter JsonTextResourceContributorTests`
Expected: PASS。

- [ ] **Step 5: Commit**

```bash
git add backend/dotnet/BuildingBlocks/src/Tw.Core/Localization/Json/JsonTextResourceContributor.cs backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Localization/JsonTextResourceContributorTests.cs
git commit -m "feat(core): add JSON static text resource contributor"
```

---

## Task 7B: 静态文案同步快照 `IStaticTextSnapshot`

**Files:**
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/Localization/IStaticTextSnapshot.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/Localization/StaticTextSnapshot.cs`
- Test: `backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Localization/StaticTextSnapshotTests.cs`

目的：`IStringLocalizer`（Plan 3）索引器是同步的，不能阻塞调用异步 `ITextLocalizer`。该快照只读内存中的静态 JSON 文案，提供同步查找（按候选 culture 高→低）；动态覆盖仍由异步 `ITextLocalizer` 负责。

- [ ] **Step 1: Write the failing test**

```csharp
using FluentAssertions;
using Tw.Localization;
using Tw.Localization.Json;
using Xunit;

namespace Tw.Core.Tests.Localization;

public class StaticTextSnapshotTests
{
    private static StaticTextSnapshot Build() => new(new[]
    {
        ("Menu", new JsonTextResource("en-US", new Dictionary<string, string> { ["Dashboard"] = "Dashboard", ["Home"] = "Home" })),
        ("Menu", new JsonTextResource("zh-Hans", new Dictionary<string, string> { ["Dashboard"] = "控制台" })),
    });

    [Fact]
    public void Find_ReturnsCurrentCulture_WhenPresent()
    {
        Build().Find("Menu", "Dashboard", new[] { "zh-Hans", "en-US" }).Should().Be("控制台");
    }

    [Fact]
    public void Find_FallsBack_ToNextCandidate()
    {
        Build().Find("Menu", "Home", new[] { "zh-Hans", "en-US" }).Should().Be("Home");
    }

    [Fact]
    public void Find_ReturnsNull_WhenMissing()
    {
        Build().Find("Menu", "Nope", new[] { "zh-Hans", "en-US" }).Should().BeNull();
        Build().Find("Other", "Dashboard", new[] { "zh-Hans" }).Should().BeNull();
    }

    [Fact]
    public void GetAll_MergesWithCurrentCultureWinning()
    {
        var all = Build().GetAll("Menu", new[] { "zh-Hans", "en-US" });
        all["Dashboard"].Should().Be("控制台");
        all["Home"].Should().Be("Home");
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Tw.Core.Tests.csproj --filter StaticTextSnapshotTests`
Expected: 编译失败。

- [ ] **Step 3: Write minimal implementation**

`IStaticTextSnapshot.cs`:

```csharp
namespace Tw.Localization;

/// <summary>静态文案的同步只读快照，供同步本地化适配器使用。</summary>
public interface IStaticTextSnapshot
{
    /// <summary>按候选 culture 从高到低查找单条静态文案，缺失返回 <see langword="null"/>。</summary>
    string? Find(string resourceName, string name, IReadOnlyList<string> candidateCultures);

    /// <summary>返回资源在候选 culture 下合并后的全部静态文案，当前 culture 覆盖回退。</summary>
    IReadOnlyDictionary<string, string> GetAll(string resourceName, IReadOnlyList<string> candidateCultures);
}
```

`StaticTextSnapshot.cs`:

```csharp
using Tw.Localization.Json;

namespace Tw.Localization;

/// <summary>基于已解析 JSON 资源的静态文案同步快照。</summary>
public sealed class StaticTextSnapshot : IStaticTextSnapshot
{
    // resourceName -> culture -> (key -> value)
    private readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>>> _map;

    /// <summary>从「资源名 + 已解析资源」集合构建快照（同 culture 多份后份覆盖前份）。</summary>
    public StaticTextSnapshot(IEnumerable<(string ResourceName, JsonTextResource Resource)> resources)
    {
        Check.NotNull(resources);
        var map = new Dictionary<string, Dictionary<string, Dictionary<string, string>>>(StringComparer.OrdinalIgnoreCase);
        foreach (var (resourceName, resource) in resources)
        {
            if (!map.TryGetValue(resourceName, out var byCulture))
            {
                byCulture = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
                map[resourceName] = byCulture;
            }
            if (!byCulture.TryGetValue(resource.CultureName, out var dict))
            {
                dict = new Dictionary<string, string>(StringComparer.Ordinal);
                byCulture[resource.CultureName] = dict;
            }
            foreach (var (key, value) in resource.Entries)
                dict[key] = value;
        }
        _map = map.ToDictionary(
            p => p.Key,
            p => (IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>>)p.Value.ToDictionary(
                c => c.Key, c => (IReadOnlyDictionary<string, string>)c.Value, StringComparer.OrdinalIgnoreCase),
            StringComparer.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public string? Find(string resourceName, string name, IReadOnlyList<string> candidateCultures)
    {
        Check.NotNull(resourceName);
        Check.NotNull(name);
        Check.NotNull(candidateCultures);
        if (!_map.TryGetValue(resourceName, out var byCulture))
            return null;
        foreach (var culture in candidateCultures)
        {
            if (byCulture.TryGetValue(culture, out var dict) && dict.TryGetValue(name, out var value))
                return value;
        }
        return null;
    }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, string> GetAll(string resourceName, IReadOnlyList<string> candidateCultures)
    {
        Check.NotNull(resourceName);
        Check.NotNull(candidateCultures);
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        if (!_map.TryGetValue(resourceName, out var byCulture))
            return result;
        for (var i = candidateCultures.Count - 1; i >= 0; i--) // 低→高，当前 culture 最后覆盖
        {
            if (byCulture.TryGetValue(candidateCultures[i], out var dict))
                foreach (var (key, value) in dict)
                    result[key] = value;
        }
        return result;
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Tw.Core.Tests.csproj --filter StaticTextSnapshotTests`
Expected: PASS。

- [ ] **Step 5: Commit**

```bash
git add backend/dotnet/BuildingBlocks/src/Tw.Core/Localization/IStaticTextSnapshot.cs backend/dotnet/BuildingBlocks/src/Tw.Core/Localization/StaticTextSnapshot.cs backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Localization/StaticTextSnapshotTests.cs
git commit -m "feat(core): add synchronous static text snapshot"
```

---

## Task 8: 内存动态文案 store 替身 + 动态覆盖贡献源

**Files:**
- Create: `backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Localization/Fakes/InMemoryDynamicTextStore.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/Localization/DynamicTextContributor.cs`
- Test: `backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Localization/DynamicTextContributorTests.cs`

动态覆盖维度：`TenantId`、`ResourceName`、`CultureName`、`Name`。租户覆盖优先于全局覆盖；动态优先于静态。空字符串是合法文本。`Source = DynamicOverride`。

- [ ] **Step 1: Write the failing test（含内存替身）**

`Fakes/InMemoryDynamicTextStore.cs`:

```csharp
using Tw.Localization;
using Tw.Localization.Requests;

namespace Tw.Core.Tests.Localization.Fakes;

/// <summary>测试用内存动态文案 store。键：(tenantId|"", culture, resource, name)。</summary>
public sealed class InMemoryDynamicTextStore : IDynamicTextStore
{
    private readonly List<EntityRow> _rows = new();

    private sealed record EntityRow(string? TenantId, string Resource, string Culture, string Name, string Value);

    public InMemoryDynamicTextStore Add(string? tenantId, string resource, string culture, string name, string value)
    {
        _rows.Add(new EntityRow(tenantId, resource, culture, name, value));
        return this;
    }

    public ValueTask<LocalizedText?> FindAsync(TextLookupRequest request, CancellationToken cancellationToken = default)
    {
        // 候选 culture 从高到低；同 culture 内租户优先于全局。
        foreach (var culture in request.CandidateCultures)
        {
            foreach (var tenant in TenantPriority(request.Context.TenantId))
            {
                var row = _rows.FirstOrDefault(r =>
                    r.Resource == request.ResourceName && r.Culture == culture &&
                    r.Name == request.Name && r.TenantId == tenant);
                if (row is not null)
                {
                    return ValueTask.FromResult<LocalizedText?>(new LocalizedText(
                        row.Name, row.Value, culture, request.ResourceName,
                        resourceNotFound: false, LocalizedTextSource.DynamicOverride));
                }
            }
        }
        return ValueTask.FromResult<LocalizedText?>(null);
    }

    public ValueTask<IReadOnlyList<LocalizedText>> GetListAsync(TextFillRequest request, CancellationToken cancellationToken = default)
    {
        var list = new List<LocalizedText>();
        foreach (var culture in request.CandidateCultures)
        {
            foreach (var row in _rows.Where(r => r.Resource == request.ResourceName && r.Culture == culture))
            {
                if (row.TenantId is null || row.TenantId == request.Context.TenantId)
                    list.Add(new LocalizedText(row.Name, row.Value, culture, request.ResourceName, false, LocalizedTextSource.DynamicOverride));
            }
        }
        return ValueTask.FromResult<IReadOnlyList<LocalizedText>>(list);
    }

    private static IEnumerable<string?> TenantPriority(string? tenantId)
    {
        if (tenantId is not null) yield return tenantId;
        yield return null;
    }
}
```

`DynamicTextContributorTests.cs`:

```csharp
using FluentAssertions;
using Tw.Core.Tests.Localization.Fakes;
using Tw.Localization;
using Tw.Localization.Requests;
using Xunit;

namespace Tw.Core.Tests.Localization;

public class DynamicTextContributorTests
{
    [Fact]
    public async Task GetOrNull_PrefersTenantOverGlobal()
    {
        var store = new InMemoryDynamicTextStore()
            .Add(tenantId: null, "Menu", "zh-Hans", "Dashboard", "全局控制台")
            .Add(tenantId: "t1", "Menu", "zh-Hans", "Dashboard", "租户控制台");
        var contributor = new DynamicTextContributor(store, priority: 100);
        var request = new TextLookupRequest("Menu", "Dashboard",
            new LocalizationContext("zh-Hans") { TenantId = "t1" }, new[] { "zh-Hans" });

        var result = await contributor.GetOrNullAsync(request);

        result!.Value.Should().Be("租户控制台");
        result.Source.Should().Be(LocalizedTextSource.DynamicOverride);
    }

    [Fact]
    public async Task GetOrNull_EmptyStringIsValidOverride()
    {
        var store = new InMemoryDynamicTextStore().Add(null, "Menu", "zh-Hans", "Dashboard", "");
        var contributor = new DynamicTextContributor(store, 100);
        var request = new TextLookupRequest("Menu", "Dashboard",
            new LocalizationContext("zh-Hans"), new[] { "zh-Hans" });

        var result = await contributor.GetOrNullAsync(request);

        result.Should().NotBeNull();
        result!.Value.Should().Be("");
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Tw.Core.Tests.csproj --filter DynamicTextContributorTests`
Expected: 编译失败（`DynamicTextContributor` 不存在）。

- [ ] **Step 3: Write minimal implementation**

`DynamicTextContributor.cs`:

```csharp
using Tw.Localization.Requests;

namespace Tw.Localization;

/// <summary>把 <see cref="IDynamicTextStore"/> 包装为高优先级文案贡献源。</summary>
public sealed class DynamicTextContributor : ITextResourceContributor
{
    private readonly IDynamicTextStore _store;

    /// <summary>初始化动态覆盖贡献源。</summary>
    /// <param name="store">动态文案 store。</param>
    /// <param name="priority">优先级，应高于静态 JSON 贡献源。</param>
    public DynamicTextContributor(IDynamicTextStore store, int priority)
    {
        _store = Check.NotNull(store);
        Priority = priority;
    }

    /// <inheritdoc />
    public int Priority { get; }

    /// <inheritdoc />
    public ValueTask<LocalizedText?> GetOrNullAsync(
        TextLookupRequest request, CancellationToken cancellationToken = default) =>
        _store.FindAsync(request, cancellationToken);

    /// <inheritdoc />
    public async ValueTask FillAsync(
        TextFillRequest request, IDictionary<string, LocalizedText> texts,
        CancellationToken cancellationToken = default)
    {
        Check.NotNull(request);
        Check.NotNull(texts);

        var list = await _store.GetListAsync(request, cancellationToken);
        // store 返回顺序按候选从高到低，租户/全局混合；逆序写入实现高覆盖低。
        for (var i = list.Count - 1; i >= 0; i--)
            texts[list[i].Name] = list[i];
    }
}
```

> 说明：动态覆盖填充的精确「租户>全局、当前>父级>默认」覆盖序由 store 的 `GetListAsync` 返回顺序保证。设计稿要求 store 实现维护这一顺序；测试替身已按候选 culture 从高到低产出。`FillAsync` 逆序写入即可让高优先项最终生效。

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Tw.Core.Tests.csproj --filter DynamicTextContributorTests`
Expected: PASS。

- [ ] **Step 5: Commit**

```bash
git add backend/dotnet/BuildingBlocks/src/Tw.Core/Localization/DynamicTextContributor.cs "backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Localization/Fakes/InMemoryDynamicTextStore.cs" backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Localization/DynamicTextContributorTests.cs
git commit -m "feat(core): add dynamic text contributor and in-memory store fake"
```

---

## Task 9: `TextLocalizer` 编排实现

**Files:**
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/Localization/TextLocalizer.cs`
- Test: `backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Localization/TextLocalizerTests.cs`

职责：用 `CultureFallback.ExpandCandidates` 展开候选 → 构造 `TextLookupRequest`/`TextFillRequest` → 按 `Priority` 高→低遍历 contributor 调 `GetOrNullAsync`，首个非 null 即返回（`GetAsync`）；按 `Priority` 低→高遍历调 `FillAsync` 累积（`GetAllAsync`）。`GetAsync` 全部未命中时按 `MissingTextBehavior` 返回（ReturnKey/ReturnEmpty/ReturnKeyAndLog），结果 `ResourceNotFound=true`。取消令牌用 `FallbackToProvider`。

- [ ] **Step 1: Write the failing test**

```csharp
using FluentAssertions;
using Tw.Context;
using Tw.Core.Tests.Localization.Fakes;
using Tw.Localization;
using Tw.Localization.Json;
using Xunit;

namespace Tw.Core.Tests.Localization;

public class TextLocalizerTests
{
    private static TextLocalizer Build(IDynamicTextStore? dynamic = null)
    {
        var options = new LocalizationOptions { DefaultCulture = "en-US", SupportedCultures = { "en-US", "zh-Hans" } };
        var staticContributor = new JsonTextResourceContributor("Menu", priority: 0, new[]
        {
            new JsonTextResource("en-US", new Dictionary<string, string> { ["Dashboard"] = "Dashboard", ["Home"] = "Home" }),
            new JsonTextResource("zh-Hans", new Dictionary<string, string> { ["Dashboard"] = "控制台" }),
        });
        var contributors = new List<ITextResourceContributor> { staticContributor };
        if (dynamic is not null)
            contributors.Add(new DynamicTextContributor(dynamic, priority: 100));

        return new TextLocalizer(contributors, options,
            new NullCancellationTokenProvider(new AsyncLocalCancellationTokenScopeProvider()));
    }

    [Fact]
    public async Task GetAsync_ReturnsCurrentCulture()
    {
        var result = await Build().GetAsync("Menu", "Dashboard", new LocalizationContext("zh-Hans"));
        result.Value.Should().Be("控制台");
        result.ResourceNotFound.Should().BeFalse();
    }

    [Fact]
    public async Task GetAsync_FallsBackToDefaultCulture()
    {
        var result = await Build().GetAsync("Menu", "Home", new LocalizationContext("zh-Hans"));
        result.Value.Should().Be("Home");
        result.CultureName.Should().Be("en-US");
    }

    [Fact]
    public async Task GetAsync_DynamicOverridesStatic()
    {
        var store = new InMemoryDynamicTextStore().Add(null, "Menu", "zh-Hans", "Dashboard", "动态控制台");
        var result = await Build(store).GetAsync("Menu", "Dashboard", new LocalizationContext("zh-Hans"));
        result.Value.Should().Be("动态控制台");
        result.Source.Should().Be(LocalizedTextSource.DynamicOverride);
    }

    [Fact]
    public async Task GetAsync_Missing_ReturnsKeyAndFlag()
    {
        var result = await Build().GetAsync("Menu", "Nope", new LocalizationContext("zh-Hans"));
        result.Value.Should().Be("Nope");
        result.ResourceNotFound.Should().BeTrue();
    }

    [Fact]
    public async Task GetAllAsync_MergesWithCurrentCultureWinning()
    {
        var store = new InMemoryDynamicTextStore().Add(null, "Menu", "zh-Hans", "Home", "动态主页");
        var all = await Build(store).GetAllAsync("Menu", new LocalizationContext("zh-Hans"));
        var byName = all.ToDictionary(t => t.Name, t => t.Value);
        byName["Dashboard"].Should().Be("控制台");   // 静态当前 culture
        byName["Home"].Should().Be("动态主页");        // 动态覆盖静态默认
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Tw.Core.Tests.csproj --filter TextLocalizerTests`
Expected: 编译失败。

- [ ] **Step 3: Write minimal implementation**

```csharp
using Tw.Context;
using Tw.Localization.Requests;

namespace Tw.Localization;

/// <summary>默认系统文案编排实现。</summary>
public sealed class TextLocalizer : ITextLocalizer
{
    private readonly IReadOnlyList<ITextResourceContributor> _byPriorityDesc;
    private readonly IReadOnlyList<ITextResourceContributor> _byPriorityAsc;
    private readonly LocalizationOptions _options;
    private readonly ICancellationTokenProvider _cancellationTokenProvider;

    /// <summary>初始化编排器。</summary>
    public TextLocalizer(
        IEnumerable<ITextResourceContributor> contributors,
        LocalizationOptions options,
        ICancellationTokenProvider cancellationTokenProvider)
    {
        Check.NotNull(contributors);
        _options = Check.NotNull(options);
        _cancellationTokenProvider = Check.NotNull(cancellationTokenProvider);

        var list = contributors.ToList();
        _byPriorityDesc = list.OrderByDescending(c => c.Priority).ToList();
        _byPriorityAsc = list.OrderBy(c => c.Priority).ToList();
    }

    /// <inheritdoc />
    public async ValueTask<LocalizedText> GetAsync(
        string resourceName, string name, LocalizationContext context,
        CancellationToken cancellationToken = default)
    {
        Check.NotNull(resourceName);
        Check.NotNull(name);
        Check.NotNull(context);
        var token = _cancellationTokenProvider.FallbackToProvider(cancellationToken);

        var candidates = CultureFallback.ExpandCandidates(context, _options.DefaultCulture);
        var request = new TextLookupRequest(resourceName, name, context, candidates);

        foreach (var contributor in _byPriorityDesc)
        {
            var hit = await contributor.GetOrNullAsync(request, token);
            if (hit is not null)
                return hit;
        }

        var value = _options.MissingTextBehavior == MissingTextBehavior.ReturnEmpty ? string.Empty : name;
        // ReturnKeyAndLog 的日志在注册了 logger 的装饰层处理；核心默认不依赖 ILogger。
        return new LocalizedText(name, value, context.CultureName, resourceName,
            resourceNotFound: true, LocalizedTextSource.NotFound);
    }

    /// <inheritdoc />
    public async ValueTask<IReadOnlyList<LocalizedText>> GetAllAsync(
        string resourceName, LocalizationContext context,
        CancellationToken cancellationToken = default)
    {
        Check.NotNull(resourceName);
        Check.NotNull(context);
        var token = _cancellationTokenProvider.FallbackToProvider(cancellationToken);

        var candidates = CultureFallback.ExpandCandidates(context, _options.DefaultCulture);
        var request = new TextFillRequest(resourceName, context, candidates);

        var bag = new Dictionary<string, LocalizedText>(StringComparer.Ordinal);
        foreach (var contributor in _byPriorityAsc)
            await contributor.FillAsync(request, bag, token);

        return bag.Values.ToList();
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Tw.Core.Tests.csproj --filter TextLocalizerTests`
Expected: PASS。

- [ ] **Step 5: Commit**

```bash
git add backend/dotnet/BuildingBlocks/src/Tw.Core/Localization/TextLocalizer.cs backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Localization/TextLocalizerTests.cs
git commit -m "feat(core): add TextLocalizer orchestration"
```

---

## Task 10: 业务实体翻译服务 + 内存替身

**Files:**
- Create: `backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Localization/Fakes/InMemoryEntityTranslationStore.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/Localization/EntityTranslationService.cs`
- Test: `backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Localization/EntityTranslationServiceTests.cs`

回退链（设计稿「业务实体翻译流」）：当前租户当前 culture → 全局当前 culture → 当前租户父级 → 全局父级 → 当前租户默认 → 全局默认 → null。service 用 `CultureFallback.ExpandCandidates` 展开候选，一次性 `IEntityTranslationStore.GetListAsync` 批量取，内存按「culture 高→低、同 culture 租户优先全局」裁决，避免 N+1。

- [ ] **Step 1: Write the failing test（含内存替身）**

`Fakes/InMemoryEntityTranslationStore.cs`:

```csharp
using Tw.Localization;
using Tw.Localization.Requests;

namespace Tw.Core.Tests.Localization.Fakes;

/// <summary>测试用内存实体翻译 store，一次返回查询命中的全部翻译。</summary>
public sealed class InMemoryEntityTranslationStore : IEntityTranslationStore
{
    private readonly List<EntityTranslation> _rows = new();
    public int CallCount { get; private set; }

    public InMemoryEntityTranslationStore Add(EntityTranslation row)
    {
        _rows.Add(row);
        return this;
    }

    public ValueTask<IReadOnlyList<EntityTranslation>> GetListAsync(
        EntityTranslationQuery query, CancellationToken cancellationToken = default)
    {
        CallCount++;
        var hits = _rows.Where(r =>
            r.EntityType == query.EntityType &&
            query.EntityIds.Contains(r.EntityId) &&
            query.FieldNames.Contains(r.FieldName) &&
            query.CandidateCultures.Contains(r.CultureName) &&
            (r.TenantId is null || r.TenantId == query.TenantId)).ToList();
        return ValueTask.FromResult<IReadOnlyList<EntityTranslation>>(hits);
    }
}
```

`EntityTranslationServiceTests.cs`:

```csharp
using FluentAssertions;
using Tw.Context;
using Tw.Core.Tests.Localization.Fakes;
using Tw.Localization;
using Tw.Localization.Requests;
using Xunit;

namespace Tw.Core.Tests.Localization;

public class EntityTranslationServiceTests
{
    private static EntityTranslationService Build(InMemoryEntityTranslationStore store)
    {
        var options = new LocalizationOptions { DefaultCulture = "en-US", SupportedCultures = { "en-US", "zh-Hans" } };
        return new EntityTranslationService(store, options,
            new NullCancellationTokenProvider(new AsyncLocalCancellationTokenScopeProvider()));
    }

    [Fact]
    public async Task GetField_ReturnsCurrentCulture()
    {
        var store = new InMemoryEntityTranslationStore()
            .Add(new EntityTranslation("Product", "1", "Name", "zh-Hans", "手机"));
        var result = await Build(store).GetFieldAsync(
            new EntityTranslationLookup(new EntityTranslationKey("Product", "1", "Name"),
                new LocalizationContext("zh-Hans")));
        result.Should().Be("手机");
    }

    [Fact]
    public async Task GetField_PrefersTenantOverGlobal()
    {
        var store = new InMemoryEntityTranslationStore()
            .Add(new EntityTranslation("Product", "1", "Name", "zh-Hans", "全局名", tenantId: null))
            .Add(new EntityTranslation("Product", "1", "Name", "zh-Hans", "租户名", tenantId: "t1"));
        var result = await Build(store).GetFieldAsync(
            new EntityTranslationLookup(new EntityTranslationKey("Product", "1", "Name"),
                new LocalizationContext("zh-Hans") { TenantId = "t1" }));
        result.Should().Be("租户名");
    }

    [Fact]
    public async Task GetField_ReturnsNull_WhenMissing()
    {
        var store = new InMemoryEntityTranslationStore();
        var result = await Build(store).GetFieldAsync(
            new EntityTranslationLookup(new EntityTranslationKey("Product", "9", "Name"),
                new LocalizationContext("zh-Hans")));
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetFields_BatchQuery_HitsStoreOnce()
    {
        var store = new InMemoryEntityTranslationStore()
            .Add(new EntityTranslation("Product", "1", "Name", "zh-Hans", "手机"))
            .Add(new EntityTranslation("Product", "2", "Name", "zh-Hans", "电脑"));
        var query = new EntityTranslationBatchQuery(
            new[]
            {
                new EntityTranslationKey("Product", "1", "Name"),
                new EntityTranslationKey("Product", "2", "Name"),
            },
            new LocalizationContext("zh-Hans"));

        var result = await Build(store).GetFieldsAsync(query);

        result.Should().HaveCount(2);
        result[new EntityTranslationKey("Product", "1", "Name")].Value.Should().Be("手机");
        store.CallCount.Should().Be(1); // 避免 N+1
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Tw.Core.Tests.csproj --filter EntityTranslationServiceTests`
Expected: 编译失败。

- [ ] **Step 3: Write minimal implementation**

```csharp
using Tw.Context;
using Tw.Localization.Requests;

namespace Tw.Localization;

/// <summary>默认业务实体翻译服务，批量查询 + 回退裁决。</summary>
public sealed class EntityTranslationService : IEntityTranslationService
{
    private readonly IEntityTranslationStore _store;
    private readonly LocalizationOptions _options;
    private readonly ICancellationTokenProvider _cancellationTokenProvider;

    /// <summary>初始化实体翻译服务。</summary>
    public EntityTranslationService(
        IEntityTranslationStore store,
        LocalizationOptions options,
        ICancellationTokenProvider cancellationTokenProvider)
    {
        _store = Check.NotNull(store);
        _options = Check.NotNull(options);
        _cancellationTokenProvider = Check.NotNull(cancellationTokenProvider);
    }

    /// <inheritdoc />
    public async ValueTask<string?> GetFieldAsync(
        EntityTranslationLookup lookup, CancellationToken cancellationToken = default)
    {
        Check.NotNull(lookup);
        var batch = new EntityTranslationBatchQuery(new[] { lookup.Key }, lookup.Context);
        var map = await GetFieldsAsync(batch, cancellationToken);
        return map.TryGetValue(lookup.Key, out var hit) ? hit.Value : null;
    }

    /// <inheritdoc />
    public async ValueTask<IReadOnlyDictionary<EntityTranslationKey, EntityTranslation>> GetFieldsAsync(
        EntityTranslationBatchQuery query, CancellationToken cancellationToken = default)
    {
        Check.NotNull(query);
        var token = _cancellationTokenProvider.FallbackToProvider(cancellationToken);

        var candidates = CultureFallback.ExpandCandidates(query.Context, _options.DefaultCulture);
        var cultureRank = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < candidates.Count; i++)
            cultureRank[candidates[i]] = i; // 越小越优先

        // 按 EntityType 分组各发一次（同 type 一次 store 调用）。
        var result = new Dictionary<EntityTranslationKey, EntityTranslation>();
        foreach (var group in query.Keys.GroupBy(k => k.EntityType))
        {
            var entityIds = group.Select(k => k.EntityId).Distinct().ToList();
            var fieldNames = group.Select(k => k.FieldName).Distinct().ToList();
            var storeQuery = new EntityTranslationQuery(
                group.Key, entityIds, fieldNames, candidates, query.Context.TenantId);

            var rows = await _store.GetListAsync(storeQuery, token);

            foreach (var row in rows)
            {
                var key = new EntityTranslationKey(row.EntityType, row.EntityId, row.FieldName);
                if (!cultureRank.TryGetValue(row.CultureName, out var rank))
                    continue;
                if (!result.TryGetValue(key, out var existing) || IsBetter(row, rank, existing, cultureRank, query.Context.TenantId))
                    result[key] = row;
            }
        }
        return result;
    }

    private static bool IsBetter(
        EntityTranslation candidate, int candidateRank,
        EntityTranslation existing, IReadOnlyDictionary<string, int> cultureRank, string? tenantId)
    {
        var existingRank = cultureRank[existing.CultureName];
        if (candidateRank != existingRank)
            return candidateRank < existingRank; // culture 更优先
        // 同 culture：租户优先于全局
        var candidateTenantWins = candidate.TenantId == tenantId && existing.TenantId is null;
        return candidateTenantWins;
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Tw.Core.Tests.csproj --filter EntityTranslationServiceTests`
Expected: PASS。

- [ ] **Step 5: Commit**

```bash
git add backend/dotnet/BuildingBlocks/src/Tw.Core/Localization/EntityTranslationService.cs "backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Localization/Fakes/InMemoryEntityTranslationStore.cs" backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Localization/EntityTranslationServiceTests.cs
git commit -m "feat(core): add entity translation service with batch fallback"
```

---

## Task 11: `AddLocalization` 核心注册

**Files:**
- Create: `backend/dotnet/BuildingBlocks/src/Tw.Core/Localization/LocalizationServiceCollectionExtensions.cs`
- Test: `backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Localization/LocalizationServiceCollectionExtensionsTests.cs`

注册职责：接收 `Action<LocalizationOptions>` 配置 → 校验 → 注册 `LocalizationOptions` singleton、`ITextLocalizer`→`TextLocalizer`、`IEntityTranslationService`→`EntityTranslationService`、`AddCancellationTokenProvider()`。从 `ResourcePaths` 加载 JSON 文件、解析、按 `resourceName`（文件名约定：`<ResourceName>.<culture>.json` 或目录约定）分组注册 `JsonTextResourceContributor`。`IDynamicTextStore` 不在核心注册（业务应用实现）；若容器已注册 `IDynamicTextStore`，则追加 `DynamicTextContributor`。

> 文件名 → 资源名/culture 约定：采用 `<ResourceName>.<culture>.json`（例 `Menu.zh-Hans.json`）。解析时以文件内 `culture` 为准，资源名取文件名第一段。

- [ ] **Step 1: Write the failing test**

```csharp
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Tw.Localization;
using Xunit;

namespace Tw.Core.Tests.Localization;

public class LocalizationServiceCollectionExtensionsTests
{
    [Fact]
    public void AddLocalization_RegistersLocalizerAndService()
    {
        var services = new ServiceCollection();

        services.AddLocalization(o =>
        {
            o.DefaultCulture = "en-US";
            o.SupportedCultures.Add("en-US");
        });

        using var provider = services.BuildServiceProvider();
        provider.GetService<ITextLocalizer>().Should().BeOfType<TextLocalizer>();
        provider.GetService<IEntityTranslationService>().Should().BeOfType<EntityTranslationService>();
        provider.GetService<LocalizationOptions>().Should().NotBeNull();
    }

    [Fact]
    public void AddLocalization_ValidatesOptions()
    {
        var services = new ServiceCollection();

        var act = () => services.AddLocalization(o =>
        {
            o.DefaultCulture = "bad culture";
            o.SupportedCultures.Add("bad culture");
        });

        act.Should().Throw<TwConfigurationException>();
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Tw.Core.Tests.csproj --filter LocalizationServiceCollectionExtensionsTests`
Expected: 编译失败。

- [ ] **Step 3: Write minimal implementation**

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Tw.Localization.Json;

namespace Tw.Localization;

/// <summary>为 <see cref="IServiceCollection"/> 提供本地化能力注册扩展。</summary>
public static class LocalizationServiceCollectionExtensions
{
    /// <summary>注册本地化核心能力。</summary>
    /// <param name="services">服务容器。</param>
    /// <param name="configure">配置回调。</param>
    /// <exception cref="ArgumentNullException">参数为 <see langword="null"/> 时抛出。</exception>
    /// <exception cref="TwConfigurationException">配置校验失败时抛出。</exception>
    public static IServiceCollection AddLocalization(
        this IServiceCollection services, Action<LocalizationOptions> configure)
    {
        Check.NotNull(services);
        Check.NotNull(configure);

        var options = new LocalizationOptions();
        configure(options);
        options.Validate();

        services.AddCancellationTokenProvider();
        services.TryAddSingleton(options);

        // 加载 JSON 静态资源一次，复用于贡献源与同步快照。
        var parsed = LoadJsonResources(options);

        foreach (var group in parsed.GroupBy(p => p.ResourceName, StringComparer.OrdinalIgnoreCase))
        {
            var contributor = new JsonTextResourceContributor(group.Key, priority: 0, group.Select(p => p.Resource));
            services.AddSingleton<ITextResourceContributor>(contributor);
        }

        services.TryAddSingleton<IStaticTextSnapshot>(new StaticTextSnapshot(parsed));

        services.TryAddSingleton<ITextLocalizer>(sp => new TextLocalizer(
            sp.GetServices<ITextResourceContributor>(),
            sp.GetRequiredService<LocalizationOptions>(),
            sp.GetRequiredService<Tw.Context.ICancellationTokenProvider>()));

        services.TryAddSingleton<IEntityTranslationService>(sp => new EntityTranslationService(
            sp.GetRequiredService<IEntityTranslationStore>(),
            sp.GetRequiredService<LocalizationOptions>(),
            sp.GetRequiredService<Tw.Context.ICancellationTokenProvider>()));

        return services;
    }

    private static List<(string ResourceName, JsonTextResource Resource)> LoadJsonResources(LocalizationOptions options)
    {
        var result = new List<(string, JsonTextResource)>();
        foreach (var path in options.ResourcePaths)
        {
            if (!Directory.Exists(path))
            {
                if (options.RequireResourcePaths)
                    throw new TwConfigurationException($"本地化资源路径不存在：{path}");
                continue;
            }
            foreach (var file in Directory.EnumerateFiles(path, "*.json", SearchOption.AllDirectories).OrderBy(f => f, StringComparer.Ordinal))
            {
                var resourceName = Path.GetFileName(file).Split('.')[0];
                var resource = JsonTextResourceParser.Parse(File.ReadAllText(file), file);
                result.Add((resourceName, resource));
            }
        }
        return result;
    }
}
```

> `DynamicTextContributor` 的注册放在 Plan 3 或业务应用：当业务应用注册了 `IDynamicTextStore` 后，可显式 `services.AddSingleton<ITextResourceContributor>(sp => new DynamicTextContributor(sp.GetRequiredService<IDynamicTextStore>(), 100));`。本任务不强行依赖 `IDynamicTextStore`，以免无 store 时启动失败。`IEntityTranslationService` 工厂依赖 `IEntityTranslationStore`，仅在被解析时才要求其存在。

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Tw.Core.Tests.csproj --filter LocalizationServiceCollectionExtensionsTests`
Expected: PASS。

- [ ] **Step 5: Commit**

```bash
git add backend/dotnet/BuildingBlocks/src/Tw.Core/Localization/LocalizationServiceCollectionExtensions.cs backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Localization/LocalizationServiceCollectionExtensionsTests.cs
git commit -m "feat(core): add AddLocalization registration"
```

---

## Task 12: charter 与共享包文档

**Files:**
- Modify: `backend/dotnet/BuildingBlocks/src/Tw.Core/package-charter.yaml`
- Create: `docs/shared-packages/dotnet/Tw.Core/localization/text-localization.md`
- Create: `docs/shared-packages/dotnet/Tw.Core/localization/entity-translation.md`
- Modify: `docs/shared-packages/dotnet/Tw.Core/README.md`
- Modify: `docs/shared-packages/dotnet/README.md`

- [ ] **Step 1: 更新 charter `public_capabilities`**

编辑 `backend/dotnet/BuildingBlocks/src/Tw.Core/package-charter.yaml`，在 `public_capabilities` 列表新增 `Tw.Localization`（保持字母序合理位置），并在 `in_scope` 增加一行：`- 多语言系统文案与业务实体翻译抽象`。

- [ ] **Step 2: 写能力使用文档（How-to Guide）**

`docs/shared-packages/dotnet/Tw.Core/localization/text-localization.md` 覆盖：能力定位、`AddLocalization` 注册方式（含 JSON 资源目录约定 `<ResourceName>.<culture>.json`）、`ITextLocalizer.GetAsync`/`GetAllAsync` 用法、动态覆盖如何接入（业务实现 `IDynamicTextStore` + 注册 `DynamicTextContributor`）、回退链与缺失文案行为、注意事项（不经全局静态入口）。

`docs/shared-packages/dotnet/Tw.Core/localization/entity-translation.md` 覆盖：能力定位、`IEntityTranslationStore` 由业务实现、`IEntityTranslationService.GetFieldAsync`/`GetFieldsAsync` 用法、回退链、批量避免 N+1、缺失返回 null、不自动覆盖实体字段。

> 参考既有 `docs/shared-packages/dotnet/Tw.Core/context/cancellation-token-provider.md` 的 How-to 结构与措辞风格保持一致。

- [ ] **Step 3: 更新索引（Reference）**

在 `docs/shared-packages/dotnet/Tw.Core/README.md` 增加指向两篇 localization 文档的链接；在 `docs/shared-packages/dotnet/README.md` 增加 Tw.Core localization 能力条目。确保从总索引可跳转。

- [ ] **Step 4: Commit**

```bash
git add backend/dotnet/BuildingBlocks/src/Tw.Core/package-charter.yaml docs/shared-packages/dotnet/Tw.Core/localization docs/shared-packages/dotnet/Tw.Core/README.md docs/shared-packages/dotnet/README.md
git commit -m "docs(shared-packages): document Tw.Core localization capabilities"
```

---

## Task 13: 全量验证

- [ ] **Step 1: 构建**

Run: `dotnet build backend/dotnet/Tw.SmartPlatform.slnx`
Expected: 成功。

- [ ] **Step 2: 全量测试**

Run: `dotnet test backend/dotnet/Tw.SmartPlatform.slnx`
Expected: 全部通过。

- [ ] **Step 3: 依赖边界核查**

确认 `Tw.Core.csproj` 未新增 `Microsoft.AspNetCore.*` 或 `Microsoft.EntityFrameworkCore*` 引用（本计划只用到 `System.Text.Json`、`System.Globalization`，均在 BCL 内，无需新增包引用）。
Run: `dotnet build backend/dotnet/BuildingBlocks/src/Tw.Core/Tw.Core.csproj`
Expected: 成功，无新增第三方依赖。

---

## 完成标准

- `Tw.Localization` 下模型、DTO、接口、`CultureFallback`、JSON 解析与贡献源、`StaticTextSnapshot`、`DynamicTextContributor`、`TextLocalizer`、`EntityTranslationService`、`AddLocalization` 全部实现并测试覆盖。
- 内存 `IDynamicTextStore`、`IEntityTranslationStore` 替身可用于测试。
- 回退链、动态优先、租户优先、缺失行为、批量避免 N+1 均有测试。
- charter 与共享包文档同步更新。
- `Tw.Core` 不引入 ASP.NET Core / EF Core 依赖。
- `dotnet build` 与 `dotnet test` 全量通过。

## Self-Review 备注

- 设计稿「缓存与失效」的分层缓存（语言列表/资源索引/静态字典/动态/实体批量）在首轮以「`JsonTextResourceContributor` 内存持有解析结果 + 接口契约 `ILocalizationCacheInvalidator`/`ILocalizationChangeToken`」覆盖；文件监听（`WatchFileChanges`）与分布式缓存属首轮非目标，留待后续计划，不在本 Plan 任务内。
- 设计稿「运行时导出 API DTO」属 Web 边界，归 Plan 3。
- `MissingTextBehavior.ReturnKeyAndLog` 的日志落地需 `ILogger`，核心默认不依赖；如需，业务应用以装饰器包装 `ITextLocalizer`。本 Plan 仅保证返回值语义正确。
