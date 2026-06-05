# 请求本地化使用指南

## 能力定位

`Tw.Localization.AspNetCore` 提供基于 ASP.NET Core 请求管道的文化解析能力，将 `Tw.Localization` 核心服务与 HTTP 请求上下文对接。主要能力：

- `RequestLocalizationMiddleware`：从请求上下文中解析当前文化，写入 `ICurrentLocalizationContextAccessor`；当文化来自路由或查询参数显式切换时自动写入持久化 Cookie。
- `ICurrentLocalizationContextAccessor`：在业务代码中无需直接依赖 `HttpContext` 即可读取当前请求的 `LocalizationContext`。
- `TwStringLocalizer` / `TwStringLocalizer<TResource>`：`IStringLocalizer` 适配器，桥接 ASP.NET Core 标准接口与 `Tw.Localization` 静态 JSON 快照。
- `LocalizationResourceDto` / `LocalizationTextDto`：运行时导出 DTO，供业务应用构建本地化资源查询接口。

## DI 注册

在 `Program.cs` 或 Startup 的 `ConfigureServices` 中调用 `AddLocalization`：

```csharp
using Microsoft.Extensions.DependencyInjection;
using Tw.Localization.AspNetCore;

builder.Services.AddLocalization(options =>
{
    options.DefaultCulture = "zh-Hans";
    options.SupportedCultures.Add("zh-Hans");
    options.SupportedCultures.Add("en-US");
    options.JsonResourcePaths.Add("Resources/app.zh-Hans.json");
    options.JsonResourcePaths.Add("Resources/app.en-US.json");
});
```

`AddLocalization`（命名空间 `Tw.Localization.AspNetCore`）同时完成核心服务、Web 集成和 `IStringLocalizer` 适配器的完整注册，调用方无需单独调用 `Tw.Localization.AddLocalization`。

## 中间件注册

在 `Program.cs` 或 Startup 的 `Configure` 中注册中间件：

```csharp
using Tw.Localization.AspNetCore;

// UseRouting 之后，MVC / Endpoints / UseAuthorization 之前
app.UseRouting();
app.UseLocalization();   // 必须在 UseRouting 之后（路由文化值才可读）
app.UseAuthorization();
app.MapControllers();
```

> **顺序要求**：若使用路由文化值（`{culture}` 路由参数），`UseLocalization()` 必须在 `UseRouting()` 之后注册，否则路由值尚未解析，路由优先级将失效。

## 文化解析顺序

中间件按以下优先级从高到低依次尝试解析当前请求的文化：

| 优先级 | 来源 | 说明 |
|--------|------|------|
| 1 | 路由值 `{culture}` | URL 中的路由参数，例如 `/zh-Hans/orders` |
| 2 | 查询参数 `?culture=` | URL 查询字符串，例如 `?culture=en-US` |
| 3 | Cookie `.Tw.Culture` | 上次显式切换时写入的持久化 Cookie |
| 4 | `Accept-Language` 请求头 | 浏览器/客户端发送的首选语言头 |
| 5 | `LocalizationOptions.DefaultCulture` | 配置的默认文化兜底 |

解析到的文化必须在 `LocalizationOptions.SupportedCultures` 中，不支持的文化值被忽略并继续尝试下一来源。

### IsExplicitSwitch 语义

当文化来源为路由（优先级 1）或查询参数（优先级 2）时，解析结果的 `IsExplicitSwitch` 标志为 `true`，表示用户主动切换了语言。

## Cookie 写入规则

Cookie 仅在 `IsExplicitSwitch == true` 时写入，即只有路由或查询参数明确指定了受支持的文化时，才更新持久化 Cookie：

| 属性 | 值 |
|------|-----|
| Cookie 名称 | `.Tw.Culture` |
| Path | `/`（站点级全局） |
| MaxAge | 365 天（持久化，约 1 年） |

Cookie 来源、`Accept-Language` 或默认文化兜底命中时，不写入 Cookie，避免干扰用户的显式偏好。

## 使用 ICurrentLocalizationContextAccessor

在业务服务或 Handler 中，通过注入 `ICurrentLocalizationContextAccessor` 读取当前请求的 `LocalizationContext`，无需直接依赖 `HttpContext`：

