# Tw.Localization

`Tw.Localization` 提供独立可选的多语言核心能力，包括静态 JSON 文本资源、动态文本覆盖和实体字段翻译查询。本包不依赖 ASP.NET Core 或任何 ORM/EF Core，可独立用于任何 .NET 宿主。

## 依赖边界

| 依赖项 | 说明 |
|--------|------|
| `Tw.Core` | Guard 与基础原语工具 |
| `Microsoft.Extensions.DependencyInjection.Abstractions` | DI 抽象（仅 Abstractions，不依赖宿主） |

不依赖：ASP.NET Core、EF Core、任何数据库驱动或 HTTP 框架。

## 配置异常

`LocalizationOptions.Validate`、`AddLocalization` 和 JSON 资源解析失败时抛出 `Tw.Localization.LocalizationConfigurationException`。该异常继承 `TwException`，用于区分本地化配置或资源格式错误与通用基础异常。

## 注册入口

```csharp
using Microsoft.Extensions.DependencyInjection;
using Tw.Localization;

services.AddLocalization(options =>
{
    options.DefaultCulture = "zh-Hans";
    options.SupportedCultures.Add("zh-Hans");
    options.SupportedCultures.Add("en-US");
    options.JsonResourcePaths.Add("Resources/app.zh-Hans.json");
    options.JsonResourcePaths.Add("Resources/app.en-US.json");
});
```

`AddLocalization` 位于命名空间 `Tw.Localization`（扩展类 `LocalizationServiceCollectionExtensions`）。

## 注册行为

`AddLocalization` 执行以下注册：

- `ITextLocalizer`（Singleton）：文本本地化查找服务。
- `IEntityTranslationService`（Singleton）：实体字段翻译查询服务。
- `IStaticTextSnapshot`（Singleton）：静态 JSON 资源的只读视图。
- 解析 `JsonResourcePaths` 中的所有 JSON 文件并在启动时加载到内存。
- 注册默认的空 `IEntityTranslationStore`（`TryAddSingleton`，业务实现会覆盖）。

> `IEntityTranslationStore` 使用 `TryAddSingleton` 注册。若业务应用提供了自己的实现，在调用 `AddLocalization` **之前**先注册自定义实现，即可覆盖默认空存储。  
> 注意：`IEntityTranslationStore` 必须以 Singleton 方式注册。若注册为 Scoped，单例的 `IEntityTranslationService` 会在整个应用生命周期内持有同一个 Scoped 实例，导致作用域语义失效。

## 能力索引

- [文本本地化使用指南](text-localization.md)：JSON 资源格式、`ITextLocalizer` 用法、动态覆盖扩展点。
- [实体翻译使用指南](entity-translation.md)：`IEntityTranslationService` 用法、`IEntityTranslationStore` 扩展点、批量查询。
