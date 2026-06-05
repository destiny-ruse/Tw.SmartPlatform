# Tw.Localization.AspNetCore

`Tw.Localization.AspNetCore` 是 `Tw.Localization` 的可选 ASP.NET Core 适配包，提供请求文化解析中间件、`IStringLocalizer` 适配器和运行时导出 DTO。本包不依赖 EF Core 或任何 ORM/数据库驱动，可选接入 ASP.NET Core 宿主。

## 依赖边界

| 依赖项 | 说明 |
|--------|------|
| `Tw.Localization` | 多语言核心能力（静态 JSON 快照、`ITextLocalizer`、`IStaticTextSnapshot` 等）|
| `Tw.AspNetCore` | Web 集成基础能力（`IHttpContextAccessor`、`HttpContextCancellationTokenProvider`）|
| `Microsoft.AspNetCore.App`（FrameworkReference）| ASP.NET Core 框架引用（中间件、IStringLocalizer 抽象、HttpContext 等）|

不依赖：EF Core、任何 ORM、数据库驱动或消息队列。

## 注册入口

```csharp
using Microsoft.Extensions.DependencyInjection;
using Tw.Localization.AspNetCore;

services.AddLocalization(options =>
{
    options.DefaultCulture = "zh-Hans";
    options.SupportedCultures.Add("zh-Hans");
    options.SupportedCultures.Add("en-US");
    options.JsonResourcePaths.Add("Resources/app.zh-Hans.json");
    options.JsonResourcePaths.Add("Resources/app.en-US.json");
});
```

`AddLocalization` 位于命名空间 `Tw.Localization.AspNetCore`（扩展类 `LocalizationServiceCollectionExtensions`）。

### 注册行为

`AddLocalization` 依次执行以下注册：

1. 调用 `Tw.AspNetCore` 的 `AddWebIntegration()`：注册 `IHttpContextAccessor`，并将 `ICancellationTokenProvider` 替换为 `HttpContextCancellationTokenProvider`。
2. 调用核心 `Tw.Localization` 的 `AddLocalization(...)`：注册 `ITextLocalizer`、`IStaticTextSnapshot`、`IEntityTranslationService` 等核心服务（均为 Singleton）。
3. 以 **Scoped** 生命周期注册以下 Web 适配服务（均使用 `TryAddScoped`，可被业务实现覆盖）：
   - `ICurrentLocalizationContextAccessor`（实现 `CurrentLocalizationContextAccessor`）：从当前请求读取 `LocalizationContext`。
   - `IStringLocalizerFactory`（实现 `TwStringLocalizerFactory`）：`IStringLocalizer` 工厂，读取静态 JSON 快照。
   - 开放泛型 `IStringLocalizer<>`（实现 `TwStringLocalizer<>`）：`IStringLocalizer` 泛型适配器。

> **生命周期说明**：工厂与适配器刻意注册为 Scoped 而非 Singleton，避免对 Scoped 的 `ICurrentLocalizationContextAccessor` 产生捕获依赖（captive dependency）。

## 能力索引

- [请求本地化使用指南](request-localization.md)：中间件注册、文化解析顺序、`IStringLocalizer` 静态快照边界、运行时导出 DTO。