```csharp
using Tw.Localization;
using Tw.Localization.AspNetCore;

public class OrderService(
    ICurrentLocalizationContextAccessor localizationAccessor,
    ITextLocalizer localizer)
{
    public async ValueTask<string> GetStatusLabelAsync(string statusKey)
    {
        var context = localizationAccessor.Current;
        var result = await localizer.GetAsync("orders", statusKey, context);
        return result.Value;
    }
}
```

`ICurrentLocalizationContextAccessor.Current` 返回 `LocalizationContext`，包含当前请求解析到的文化名称，可直接传递给 `ITextLocalizer.GetAsync` 或 `GetAllAsync`。

## IStringLocalizer 静态快照边界

`TwStringLocalizer` 和 `TwStringLocalizer<TResource>` 实现了 ASP.NET Core 标准的 `IStringLocalizer` 接口，适用于与现有 ASP.NET Core 本地化生态（如 DataAnnotations 验证消息）集成的场景。

**边界约束**：`IStringLocalizer` 适配器只读取**静态 JSON 快照**（`IStaticTextSnapshot`），不访问动态文本覆盖（`IDynamicTextStore`）。同步 `IStringLocalizer` 接口不支持异步 I/O，因此无法访问可能涉及数据库的动态来源。

如需访问动态文本覆盖，请使用异步的 `ITextLocalizer`：

```csharp
// 静态快照（同步，只读 JSON）
IStringLocalizer<MyResource> staticLocalizer = ...;
var label = staticLocalizer["Menu__Home"].Value;

// 动态 + 静态（异步，优先动态覆盖）
ITextLocalizer dynamicLocalizer = ...;
var result = await dynamicLocalizer.GetAsync("app", "Menu__Home", context);
```

## 运行时导出 DTO

`LocalizationResourceDto` 和 `LocalizationTextDto` 是纯 record DTO，用于将本地化资源数据序列化为 HTTP 响应，供前端或其他服务消费。两者均无 ASP.NET Core 或 EF Core 依赖，可在任何宿主中使用。

### 数据结构

```csharp
// 单条文本
record LocalizationTextDto(string Name, string Value, bool ResourceNotFound);

// 资源集合
record LocalizationResourceDto(
    string ResourceName,
    string CultureName,
    IReadOnlyList<LocalizationTextDto> Texts);
```

### 推荐接口形状

业务应用拥有实际的 Controller、鉴权和审计逻辑。推荐的只读查询接口形状如下：

```
GET /api/localization/resources/{resourceName}?culture=zh-Hans
GET /api/localization/resources/{resourceName}?culture=zh-Hans&onlyDynamic=true
```

示例 Controller 实现思路（业务应用自行实现，本包不提供）：

```csharp
[HttpGet("resources/{resourceName}")]
[Authorize(Policy = "InternalApi")]
public async Task<LocalizationResourceDto> GetResourceAsync(
    string resourceName,
    [FromQuery] string culture,
    [FromQuery] bool onlyDynamic = false,
    CancellationToken ct = default)
{
    var context = new LocalizationContext(culture);
    var texts = await localizer.GetAllAsync(resourceName, context, ct);

    return new LocalizationResourceDto(
        resourceName,
        culture,
        texts.Select(t => new LocalizationTextDto(t.Name, t.Value, t.ResourceNotFound))
             .ToList());
}
```

`onlyDynamic=true` 的过滤逻辑由业务应用自行实现（例如过滤 `Source != LocalizedTextSource.Dynamic` 的条目）；鉴权策略、审计日志和缓存均由业务应用负责。

## 注意事项

- 业务应用只应调用 `Tw.Localization.AspNetCore.AddLocalization(...)`，不要额外再调用 `Tw.Localization.AddLocalization(...)`。前者内部已调用核心注册；核心注册使用 `AddSingleton`（非 `TryAdd`），重复调用会产生重复注册（如重复的 `ITextResourceContributor` 和 `IStaticTextSnapshot`），不具备幂等保护。
- `IStringLocalizerFactory`、`IStringLocalizer<>` 和 `ICurrentLocalizationContextAccessor` 均以 Scoped 方式注册，不得在 Singleton 服务中直接注入，避免捕获依赖；若 Singleton 确实需要读取当前本地化上下文，应注入 `IServiceScopeFactory` 并在需要时手动创建作用域获取。
- Cookie 名称常量为 `RequestLocalizationMiddleware.CultureCookieName`（值为 `".Tw.Culture"`），如需在其他中间件或客户端代码中引用，应使用此常量而非硬编码字符串。
